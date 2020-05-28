﻿﻿﻿
using System;
using UnityEngine;
using UnityEngine.UI;
using Extensions;
using TMPro;

namespace Modules.GameText.Components
{
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    public sealed partial class GameTextSetter : MonoBehaviour
	{
        //----- params -----

        public enum SetType
        {
            Selector,
            Direct,
        }

        //----- field -----

	    [SerializeField]
	    private SetType setType = SetType.Selector;
        [SerializeField]
        private string textGuid = null;
        [SerializeField]
        private string content = null;

        private Text textComponent = null;

	    private TextMeshProUGUI textMeshProComponent = null;

        //----- property -----
        
	    public string TextGuid { get { return textGuid; } }

        public string Content { get { return content; } }

        //----- method -----

        void Awake()
        {
            ImportText();
        }

        public void Format(params object[] args)
        {
            ImportText();

            if (!string.IsNullOrEmpty(content))
            {
                ApplyText(string.Format(content, args));
            }
        }

	    private void SetText(Enum textType)
        {
            var gameText = GameText.Instance;

            if (gameText == null) { return; }

            textGuid = textType == null ? null : gameText.FindTextGuid(textType);

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

            if (gameText == null || gameText.Cache == null) { return; }

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
