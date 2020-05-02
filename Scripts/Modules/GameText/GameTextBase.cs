
using System;
using System.Collections.Generic;
using Extensions;

namespace Modules.GameText.Components
{
    public abstract class GameTextBase<T> : Singleton<T> where T : GameTextBase<T>
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

        protected virtual void BuildGenerateContents() { }

        public virtual Type GetCategoriesType() { return null; }

        public virtual string FindCategoryGuid(Enum categoryType) { return null; }

        public virtual Type FindCategoryEnumType(string categoryGuid) { return null; }

        public virtual Enum FindCategoryDefinitionEnum(string categoryGuid) { return null; }

        public virtual IReadOnlyDictionary<Enum, string> FindCategoryTexts(string categoryGuid) { return null; }

        public virtual string FindTextGuid(Enum textType) { return null; }
    }
}
