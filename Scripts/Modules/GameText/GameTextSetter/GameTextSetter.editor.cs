
#if UNITY_EDITOR

using System;
using UnityEngine;
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

        private static AesCryptoKey aesCryptoKey = null;

        //----- property -----

        //----- method -----

        private string GetDevelopmentText()
        {
            if (aesCryptoKey == null)
            {
                aesCryptoKey = new AesCryptoKey(AESKey, AESIv);
            }

            if (string.IsNullOrEmpty(developmentText)) { return string.Empty; }

            return string.Format("{0}{1}", DevelopmentMark, developmentText.Decrypt(aesCryptoKey));
        }

        private void SetDevelopmentText(string text)
        {
            if (aesCryptoKey == null)
            {
                aesCryptoKey = new AesCryptoKey(AESKey, AESIv);
            }

            developmentText = string.IsNullOrEmpty(text) ? string.Empty : text.Encrypt(aesCryptoKey);

            ImportText();
        }

        private void ApplyDevelopmentText()
        {
            if (Application.isPlaying) { return; }

            if (string.IsNullOrEmpty(developmentText)) { return; }

            if (!string.IsNullOrEmpty(textGuid)) { return; }

            var text = GetDevelopmentText();

            ApplyText(text);
        }

        private bool CleanDevelopmentText()
        {
            if (Application.isPlaying) { return false; }

            if (string.IsNullOrEmpty(developmentText)) { return false; }

            var text = GetDevelopmentText();

            var targetText = GetTargetText();

            if (targetText == text)
            {
                ApplyText(string.Empty);

                return true;
            }

            return false;
        }
    }
}

#endif
