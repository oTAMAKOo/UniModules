﻿
using System;
using System.Linq;
using Extensions;
using Modules.Localize;

namespace Modules.TextData.Editor
{
	public sealed class LanguageInfo
	{
		public Enum Language { get; private set; }

		public string Identifier { get; private set; }
            
		public int TextIndex { get; private set; }

        public bool ScriptGenerate { get; private set; }

		public LanguageInfo(Enum language, string identifier, int textIndex, bool scriptGenerate)
		{
			Language = language;
			Identifier = identifier;
			TextIndex = textIndex;
            ScriptGenerate = scriptGenerate;
		}
	}

    public sealed class LanguageManager : Singleton<LanguageManager>
    {
		//----- params -----

		//----- field -----
		
		private LanguageInfo[] languageInfos = null;

		//----- property -----

		public LanguageInfo Current
		{
			get
			{
				if (languageInfos == null){ return null; }

				var selection = EditorLanguage.selection;
			
				return languageInfos.FirstOrDefault(x => Convert.ToInt32(x.Language) == selection);
			}
		}

		//----- method -----

		public void Initialize(LanguageInfo[] languageInfos)
		{
			this.languageInfos = languageInfos;
		}

        public LanguageInfo GetLanguageInfo(Enum language)
        {
            return languageInfos.FirstOrDefault(x => x.Language == language);
        }
	}
}
