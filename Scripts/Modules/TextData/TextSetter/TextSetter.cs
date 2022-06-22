﻿
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

	    #pragma warning disable 414

        [SerializeField]
	    private ContentType type = ContentType.Embedded;

        #pragma warning restore 414

        [SerializeField]
        private string textGuid = null;
        [SerializeField]
        private string content = null;

        private Text textComponent = null;

	    private TextMeshProUGUI textMeshProComponent = null;

        //----- property -----

        public ContentType ContentType { get { return type; } }

        public string TextGuid { get { return textGuid; } }

        public string Content { get { return content; } }

        //----- method -----

        void Awake()
        {
            ImportText();

            if (Application.isPlaying)
            {
                // テキスト更新通知を受け取ったら再度テキストを適用.
                TextData.Instance.OnUpdateContentsAsObservable()
                    .Subscribe(_ => ImportText())
                    .AddTo(this);
            }
        }

        public void Format(params object[] args)
        {
            ImportText();

            if (!string.IsNullOrEmpty(content))
            {
                ApplyText(string.Format(content, args));
            }
        }

        private void SetTextGuid(string guid)
        {
            var textData = TextData.Instance;

            if (textData == null) { return; }

            if (string.IsNullOrEmpty(guid))
            {
                textGuid = null;
            }
            else
            {
                guid = guid.Trim();

                var text = textData.FindText(guid);

                if (!string.IsNullOrEmpty(text))
                {
                    textGuid = guid;
                }
            }

            content = string.Empty;

            ApplyText(content);
            ApplyTextData();
        }

        private void ImportText()
        {
			if (Application.isBatchMode){ return; }

            #if UNITY_EDITOR

            ApplyDevelopmentText();

            #endif

            ApplyTextData();
        }

        private void ApplyTextData()
        {
            var textData = TextData.Instance;

            if (textData == null || textData.Texts == null) { return; }

            if (string.IsNullOrEmpty(textGuid)) { return; }

            content = textData.FindText(textGuid);

            ApplyText(content);
        }

        private void ApplyText(string text)
        {
            GetTargetComponent();

            if (textComponent != null)
            {
                textComponent.text = text;
            }

            if (textMeshProComponent != null)
            {
                textMeshProComponent.ForceMeshUpdate(true);
                textMeshProComponent.SetText(text);
            }
        }

	    private string GetTargetText()
	    {
	        GetTargetComponent();

	        if (textComponent != null)
	        {
	            return textComponent.text;
	        }

	        if (textMeshProComponent != null)
	        {
                return textMeshProComponent.text;
            }

            return null;
	    }

        private void GetTargetComponent()
	    {
	        if (textComponent == null)
	        {
	            textComponent = UnityUtility.GetComponent<Text>(gameObject);
	        }

	        if (textMeshProComponent == null)
	        {
	            textMeshProComponent = UnityUtility.GetComponent<TextMeshProUGUI>(gameObject);
	        }
	    }
    }
}
