
#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using Extensions;

namespace Modules.UI.DummyContent
{
    public sealed partial class DummyText
    {
        //----- params -----

        public const char DummyMark = '#';

        //----- field -----

        #pragma warning disable 0414

        [SerializeField, HideInInspector]
        private string dummyText = null;

        #pragma warning restore 0414

        private static AesCryptoKey aesCryptoKey = null;

        //----- property -----

        //----- method -----

        private AesCryptoKey GetCryptoKey()
        {
            if (aesCryptoKey == null)
            {
                aesCryptoKey = new AesCryptoKey(GetType().FullName);
            }

            return aesCryptoKey;
        }

        private string GetDummyText()
        {
            var cryptoKey = GetCryptoKey();

            if (string.IsNullOrEmpty(dummyText)) { return string.Empty; }

            var text = string.Empty;

            try
            {
                text = dummyText.Decrypt(cryptoKey);
            }
            catch
            {
                using (new DisableStackTraceScope())
                {
                    var hierarchyPath = UnityUtility.GetHierarchyPath(gameObject);

                    Debug.LogErrorFormat("dummyText decrypt failed.\n{0}", hierarchyPath);

                    dummyText = null;
                }

                EditorUtility.SetDirty(this);
            }

            return string.IsNullOrEmpty(text) ? null : string.Format("{0}{1}", DummyMark, text);
        }

        private void SetDummyText(string text)
        {
            var cryptoKey = GetCryptoKey();

            try
            {
                dummyText = string.IsNullOrEmpty(text) ? string.Empty : text.Encrypt(cryptoKey);
            }
            catch
            {
                using (new DisableStackTraceScope())
                {
                    var hierarchyPath = UnityUtility.GetHierarchyPath(gameObject);
                    
                    Debug.LogErrorFormat("dummyText encrypt failed.\n{0}", hierarchyPath);

                    dummyText = null;
                }
            }

            ImportText();
        }

        private void ApplyDummyText()
        {
            if (Application.isPlaying) { return; }

            if (string.IsNullOrEmpty(dummyText)) { return; }

			var text = GetDummyText();

            ApplyText(text);

            var rt = transform as RectTransform;

            if (rt != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
            }
        }

        private bool CleanDummyText()
        {
            if (Application.isPlaying) { return false; }

            if (string.IsNullOrEmpty(dummyText)) { return false; }

            var text = GetDummyText();

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
