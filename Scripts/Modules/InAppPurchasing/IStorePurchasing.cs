
#if UNITY_PURCHASING

using UnityEngine.Purchasing;

namespace Modules.InAppPurchasing
{
    public interface IStorePurchasing
    {
        void Initialize();

        void OnStoreListenerInitialized(IStoreController controller, IExtensionProvider storeExtensionProvider);
    }
}

#endif