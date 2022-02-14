
#if UNITY_PURCHASING

using System;
using UnityEngine;
using UnityEngine.Purchasing;
using UniRx;
using Extensions;

namespace Modules.InAppPurchasing
{
    public class GooglePlayStorePurchasing : IStorePurchasing
    {
        //----- params -----

        //----- field -----

        private IGooglePlayStoreExtensions googleExtensions = null;

        private Subject<Product> onDeferredPurchase = null;

        //----- property -----

        //----- method -----

        public void Initialize()
        {
            var purchasingModule = StandardPurchasingModule.Instance();

            var configurationBuilder = ConfigurationBuilder.Instance(purchasingModule);

            if (configurationBuilder != null)
            {
                var googlePlayConfiguration = configurationBuilder.Configure<IGooglePlayConfiguration>();

                if (googlePlayConfiguration != null)
                {
                    Action<Product> onDeferredPurchaseCallback = product =>
                    {
                        OnDeferredPurchase(product);

                        if (onDeferredPurchase != null)
                        {
                            onDeferredPurchase.OnNext(product);
                        }
                    };

                    googlePlayConfiguration.SetDeferredPurchaseListener(onDeferredPurchaseCallback);
                }
            }
        }

        public string GetStoreName()
        {
            return GooglePlay.Name;
        }

        public virtual void OnStoreListenerInitialized(IStoreController controller, IExtensionProvider storeExtensionProvider)
        {
            googleExtensions = storeExtensionProvider.GetExtension<IGooglePlayStoreExtensions>();
        }

        public virtual void OnRestore() { }
        
        protected virtual void OnDeferredPurchase(Product product) { }

        public IObservable<Product> OnDeferredPurchaseAsObservable()
        {
            return onDeferredPurchase ?? (onDeferredPurchase = new Subject<Product>());
        }
    }
}

#endif