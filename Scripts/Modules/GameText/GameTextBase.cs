
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

        public virtual string FindTextGuid(Enum textType) { return null; }

        protected virtual void BuildGenerateContents() { }

        protected virtual Type GetCategoriesType() { return null; }

        protected virtual string FindCategoryGuid(Enum categoryType) { return null; }

        protected virtual Enum FindCategoryEnumFromCategoryGuid(string categoryGuid) { return null; }

        protected virtual Enum FindCategoryEnumFromTextGuid(string textGuid) { return null; }

        protected virtual IReadOnlyDictionary<Enum, string> FindCategoryTexts(string categoryGuid) { return null; }

        protected abstract string GetAesKey();

        protected abstract string GetAesIv();
    }
}
