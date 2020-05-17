
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using Extensions;

namespace Modules.GameText.Components
{
    public abstract class GameTextBase<T> : Singleton<T> where T : GameTextBase<T>
    {
        //----- params -----

        //----- field -----

        protected Dictionary<string, string> cache = null;

        //----- property -----

        public IReadOnlyDictionary<string, string> Cache { get { return cache; } }

        //----- method -----

        protected override void OnCreate()
        {
            cache = new Dictionary<string, string>();
            
            BuildGenerateContents();
        }

        public string FindText(string textGuid)
        {
            if (string.IsNullOrEmpty(textGuid)) { return string.Empty; }

            return cache.GetValueOrDefault(textGuid);
        }

        protected virtual void BuildGenerateContents() { }

        public virtual Type GetCategoriesType() { return null; }

        public virtual string FindCategoryGuid(Enum categoryType) { return null; }

        public virtual Type FindCategoryEnumType(string categoryGuid) { return null; }

        public virtual Enum FindCategoryDefinitionEnum(string categoryGuid) { return null; }

        public virtual IReadOnlyDictionary<Enum, string> FindCategoryTexts(string categoryGuid) { return null; }

        public virtual string FindTextGuid(Enum textType) { return null; }

        protected abstract string GetAesKey();

        protected abstract string GetAesIv();
    }
}
