
using UnityEngine;
using System;
using System.Collections.Generic;
using Extensions;

namespace Modules.TextData.Components
{
    public sealed class TextDataAsset : ScriptableObject
	{
        [Serializable]
        public sealed class TextContent
        {
            [SerializeField, ReadOnly]
            private string identifier = null;
            [SerializeField, ReadOnly]
            private string guid = null;
            [SerializeField, ReadOnly]
            private string enumName = null;
            [SerializeField, ReadOnly]
            private string text = null;

            public string Identifier { get { return identifier; } }

            public string Guid { get { return guid; } }

            public string EnumName { get { return enumName; } }

            public string Text { get { return text; } }

            public TextContent(string identifier, string guid, string enumName, string text)
            {
                this.identifier = identifier;
                this.guid = guid;
                this.enumName = enumName;
                this.text = text;
            }
        }

        [Serializable]
        public sealed class CategoryContent
        {
            [SerializeField, ReadOnly]
            private string guid = null;
            [SerializeField, ReadOnly]
            private string name = null;
            [SerializeField, ReadOnly]
            private string displayName = null;
            [SerializeField, ReadOnly]
            private TextContent[] texts = new TextContent[0];

            public string Guid { get { return guid; } }

            public string Name { get { return name; } }

            public string DisplayName { get { return displayName; } }

            public IReadOnlyList<TextContent> Texts { get { return texts; } }

            public CategoryContent(string guid, string name, string displayName, TextContent[] texts)
            {
                this.guid = guid;
                this.name = name;
                this.displayName = displayName;
                this.texts = texts;
            }
        }

        [SerializeField, ReadOnly]
        private ContentType contentType = ContentType.Embedded;
	    [SerializeField, ReadOnly]
	    private string hash = null;
        [SerializeField, HideInInspector]
        private CategoryContent[] categories = new CategoryContent[0];
        
        public ContentType ContentType { get { return contentType; } }

        public IReadOnlyList<CategoryContent> Contents { get { return categories; } }

	    public string Hash { get { return hash; } }

        public void SetContents(ContentType contentType, string hash, CategoryContent[] categories)
        {
            this.contentType = contentType;
            this.categories = categories;
            this.hash = hash;
        }
    }
}
