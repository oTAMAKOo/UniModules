
using System;
using System.Collections.Generic;

using Object = UnityEngine.Object;

namespace Modules.ExternalResource.Editor
{
    [Serializable]
    public sealed class GroupInfo
    {
        /// <summary> グループ名 </summary>
        public string groupName = null;

        /// <summary> 所属アセット </summary>
        public List<Object> manageTargetAssets = new List<Object>();

        public GroupInfo(string groupName)
        {
            this.groupName = groupName;
        }
    }
}
