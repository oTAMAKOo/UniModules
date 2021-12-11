﻿﻿﻿
using UnityEngine;
using UnityEngine.UI;
using Extensions;
using TMPro;
using UniRx;

namespace Modules.GameText.Components
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed partial class GameTextSetter : MonoBehaviour
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
                GameText.Instance.OnUpdateContentsAsObservable()
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
            var gameText = GameText.Instance;

            if (gameText == null) { return; }

            if (string.IsNullOrEmpty(guid))
            {
                textGuid = null;
            }
            else
            {
                guid = guid.Trim();

                var text = gameText.FindText(guid);

                if (!string.IsNullOrEmpty(text))
                {
                    textGuid = guid;
                }
            }

            content = string.Empty;

            ApplyText(content);
            ApplyGameText();
        }

        private void ImportText()
        {
            #if UNITY_EDITOR

            ApplyDevelopmentText();

            #endif

            ApplyGameText();
        }

        private void ApplyGameText()
        {
            var gameText = GameText.Instance;

            if (gameText == null || gameText.Texts == null) { return; }

            if (string.IsNullOrEmpty(textGuid)) { return; }

            content = gameText.FindText(textGuid);

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
