
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
                aesCryptoKey = Reflection.GetPrivateField<GameText, AesCryptoKey>(GameText.Instance, "cryptoKey");
            }

            return aesCryptoKey;
        }

        private string GetDevelopmentText()
        {
            var cryptoKey = GetCryptoKey();

            if (string.IsNullOrEmpty(developmentText)) { return string.Empty; }

            var text = string.Empty;

            try
            {
                text = developmentText.Decrypt(cryptoKey);
            }
            catch
            {
                developmentText = null;

                Debug.LogError("DevelopmentText decrypt failed.");
            }

            return string.Format("{0}{1}", DevelopmentMark, text);
        }

        private void SetDevelopmentText(string text)
        {
            var cryptoKey = GetCryptoKey();

            try
            {
                developmentText = string.IsNullOrEmpty(text) ? string.Empty : text.Encrypt(cryptoKey);
            }
            catch
            {
                developmentText = null;
                
                Debug.LogError("DevelopmentText encrypt failed.");
            }

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
