
#if UNITY_PURCHASING

using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Cysharp.Threading.Tasks;
using UniRx;
using Extensions;
using Modules.Devkit.Console;

namespace Modules.InAppPurchasing
{
    /// <summary>
    /// 購入失敗時のエラー.
    /// </summary>
    public enum BuyFailureReason
    {
        /// <summary> エラー無し </summary>
        None,

        /// <summary> 課金システム未初期化 </summary>
        NotInitialization,

        /// <summary> 販売されていないアイテムを指定した </summary>
        UnknownItem,

        /// <summary> 課金メッセージを受け取れない </summary>
        NotReceiveMessage,

        /// <summary> 通信不可（課金システムの初期化は完了）</summary>
        NetworkUnavailable,

        /// <summary> 購入処理中 </summary>
        InPurchaseing,

        /// <summary> 不明なエラー </summary>
        Unknown
    }

    public abstract class PurchaseManager<TInstance> : Singleton<TInstance>, IDetailedStoreListener
        where TInstance : PurchaseManager<TInstance>
    {
        //----- params -----

        public readonly string ConsoleEventName = "Purchase";
        public readonly Color ConsoleEventColor = new Color(0.2f, 0.6f, 0.8f);

        // ダミーストア名.
        private const string DummyStoreName = "DummyStore";

        //----- field -----

        // 基本的な課金処理を行うオブジェクト.
        protected IStoreController storeController = null;

        // 各ストアに依存する課金処理を行うオブジェクト.
        protected IExtensionProvider storeExtensionProvider = null;

        // 各ストア用の課金処理オブジェクト.
        protected IStorePurchasing storePurchasing = null;

        // 課金リスト更新通知.
        private Subject<Product[]> onStoreProductsUpdate = null;
        // 課金完了通知.
        private Subject<PurchaseResult> onStorePurchaseComplete = null;
        // 課金復元通知.
        private Subject<Product> onStorePurchaseRestore = null;

        protected bool initialized = false;

        //----- property -----

        /// <summary>
        /// 課金処理の準備が出来ているか.
        /// </summary>
        public bool IsPurchaseReady { get; private set; }

        /// <summary>
        /// 販売中の課金アイテム.
        /// </summary>
        public Product[] StoreProducts { get; private set; }

        /// <summary>
        /// Pendingとなった課金アイテム.
        /// </summary>
        public Product[] PendingProducts { get; private set; }

        /// <summary>
        /// 購入中か.
        /// </summary>
        public bool IsPurchaseing { get; protected set; }

        /// <summary>
        /// 各ストア用の課金処理オブジェクト
        /// </summary>
        public IStorePurchasing StorePurchasing { get { return storePurchasing; } }

        //----- method -----

        public virtual void Initialize()
        {
            if (initialized) { return; }

            StoreProducts = new Product[] { };
            PendingProducts = new Product[] { };

            IsPurchaseReady = false;
            IsPurchaseing = false;

            SetupStorePurchasing();

            initialized = true;
        }

        protected virtual void SetupStorePurchasing()
        {
            var purchasingModule = StandardPurchasingModule.Instance();

            var currentAppStore = purchasingModule.appStore;

            switch (currentAppStore)
            {
                case AppStore.AppleAppStore:
                    storePurchasing = new ApplePurchasing();
                    break;

                case AppStore.GooglePlay:
                    storePurchasing = new GooglePlayStorePurchasing();
                    break;
            }

            if (storePurchasing != null)
            {
                storePurchasing.Initialize();
            }
        }

        /// <summary>
        /// 商品情報更新.
        /// </summary>
        /// <returns></returns>
        public async UniTask UpdateProducts()
        {
            var products = await FetchProducts();

            if (!IsPurchaseReady)
            {
                InitializePurchasing(products);
            }
            else
            {
                UpdatePurchasing(products);
            }
        }

        /// <summary>
        /// 課金システム初期化.
        /// </summary>
        private void InitializePurchasing(ProductDefinition[] productDefinitions)
        {
            storeController = null;
            storeExtensionProvider = null;

            // Unityの課金システム構築.
            var module = StandardPurchasingModule.Instance();

            #if UNITY_EDITOR

            module.useFakeStoreUIMode = FakeStoreUIMode.StandardUser;

            #endif

            var builder = ConfigurationBuilder.Instance(module);

            if (productDefinitions.Any())
            {
                foreach (var productDefinition in productDefinitions)
                {
                    var ids = new IDs();

                    var storeName = DummyStoreName;

                    if (storePurchasing != null)
                    {
                        storeName = storePurchasing.GetStoreName();
                    }

                    if (!string.IsNullOrEmpty(storeName))
                    {
                        ids.Add(productDefinition.storeSpecificId, storeName);
                    }

                    builder.AddProduct(productDefinition.storeSpecificId, productDefinition.type, ids);
                }
            }

            // 非同期の課金処理の初期化を開始.
            UnityPurchasing.Initialize(this, builder);
        }

        /// <summary> ストア商品リストを更新. </summary>
        private void UpdatePurchasing(ProductDefinition[] productDefinitions)
        {
            void OnSuccessCallback()
            {
                OnSuccessUpdatePurchasing(productDefinitions);
            }

            storeController.FetchAdditionalProducts(productDefinitions.ToHashSet(), OnSuccessCallback, OnFailedUpdatePurchasing);
        }

        protected virtual void OnSuccessUpdatePurchasing(ProductDefinition[] productDefinitions)
        {
            StoreProducts = storeController.products.all
                .Where(x => !string.IsNullOrEmpty(x.metadata.localizedTitle))
                .Where(x => !string.IsNullOrEmpty(x.metadata.localizedPriceString))
                .Where(x => productDefinitions.Any(y => y.id == x.definition.id && y.storeSpecificId == x.definition.storeSpecificId))
                .ToArray();

            if (onStoreProductsUpdate != null)
            {
                onStoreProductsUpdate.OnNext(StoreProducts);
            }
        }

        protected virtual void OnFailedUpdatePurchasing(InitializationFailureReason reason, string message)
        {
            var logMessage = $"UpdatePurchasing Error. ({reason})";

            UnityConsole.Event(ConsoleEventName, ConsoleEventColor, logMessage, LogType.Error);
        }

        #region Purchase

        /// <summary> アイテムの購入 </summary>
        protected BuyFailureReason Purchase(string productId, string developerPayload = null)
        {
            var result = PurchaseInternal(productId, developerPayload);

            if (result == BuyFailureReason.None)
            {
                var product = StoreProducts.FirstOrDefault(x => x.definition.storeSpecificId == productId);

                if (product != null)
                {
                    var builder = new StringBuilder();

                    builder.AppendLine("------- PurchaseProducts -------");
                    builder.AppendLine(GetProductString(product)).AppendLine();

                    UnityConsole.Event(ConsoleEventName, ConsoleEventColor, builder.ToString());
                }
            }
            else
            {
                OnPurchaseError(result);
            }

            return result;
        }

        protected virtual void OnPurchaseError(BuyFailureReason reason)
        {
            var message = $"Purchase Error. ({reason})";

            UnityConsole.Event(ConsoleEventName, ConsoleEventColor, message, LogType.Error);
        }

        /// <summary> アイテムの購入 </summary>
        private BuyFailureReason PurchaseInternal(string productId, string developerPayload)
        {
            // コールバックが通知できない場合は何もしない.
            if (onStorePurchaseComplete == null || !onStorePurchaseComplete.HasObservers)
            {
                return BuyFailureReason.NotReceiveMessage;
            }

            // 購入処理中.
            if (IsPurchaseing) { return BuyFailureReason.InPurchaseing; }

            try
            {
                // 課金システムが未初期化の場合はなにもしない.
                if (!IsPurchaseReady)
                {
                    return BuyFailureReason.NotInitialization;
                }

                var product = storeController.products.WithID(productId);

                // 購入できないアイテムの場合.
                if (product == null || !product.availableToPurchase)
                {
                    return BuyFailureReason.UnknownItem;
                }

                // 通信不可の場合は何もしない（初期化は終了済み）.
                if (!NetworkConnection())
                {
                    return BuyFailureReason.NetworkUnavailable;
                }

                // Androidの場合はDeveloperPayloadを送る.
                if (Application.platform == RuntimePlatform.Android)
                {
                    storeController.InitiatePurchase(product, developerPayload);
                }
                else
                {
                    storeController.InitiatePurchase(product);
                }

                IsPurchaseing = true;

                return BuyFailureReason.None;
            }
            catch (Exception)
            {
                // 何らかのエラーが発生（課金は未発生）.
                return BuyFailureReason.Unknown;
            }
        }

        /// <summary> 購入処理を完了しアイテムを購入完了状態に更新 </summary>
        public void PurchaseFinish(Product product)
        {
            UpdatePendingProduct(product, PurchaseProcessingResult.Complete);
        }

        #endregion

        #region Restore

        /// <summary>
        /// 通信の影響で購入失敗したアイテムや、再インストール時に(非消費型/サブスクリプション型)アイテムを復元.
        /// </summary>
        public BuyFailureReason Restore()
        {
            var result = RestoreInternal();

            if (result != BuyFailureReason.None)
            {
                var message = $"Restore Error. ({result})";

                UnityConsole.Event(ConsoleEventName, ConsoleEventColor, message);
            }

            return result;
        }

        private BuyFailureReason RestoreInternal()
        {
            // 課金システムが未初期化の場合はなにもしない.
            if (!IsPurchaseReady)
            {
                return BuyFailureReason.NotInitialization;
            }

            // コールバックが通知できない場合は何もしない.
            if (onStorePurchaseRestore == null || !onStorePurchaseRestore.HasObservers)
            {
                return BuyFailureReason.NotReceiveMessage;
            }

            // 通信不可の場合は何もしない.
            if (!NetworkConnection())
            {
                return BuyFailureReason.NetworkUnavailable;
            }

            // 各ストア用のリストア処理.
            if (storePurchasing != null)
            {
                storePurchasing.OnRestore();
            }

            try
            {
                // 購入済みアイテムを通知.
                foreach (var pendingProduct in PendingProducts)
                {
                    if (onStorePurchaseRestore != null)
                    {
                        onStorePurchaseRestore.OnNext(pendingProduct);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);

                // 何らかのエラーが発生.
                return BuyFailureReason.Unknown;
            }

            return BuyFailureReason.None;
        }

        #endregion

        /// <summary>
        /// ストアの商品情報更新通知.
        /// </summary>
        /// <returns></returns>
        public IObservable<Product[]> OnStoreProductsUpdateAsObservable()
        {
            return onStoreProductsUpdate ?? (onStoreProductsUpdate = new Subject<Product[]>());
        }

        /// <summary>
        /// ストア購入の結果通知.
        /// </summary>
        /// <returns></returns>
        public IObservable<PurchaseResult> OnStorePurchaseCompleteAsObservable()
        {
            return onStorePurchaseComplete ?? (onStorePurchaseComplete = new Subject<PurchaseResult>());
        }

        /// <summary>
        /// ストア購入復元を通知.
        /// </summary>
        /// <returns></returns>
        public IObservable<Product> OnStorePurchaseRestoreAsObservable()
        {
            return onStorePurchaseRestore ?? (onStorePurchaseRestore = new Subject<Product>());
        }

        /// <summary>
        /// Pending状態のアイテムを更新.
        /// </summary>
        private void UpdatePendingProduct(Product product, PurchaseProcessingResult result)
        {
            // レシートを持っていない.
            if (!product.hasReceipt) { return; }

            if (string.IsNullOrEmpty(product.transactionID)) { return; }

            var pendingProducts = new List<Product>(PendingProducts);

            // Pendingの場合は最新のものに更新.
            if (result == PurchaseProcessingResult.Pending)
            {
                var pendingProduct = pendingProducts.FirstOrDefault(x => x.transactionID == product.transactionID);

                if (pendingProduct != null)
                {
                    pendingProducts.Remove(pendingProduct);
                }

                pendingProducts.Add(product);
            }
            else if (result == PurchaseProcessingResult.Complete)
            {
                // 完了した場合は削除.
                var pendingProduct = pendingProducts.FirstOrDefault(x => x.transactionID == product.transactionID);

                if (pendingProduct != null)
                {
                    pendingProducts.Remove(pendingProduct);
                }

                storeController.ConfirmPendingPurchase(product);

                var logBuilder = new StringBuilder();

                logBuilder.AppendLine("------- ConfirmPendingProducts -------");
                logBuilder.AppendLine(GetProductString(product)).AppendLine();

                UnityConsole.Event(ConsoleEventName, ConsoleEventColor, logBuilder.ToString());
            }

            PendingProducts = pendingProducts.ToArray();
        }

        /// <summary>
        /// 通信接続があるかチェックします。
        /// </summary>
        /// <returns><c>true</c>の場合は通信接続がある</returns>
        private static bool NetworkConnection()
        {
            return Application.internetReachability != NetworkReachability.NotReachable;
        }

        #region IStoreListener

        /// <summary>
        /// IStoreListenerの初期化完了通知.
        /// </summary>
        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            storeController = controller;
            storeExtensionProvider = extensions;

            // 各ストアの拡張処理.
            if (storePurchasing != null)
            {
                storePurchasing.OnStoreListenerInitialized(controller, extensions);
            }

            // ストアに販売中のアイテムを更新.
            StoreProducts = controller.products.all;

            if (StoreProducts.Any())
            {
                var logBuilder = new StringBuilder();

                logBuilder.AppendLine("------- StoreProducts -------");

                foreach (var item in StoreProducts)
                {
                    logBuilder.AppendLine(GetProductString(item)).AppendLine();
                }

                UnityConsole.Event(ConsoleEventName, ConsoleEventColor, logBuilder.ToString());
            }

            // Pending状態のアイテムを更新.
            foreach (var product in controller.products.all)
            {
                UpdatePendingProduct(product, PurchaseProcessingResult.Pending);
            }

            if (PendingProducts.Any())
            {
                var logBuilder = new StringBuilder();

                logBuilder.AppendLine("------- PendingProducts -------");

                foreach (var item in PendingProducts)
                {
                    logBuilder.AppendLine(GetProductString(item)).AppendLine();
                }

                UnityConsole.Event(ConsoleEventName, ConsoleEventColor, logBuilder.ToString());
            }

            if (onStoreProductsUpdate != null)
            {
                onStoreProductsUpdate.OnNext(StoreProducts);
            }

            IsPurchaseReady = true;
        }

        /// <summary> IStoreListenerの初期化失敗通知. </summary>
        public void OnInitializeFailed(InitializationFailureReason error)
        {
            IsPurchaseReady = false;
            
            var logMessage = $"InitializeFailed. {error}";

            UnityConsole.Event(ConsoleEventName, ConsoleEventColor, logMessage);
        }

        /// <summary> IStoreListenerの初期化失敗通知. </summary>
        public void OnInitializeFailed(InitializationFailureReason error, string message)
        {
            IsPurchaseReady = false;
            
            var logMessage = $"InitializeFailed. {error}\n\n{message}";

            UnityConsole.Event(ConsoleEventName, ConsoleEventColor, logMessage);
        }

        /// <summary>
        /// IStoreListenerのアプリ内課金成功の通知.
        /// </summary>
        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
        {
            IsPurchaseing = false;

            var product = args.purchasedProduct;

            // 通信前提のため一度Pendingに追加.
            UpdatePendingProduct(product, PurchaseProcessingResult.Pending);

            // 通知できない場合はここで処理を終了.
            if (onStorePurchaseComplete == null || !onStorePurchaseComplete.HasObservers)
            {
                return PurchaseProcessingResult.Pending;
            }

            // 初期化時に登録されていないアイテムの場合（アプリの不具合・サーバの設定ミス等）.
            if (product.definition.id == null)
            {
                onStorePurchaseComplete.OnNext(new PurchaseResult(product, PurchaseFailureReason.ProductUnavailable));

                return PurchaseProcessingResult.Pending;
            }

            // コンビニ決済未完了のレシート.

            var googlePlayStorePurchasing = storePurchasing as GooglePlayStorePurchasing;

            if (googlePlayStorePurchasing != null)
            {
                if (googlePlayStorePurchasing.GooglePlayStoreExtensions != null)
                {
                    if (googlePlayStorePurchasing.GooglePlayStoreExtensions.IsPurchasedProductDeferred(product))
                    {
                        return PurchaseProcessingResult.Pending;
                    }
                }
            }

            // アプリの強制終了にも耐えうるようにする.
            try
            {
                // アイテムの購入完了処理.
                // ※ 過去に購入した現在は販売していないアイテムが未消費の可能性がある為未登録のアイテムの除外はしない.
                if (onStorePurchaseComplete != null)
                {
                    onStorePurchaseComplete.OnNext(new PurchaseResult(product, null));
                }
            }
            catch (Exception)
            {
                // 不明なエラーが発生.
                // ※ 成功通知時に強制終了している場合もここで通知されるので、レシートの有無で判断する.
                if (onStorePurchaseComplete != null)
                {
                    onStorePurchaseComplete.OnNext(new PurchaseResult(product, PurchaseFailureReason.Unknown));
                }
            }

            return PurchaseProcessingResult.Pending;
        }

        /// <summary>
        /// IStoreListenerアプリ内課金の失敗の通知.
        /// </summary>
        public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
        {
            IsPurchaseing = false;

            if (onStorePurchaseComplete != null)
            {
                onStorePurchaseComplete.OnNext(new PurchaseResult(product, failureReason));
            }

            var message = $"PurchaseFailed. ({failureReason})\n{GetProductString(product)}";

            UnityConsole.Event(ConsoleEventName, ConsoleEventColor, message, LogType.Error);
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureDescription failureDescription)
        {
            OnPurchaseFailed(product, failureDescription.reason);
        }

        #endregion

        private static string GetProductString(Product product)
        {
            var builder = new StringBuilder();

            builder.AppendFormat("id: {0}", product.definition.storeSpecificId).AppendLine();
            builder.AppendFormat("type: {0}", product.definition.type).AppendLine();
            builder.AppendFormat("title: {0}", product.metadata.localizedTitle).AppendLine();
            builder.AppendFormat("price: {0}", product.metadata.localizedPrice).AppendLine();
            builder.AppendFormat("receipt: {0}", product.receipt).AppendLine();

            return builder.ToString();
        }

        /// <summary>
        /// 課金アイテムリストを取得.
        /// </summary>
        /// <returns></returns>
        protected abstract UniTask<ProductDefinition[]> FetchProducts();
    }
}

#endif
