
#if UNITY_EDITOR

using UnityEngine;
using UnityEngine.UI;
using UnityEditor.Callbacks;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using Extensions;

namespace Modules.GameText.Components
{
    public partial class GameTextSetter
    {
        //----- params -----

        private const string AESKey = "5k7DpFGc9A9iRaLkv2nCdMxCmjHFzxOX";
        private const string AESIv = "FiEA3x1fs8JhK9Tp";

        public const char DevelopmentMark = '#';

        //----- field -----

        #pragma warning disable 0414

        [SerializeField, HideInInspector]
        private string developmentText = null;

        #pragma warning restore 0414

        private static AesManaged aesManaged = null;

        //----- property -----

        //----- method -----

        public string GetDevelopmentText()
        {
            if (aesManaged == null)
            {
                aesManaged = AESExtension.CreateAesManaged(AESKey, AESIv);
            }

            if (developmentText == null) { return string.Empty; }

            return string.Format("{0}{1}", DevelopmentMark, developmentText.Decrypt(aesManaged));
        }

        private void SetDevelopmentText(string text)
        {
            if (aesManaged == null)
            {
                aesManaged = AESExtension.CreateAesManaged(AESKey, AESIv);
            }

            developmentText = text == null ? string.Empty : text.Encrypt(aesManaged);
        }

        private void ApplyDevelopmentText()
        {
            if (string.IsNullOrEmpty(developmentText)) { return; }

            if (Category == GameTextCategory.None)
            {
                var text = GetDevelopmentText();

                ApplyText(text);
            }
        }
    }
}

#endif
