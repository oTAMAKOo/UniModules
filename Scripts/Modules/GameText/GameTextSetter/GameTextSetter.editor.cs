
#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEditor;
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
            catch (Exception e)
            {
                developmentText = null;

                using (new DisableStackTraceScope())
                {
                    var hierarchyPath = UnityUtility.GetHierarchyPath(gameObject);

                    Debug.LogErrorFormat("DevelopmentText decrypt failed.\n{0}", hierarchyPath);
                }

                EditorUtility.SetDirty(this);
            }

            return string.IsNullOrEmpty(text) ? null : string.Format("{0}{1}", DevelopmentMark, text);
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

                using (new DisableStackTraceScope())
                {
                    var hierarchyPath = UnityUtility.GetHierarchyPath(gameObject);
                    
                    Debug.LogErrorFormat("DevelopmentText encrypt failed.\n{0}", hierarchyPath);
                }
                
                EditorUtility.SetDirty(this);
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
