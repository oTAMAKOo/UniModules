
using System;
using UnityEngine;
using UnityEngine.UI;
using Extensions;
using TMPro;
using UniRx;

namespace Modules.TextData.Components
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed partial class TextSetter : MonoBehaviour
    {
        //----- params -----

        //----- field -----

        [SerializeField]
        private TextType type = TextType.Internal;
        [SerializeField]
        private string textGuid = null;

        private string content = null;

        private Text textComponent = null;

        private TextMeshProUGUI textMeshProComponent = null;

        //----- property -----

        public TextType Type { get { return type; } }

        public string TextGuid { get { return textGuid; } }

        public string Content
        {
            get
            {
                if (string.IsNullOrEmpty(textGuid)) { return null; }

                var textData = TextData.Instance;

                if (textData == null || textData.Texts == null) { return null; }

                if (string.IsNullOrEmpty(content))
                {
                    content = textData.FindText(textGuid);
                }

                return content;
            }
        }

        //----- method -----

        void Awake()
        {
            ImportText();

            if (Application.isPlaying)
            {
                // テキスト更新通知を受け取ったら再度テキストを適用.
                TextData.Instance.OnUpdateContentsAsObservable()
                    .TakeWhile(_ => !UnityUtility.IsNull(this))
                    .Subscribe(_ => ImportText())
                    .AddTo(this);
            }
        }

        public void Format(params object[] args)
        {
            ImportText();

            if (!string.IsNullOrEmpty(Content))
            {
                ApplyText(string.Format(Content, args));
            }
        }

        private void SetTextGuid(string guid)
        {
            var textData = TextData.Instance;

            if (textData == null) { return; }

            textGuid = string.IsNullOrEmpty(guid) ? null : guid.Trim();

            content = string.Empty;

            ApplyTextData();
        }

        private void ImportText()
        {
            if (Application.isBatchMode){ return; }

            try
            {
                #if UNITY_EDITOR

                ApplyDummyText();

                #endif

                ApplyTextData();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private void ApplyTextData()
        {
            var textData = TextData.Instance;

            if (textData == null || textData.Texts == null) { return; }

            if (string.IsNullOrEmpty(textGuid)) { return; }

            content = string.Empty;

            ApplyText(Content);
        }

        private void ApplyText(string text)
        {
            GetTargetComponent();
            
            if (textMeshProComponent != null)
            {
                textMeshProComponent.ForceMeshUpdate(true);
                textMeshProComponent.SetText(text);
            }

            if (textComponent != null)
            {
                textComponent.text = text;
            }
        }

        private string GetTargetText()
        {
            GetTargetComponent();

            if (textMeshProComponent != null)
            {
                return textMeshProComponent.text;
            }

            if (textComponent != null)
            {
                return textComponent.text;
            }

            return null;
        }

        private void GetTargetComponent()
        {
            if (textMeshProComponent == null)
            {
                textMeshProComponent = UnityUtility.GetComponent<TextMeshProUGUI>(gameObject);
            }

            if (textComponent == null)
            {
                textComponent = UnityUtility.GetComponent<Text>(gameObject);
            }
        }
    }
}
