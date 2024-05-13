
#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using System;
using Cysharp.Threading.Tasks;
using Extensions;

namespace Modules.TextData.Components
{
    public partial class TextSetter
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

        void OnEnable()
        {
            EditModeImportText();
        }

        void OnDisable()
        { 
            CleanDummyText();
        }

        private void EditModeImportText()
        {
            if (Application.isBatchMode){ return; }

            if (Application.isPlaying){ return; }

            GetTargetComponent();

            if (textMeshProComponent == null && textComponent == null){ return; }

            var hasText = !dummyText.IsEmpty() || !textGuid.IsEmpty();

            if (!hasText){ return; }

            var exit = false;

            var task = UniTask.Defer(async () =>
            {
                while (!exit)
                {
                    if (UnityUtility.IsNull(gameObject)) { return; }

                    if (!UnityUtility.IsActiveInHierarchy(gameObject)) { return; }

                    ImportText();

                    exit |= textComponent != null && !string.IsNullOrEmpty(textComponent.text);
                    exit |= textMeshProComponent != null && !string.IsNullOrEmpty(textMeshProComponent.text);

                    await UniTask.Delay(TimeSpan.FromSeconds(0.5f), DelayType.Realtime);
                }
            });

            task.Forget();
        }

        private AesCryptoKey GetCryptoKey()
        {
            if (aesCryptoKey == null)
            {
                aesCryptoKey = Reflection.GetPrivateField<TextData, AesCryptoKey>(TextData.Instance, "cryptoKey");
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

            if (BuildPipeline.isBuildingPlayer) { return; }

            if (!UnityUtility.IsActiveInHierarchy(gameObject)) { return; }

            if (string.IsNullOrEmpty(dummyText)) { return; }

            if (!string.IsNullOrEmpty(textGuid)) { return; }

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

            if (BuildPipeline.isBuildingPlayer) { return false; }

            if (string.IsNullOrEmpty(dummyText)) { return false; }

            var text = GetDummyText();

            var targetText = GetTargetText();

            if (targetText == text)
            {
                ApplyText(string.Empty);

                SetDirty();

                return true;
            }

            return false;
        }

        private void SetDirty()
        {
            if (textMeshProComponent != null)
            {
                EditorUtility.SetDirty(textMeshProComponent);
            }

            if (textComponent != null)
            {
                EditorUtility.SetDirty(textComponent);
            }
        }
    }
}

#endif
