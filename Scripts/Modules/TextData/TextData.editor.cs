
#if UNITY_EDITOR

using System.Collections.Generic;
using System.Linq;
using Extensions;
using Modules.TextData.Components;

namespace Modules.TextData
{
    public sealed partial class TextData
    {
        //----- params -----

        public sealed class Category
        {
            public ContentType ContentType { get; private set; }

            public string Guid { get; private set; }

            public string DisplayName { get; private set; }

            public string Name { get; private set; }

            public Category(ContentType contentType, string guid, string displayName, string name)
            {
                ContentType = contentType;
                Guid = guid;
                DisplayName = displayName;
                Name = name;
            }
        }

        //----- field -----
        
        private Dictionary<string, Category> categories = null;

        private Dictionary<string, string> enumNames = null;

        //----- property -----

        public IReadOnlyList<Category> Categories
        {
            get
            {
                if (categories == null)
                {
                    categories = new Dictionary<string, Category>();
                }

                return categories.Values.ToArray();
            }
        }

        //----- method -----

        private void AddEditorContents(TextDataAsset asset)
        {
            if (asset == null) { return; }

            if (categories == null)
            {
                categories = new Dictionary<string, Category>();
            }

            if (enumNames == null)
            {
                enumNames = new Dictionary<string, string>();
            }

            foreach (var categoriesContent in asset.Contents)
            {
                var contentType = asset.ContentType;

                var categoryGuid = categoriesContent.Guid;
                
                var categoryName = categoriesContent.Name;
                var categoryDisplayName = string.Empty;

                if (!string.IsNullOrEmpty(categoriesContent.DisplayName) && cryptoKey != null)
                {
                    categoryDisplayName = categoriesContent.DisplayName.Decrypt(cryptoKey);
                }

                categories[categoryGuid] = new Category(contentType, categoryGuid, categoryDisplayName, categoryName);

                foreach (var textContent in categoriesContent.Texts)
                {
                    enumNames[textContent.Guid] = textContent.EnumName;
                }
            }
        }

        public string GetEnumName(string textGuid)
        {
            if (enumNames == null) { return null; }

            if (textGuid == null){ return null; }

            var enumName = enumNames.GetValueOrDefault(textGuid);

            return enumName;
        }
    }
}

#endif
