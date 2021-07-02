
#if UNITY_EDITOR

using UnityEngine;
using UnityEngine.UI;
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

        private static AesCryptKey aesCryptKey = null;

        //----- property -----

        //----- method -----

        private string GetDevelopmentText()
        {
            if (aesCryptKey == null)
            {
                aesCryptKey = new AesCryptKey(AESKey, AESIv);
            }

            if (string.IsNullOrEmpty(developmentText)) { return string.Empty; }

            return string.Format("{0}{1}", DevelopmentMark, developmentText.Decrypt(aesCryptKey));
        }

        private void SetDevelopmentText(string text)
        {
            if (aesCryptKey == null)
            {
                aesCryptKey = new AesCryptKey(AESKey, AESIv);
            }

            developmentText = string.IsNullOrEmpty(text) ? string.Empty : text.Encrypt(aesCryptKey);

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
