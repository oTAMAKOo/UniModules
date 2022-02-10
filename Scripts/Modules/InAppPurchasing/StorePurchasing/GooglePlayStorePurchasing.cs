
#if UNITY_PURCHASING

using UnityEngine;
using UnityEngine.Purchasing;
using Extensions;

namespace Modules.InAppPurchasing
{
    public class GooglePlayStorePurchasing : IStorePurchasing
    {
        //----- params -----

        //----- field -----

        private IGooglePlayStoreExtensions googleExtensions = null;

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
                    googlePlayConfiguration.SetDeferredPurchaseListener(OnDeferredPurchase);
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
        
        protected virtual void OnDeferredPurchase(Product product)
        {
            Debug.Log($"Purchase of {product.definition.id} is deferred");
        }
    }
}

#endif