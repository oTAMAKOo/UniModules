
#if UNITY_PURCHASING

using UnityEngine.Purchasing;

namespace Modules.InAppPurchasing
{
    public interface IStorePurchasing
    {
        void Initialize();

        string GetStoreName();

        void OnStoreListenerInitialized(IStoreController controller, IExtensionProvider storeExtensionProvider);

        void OnRestore();
    }
}

#endif