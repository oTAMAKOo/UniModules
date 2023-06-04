
using UnityEngine;
using Modules.Devkit.JsonFile;
using Modules.Devkit.ScriptableObjects;

namespace Modules.Devkit.ValidateAsset.UnityWarning
{
    public sealed class UnityWarningChatWorkNotifyConfig : ReloadableScriptableObject<UnityWarningChatWorkNotifyConfig>
    {
        //----- params -----

        public sealed class JsonData
        {
            public ulong RoomId { get; set; } = 0;

            public string ApiToken { get; set; } = null;
        }

        //----- field -----

        [SerializeField]
        private JsonFileLoader jsonFileLoader = null;

        //----- property -----

        //----- method -----

        public JsonData LoadSettingJson()
        {
            return jsonFileLoader.Load<JsonData>();
        }
    }
}