
using UnityEngine;
using System;
using System.Collections.Generic;
using Extensions;
using Extensions.Serialize;

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
	    [SerializeField, ReadOnly]
	    private LongNullable updateAt = null;

        public IReadOnlyList<TextContent> Contents { get { return contents; } }

	    public long? UpdateAt { get { return updateAt; } }

        public void SetContents(TextContent[] contents, long updateAt)
        {
            this.contents = contents;
            this.updateAt = updateAt;
        }
    }
}
