
#if ENABLE_XLUA

using UnityEngine;
using System;
using System.Collections.Generic;

namespace Modules.Lua.Text
{
    public sealed class LuaTextAsset : ScriptableObject
    {
        //----- params ----

        [Serializable]
		public sealed class Content
		{
			[SerializeField]
			public string sheetName = null;
			[SerializeField]
			public string summary = null;
			[SerializeField]
			public TextData[] texts = new TextData[0];
		}

		[Serializable]
		public sealed class TextData
		{
			[SerializeField]
			private string id = null;
			[SerializeField]
			private string text = null;

			public string Id { get { return id; } }

			public string Text { get { return text; } }

			public TextData(string id, string text)
			{
				this.id = id;
				this.text = text;
			}
		}

        //----- field -----
		
		[SerializeField]
		private string fileName = null;
		[SerializeField]
		private string rootFolderGuid = null;
		[SerializeField]
		private string hash = null;
		[SerializeField]
		private Content[] contents = new Content[0];

        //----- property -----

		public string FileName { get { return fileName; } }

		public string RootFolderGuid { get { return rootFolderGuid; } }

		public IReadOnlyList<Content> Contents { get { return contents; } }

		public string Hash { get { return hash; } }

        //----- method -----
		
		public void SetContents(string fileName, string rootFolderGuid, Content[] contents, string hash)
		{
			this.fileName = fileName;
			this.rootFolderGuid = rootFolderGuid;
			this.contents = contents;
			this.hash = hash;
		}
    }
}

#endif