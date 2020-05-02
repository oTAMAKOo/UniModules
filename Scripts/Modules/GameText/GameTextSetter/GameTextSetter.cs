﻿﻿﻿
using System;
using UnityEngine;
using UnityEngine.UI;
using Extensions;

namespace Modules.GameText.Components
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(Text))]
    public sealed partial class GameTextSetter : MonoBehaviour
	{
        //----- params -----

        //----- field -----

        [SerializeField]
        private string categoryGuid = null;
        [SerializeField]
        private string textGuid = null;
        [SerializeField]
        private string content = null;

        private Text textObject = null;

        //----- property -----

        public string CategoryGuid { get { return categoryGuid; } }

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

        public void ChangeCategory(Enum newCategory)
        {
            var gameText = GameText.Instance;

            if (gameText == null) { return; }
            
            var newCategoryGuid = gameText.FindCategoryGuid(newCategory);

            if (categoryGuid != newCategoryGuid)
            {
                categoryGuid = newCategoryGuid;
                SetText(null);
            }
        }

        public void SetText(Enum textType)
        {
            var gameText = GameText.Instance;

            if (gameText == null) { return; }

            textGuid = textType == null ? null : gameText.FindTextGuid(textType);

            content = string.Empty;

            ApplyText(content);
            ApplyGameText();
        }

        public void ImportText()
        {
            #if UNITY_EDITOR

            ApplyDevelopmentText();

            #endif

            ApplyGameText();
        }

        private void ApplyGameText()
        {
            var gameText = GameText.Instance;

            if (gameText == null) { return; }

            if (gameText.Cache == null) { return; }

            if (string.IsNullOrEmpty(textGuid))
            {
                content = string.Empty;
            }
            else
            {
                content = gameText.Cache.GetValueOrDefault(textGuid);
            }

            ApplyText(content);
        }

        private void ApplyText(string text)
        {
            if (textObject == null)
            {
                textObject = UnityUtility.GetComponent<Text>(gameObject);
            }

            if (textObject != null)
            {
                textObject.text = text;
            }
        }
    }
}
