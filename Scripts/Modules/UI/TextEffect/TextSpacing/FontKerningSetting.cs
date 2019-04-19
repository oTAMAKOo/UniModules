
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using Extensions;

namespace Modules.UI.TextEffect
{
    public class FontKerningSetting : ScriptableObject
    {
        //----- params -----

        [Serializable]
        public class CharInfo
        {
            public char character = default(char);
            public float leftSpace = 0f;
            public float rightSpace = 0f;
        }

        //----- field -----

        [SerializeField]
        private Font font = null;
        [SerializeField]
        private CharInfo[] infos = null;

        private Dictionary<char, CharInfo> dictionary = null;

        //----- property -----

        public Font Font { get { return font; } }

        //----- method -----

        public CharInfo GetCharInfo(char c)
        {
            if (dictionary == null)
            {
                dictionary = infos != null ?
                             infos.Where(x => x.character != default(char)).ToDictionary(x => x.character) :
                             new Dictionary<char, CharInfo>();
            }

            return dictionary.GetValueOrDefault(c);
        }
    }
}
