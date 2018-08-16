
using UnityEngine;
using Modules.Devkit.ScriptableObjects;

namespace Modules.Devkit.Spreadsheet
{
    public class SpreadsheetConfig : ReloadableScriptableObject<SpreadsheetConfig>
    {
        //----- params -----

        //----- field -----

        [SerializeField]
        private string clientId = string.Empty;
        [SerializeField]
        private string clientSecret = string.Empty;
        [SerializeField]
        private string redirectUri = string.Empty;
        [SerializeField]
        private string scope = string.Empty;

        //----- property -----

        public string ClientId { get { return clientId; } }
        public string ClientSecret { get { return clientSecret; } }
        public string RedirectUri { get { return redirectUri; } }
        public string Scope { get { return scope; } }

        //----- method -----
    }
}