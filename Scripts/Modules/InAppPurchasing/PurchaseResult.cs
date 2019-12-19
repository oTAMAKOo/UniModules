
#if UNITY_PURCHASING

using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Security;
using System.Collections;
using System.Text;

namespace Modules.InAppPurchasing
{
    public sealed class PurchaseResult
    {
        //----- params -----

        //----- field -----

        /// <summary> プロダクト </summary>
        public Product Product { get; private set; }

        /// <summary> 課金失敗時のエラー </summary>
        public PurchaseFailureReason? Reason { get; private set; }

        /// <summary> 検証結果 </summary>
        public bool Validate { get; private set; }

        //----- property -----

        //----- method -----

        public PurchaseResult(Product product, PurchaseFailureReason? reason)
        {
            Product = product;
            Reason = reason;
            Validate = ValidateReceipt(product.receipt);

            if(!Validate)
            {
                Debug.LogError("Receipt validate error.");
            }
        }

        private static bool ValidateReceipt(string receipt)
        {
            #if UNITY_PURCHASING_VALIDATE_RECEIPT

            #if UNITY_ANDROID || UNITY_IOS || UNITY_STANDALONE_OSX

            var googlePlayTangle = GooglePlayTangle.Data();
            var appleTangle = AppleTangle.Data();

            var validator = new CrossPlatformValidator(googlePlayTangle, appleTangle, Application.identifier);

            try
            {
                var result = validator.Validate(receipt);

                var builder = new StringBuilder();

                builder.AppendLine("Receipt is valid. Contents:");

                foreach (var productReceipt in result)
                {
                    builder.AppendFormat("productID: {0}", productReceipt.productID).AppendLine();
                    builder.AppendFormat("purchaseDate: {0}", productReceipt.purchaseDate).AppendLine();
                    builder.AppendFormat("transactionID: {0}", productReceipt.transactionID).AppendLine();
                }

                Debug.Log(builder.ToString());
            }
            catch (IAPSecurityException)
            {
                Debug.LogError("[PurchaseManager] Invalid receipt, not unlocking content.");
                return false;
            }

            #endif

            #endif

            return true;
        }
    }
}

#endif
