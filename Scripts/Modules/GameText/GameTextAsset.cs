
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
        [SerializeField, ReadOnly]
        private int index = 0;

        public string Guid { get { return guid; } }

        public string Text { get { return text; } }

        public int Index { get { return index; } }

        public TextContent(string guid, string text, int index)
        {
            this.guid = guid;
            this.text = text;
            this.index = index;
        }
    }

    public sealed class GameTextAsset : ScriptableObject
	{
        [SerializeField, ReadOnly]
        private LongNullable updateTime = null;
        [SerializeField, ReadOnly]
        private TextContent[] contents = new TextContent[0];

        public long? UpdateTime { get { return updateTime; } }

	    public IReadOnlyList<TextContent> Contents { get { return contents; } }

        public void SetContents(long? updateTime, TextContent[] contents)
        {
            this.updateTime = updateTime;
            this.contents = contents;
        }
    }
}
