
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;

namespace Modules.UI.TextColor
{
    [Serializable]
    public sealed class TextColorInfo
    {
        [SerializeField]
        public string name = null;

        [SerializeField]
        public string guid = null;

        [SerializeField]
        public Color textColor = Color.white;

        [SerializeField]
        public bool hasOutline = false;
        [SerializeField]
        public Color outlineColor = Color.white;

        [SerializeField]
        public bool hasShadow = false;
        [SerializeField]
        public Color shadowColor = Color.white;    
    }

    public sealed class TextColorSetting : ScriptableObject
    {
        //----- params -----

        //----- field -----

        [SerializeField]
        private TextColorInfo[] colorInfos = null;

        //----- property -----

        public TextColorInfo[] ColorInfos
        {
            get { return colorInfos ?? (colorInfos = new TextColorInfo[0]); }
        }

        //----- method -----

        public TextColorInfo GetTextColorInfo(string guid)
        {
            return ColorInfos.FirstOrDefault(x => x.guid == guid);
        }
    }
}
