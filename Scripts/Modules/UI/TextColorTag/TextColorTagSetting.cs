
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using Extensions;

namespace Modules.UI.TextColorTag
{
    [Serializable]
    public sealed class TextColorTagInfo
    {
        [SerializeField]
        public string tag = null;
        [SerializeField]
        public Color color = Color.white;
    }

    public sealed class TextColorTagSetting : ScriptableObject
    {
        //----- params -----

        //----- field -----

        [SerializeField]
        private TextColorTagInfo[] colorTagInfos = null;

        //----- property -----

        //----- method -----

        public TextColorTagInfo[] GeTextColorTagInfos()
        {
            return colorTagInfos ?? (colorTagInfos = new TextColorTagInfo[0]);
        }
    }
}
