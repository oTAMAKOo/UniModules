﻿﻿﻿
using UnityEngine;
using Extensions.Serialize;
using UnityEngine.UI;

namespace Modules.GameText.Components
{
    [ExecuteInEditMode]
    public sealed class GameTextSetter : MonoBehaviour
	{
        //----- params -----

        //----- field -----

        [SerializeField]
        private GameTextCategory category = GameTextCategory.None;
        [SerializeField]
        private IntNullable identifier = new IntNullable(0);
        [SerializeField]
        private string content = null;

        private Text textObject = null;

        //----- property -----

        public GameTextCategory Category { get { return category; } }
        public IntNullable Identifier { get { return identifier; } }
        public string Content { get { return content; } }

        //----- method -----

        void Awake()
        {
            textObject = gameObject.GetComponent<Text>();

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

        public void SetCategory(GameTextCategory newCategory)
        {
            if (category != newCategory)
            {
                category = newCategory;
                SetCategoryId(null);
            }
        }

        public void SetCategoryId(int? id)
        {
            identifier = id;
            content = string.Empty;

            ApplyText(content);
            ImportText();
        }

        public void ImportText()
        {
            if (!GameText.Exists) { return; }

            if (category == GameTextCategory.None || !identifier.HasValue) { return; }

            content = GameText.Instance.Find(category, identifier.Value);

            ApplyText(content);
        }

        private void ApplyText(string text)
        {
            if (textObject != null)
            {
                textObject.text = text;
            }
        }
    }
}
