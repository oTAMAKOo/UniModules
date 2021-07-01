
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using Extensions;

namespace Modules.UI.TextEffect
{
    public sealed class FontKerningSetting : ScriptableObject
    {
        //----- params -----

        [Serializable]
        public sealed class CharInfo
        {
            public char character = default(char);
            public float leftSpace = 0f;
            public float rightSpace = 0f;
        }

        //----- field -----

        [SerializeField]
        private Font font = null;
        [SerializeField]
        private CharInfo[] infos = new CharInfo[0];
        [SerializeField]
        private byte[] description = new byte[0];

        private Dictionary<char, CharInfo> dictionary = null;

        //----- property -----

        public Font Font { get { return font; } }

        //----- method ----- 

        public CharInfo GetCharInfo(char c)
        {
            if (dictionary == null)
            {
                dictionary = infos.Where(x => x.character != default(char)).ToDictionary(x => x.character);
            }

            return dictionary.GetValueOrDefault(c);
        }
    }
}
