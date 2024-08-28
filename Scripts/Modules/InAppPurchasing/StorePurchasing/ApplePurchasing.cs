
#if UNITY_PURCHASING

using UnityEngine;
using UnityEngine.Purchasing;
using System;
using UniRx;

namespace Modules.InAppPurchasing
{
    public class ApplePurchasing : IStorePurchasing
    {
        //----- params -----

        //----- field -----

        protected IAppleConfiguration appleConfiguration = null;
        protected IAppleExtensions appleExtensions = null;

        private Subject<bool> onRestoreFinish = null;

        //----- property -----

        //----- method -----

        public void Initialize()
        {
            var purchasingModule = StandardPurchasingModule.Instance();

            var configurationBuilder = ConfigurationBuilder.Instance(purchasingModule);

            if (configurationBuilder != null)
            {
                appleConfiguration = configurationBuilder.Configure<IAppleConfiguration>();
            }
        }

        public string GetStoreName()
        {
            return AppleAppStore.Name;
        }

        public virtual void OnStoreListenerInitialized(IStoreController controller, IExtensionProvider storeExtensionProvider)
        {
            appleExtensions = storeExtensionProvider.GetExtension<IAppleExtensions>();
        }

        public virtual void OnRestore()
        {
            if (appleExtensions != null)
            {
                void RestoreTransactionsCallback(bool result, string message)
                {
                    Debug.Log($"[ApplePurchasing] OnRestore : {message}");

                    if (onRestoreFinish != null)
                    {
                        onRestoreFinish.OnNext(result);
                    }
                }

                appleExtensions.RestoreTransactions(RestoreTransactionsCallback);
            }
        }

        /// <summary>
        /// リストア処理通知.
        /// trueの場合はなにかがリストアされたというわけではありません.
        /// 単にリストアの処理が終了したということです.
        /// </summary>
        public IObservable<bool> OnRestoreFinishAsObservable()
        {
            return onRestoreFinish ?? (onRestoreFinish = new Subject<bool>());
        }
    }
}

#endif