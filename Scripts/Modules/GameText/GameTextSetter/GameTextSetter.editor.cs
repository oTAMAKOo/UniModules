
#if UNITY_EDITOR

using UnityEngine;
using UnityEngine.UI;
using Extensions;

namespace Modules.GameText.Components
{
    public partial class GameTextSetter
    {
        //----- params -----

        public const char DevelopmentMark = '#';

        //----- field -----

        #pragma warning disable 0414

        [SerializeField, HideInInspector]
        private string developmentText = null;

        #pragma warning restore 0414

        private static AesCryptoKey aesCryptoKey = null;

        //----- property -----

        //----- method -----

        private AesCryptoKey GetCryptoKey()
        {
            if (aesCryptoKey == null)
            {
                aesCryptoKey = new AesCryptoKey("5k7DpFGc9A9iRaLkv2nCdMxCmjHFzxOX", "FiEA3x1fs8JhK9Tp");
            }

            return aesCryptoKey;
        }

        private string GetDevelopmentText()
        {
            var cryptoKey = GetCryptoKey();

            if (string.IsNullOrEmpty(developmentText)) { return string.Empty; }

            return string.Format("{0}{1}", DevelopmentMark, developmentText.Decrypt(cryptoKey));
        }

        private void SetDevelopmentText(string text)
        {
            var cryptoKey = GetCryptoKey();

            developmentText = string.IsNullOrEmpty(text) ? string.Empty : text.Encrypt(cryptoKey);

            ImportText();
        }

        private void ApplyDevelopmentText()
        {
            if (Application.isPlaying) { return; }

            if (string.IsNullOrEmpty(developmentText)) { return; }

            if (!string.IsNullOrEmpty(textGuid)) { return; }

            var text = GetDevelopmentText();

            ApplyText(text);

            var rt = transform as RectTransform;

            if (rt != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
            }
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
