
using UnityEngine;
using System;
using System.Collections.Generic;
using Extensions;

namespace Modules.GameText.Components
{
    [Serializable]
    public sealed class TextContent
    {
        [SerializeField, ReadOnly]
        private string guid = null;
        [SerializeField, ReadOnly]
        private string text = null;

        public string Guid { get { return guid; } }

        public string Text { get { return text; } }

        public TextContent(string guid, string text)
        {
            this.guid = guid;
            this.text = text;
        }
    }

    public sealed class GameTextAsset : ScriptableObject
	{
        [SerializeField, HideInInspector]
        private TextContent[] contents = new TextContent[0];

	    public IReadOnlyList<TextContent> Contents { get { return contents; } }

        public void SetContents(TextContent[] contents)
        {
            this.contents = contents;
        }
    }
}
