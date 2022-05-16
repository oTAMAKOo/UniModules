
using UnityEngine;
using Modules.Devkit.ScriptableObjects;

namespace Modules.Devkit.ChatWork
{
    public sealed class ChatWorkNotifyConfig : ReloadableScriptableObject<ChatWorkNotifyConfig>
    {
        //----- params -----

        //----- field -----

        [SerializeField]
        private string apiToken = null;
        [SerializeField]
        private ulong roomId = 0;

        //----- property -----

        public string ApiToken { get { return apiToken; } }

        public ulong RoomId { get { return roomId; } }

        //----- method -----
    }
}
