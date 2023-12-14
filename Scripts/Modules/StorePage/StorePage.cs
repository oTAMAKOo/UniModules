
using UnityEngine;

namespace Modules.StorePage
{
    public static class StorePage
    {
        //----- params -----

        private const string AndroidStoreUrlFormat =  "market://details?id={0}";

        private const string iOSStoreUrlFormat =  "itms-apps://itunes.apple.com/app/id{0}?mt=8";

        //----- field -----

        //----- property -----

        public static string AppIdentifier { get; private set; }

        public static string StorePageUrl { get; private set; }

        //----- method -----

        public static void SetAppIdentifier(string identifier)
        {
            AppIdentifier = identifier;

            switch (Application.platform)
            {
                case RuntimePlatform.Android:
                    StorePageUrl = string.Format(AndroidStoreUrlFormat, AppIdentifier);
                    break;
                case RuntimePlatform.IPhonePlayer:
                    StorePageUrl = string.Format(iOSStoreUrlFormat, AppIdentifier);
                    break;
            }
        }

        public static void OpenStorePage()
        {
            switch (Application.platform)
            {
                case RuntimePlatform.Android:
                    Application.OpenURL(StorePageUrl);
                    break;
                case RuntimePlatform.IPhonePlayer:
                    Application.OpenURL(StorePageUrl);
                    break;
            }
        }
    }
}