
using System.Collections.Generic;

namespace Modules.TextData.Components
{
    public class TextDataValidator
    {
        //----- params -----

        public sealed class InValidData
        {
            public string categoryGuid = null;
            public string textGuid = null;
        }

        //----- field -----

        //----- property -----

        //----- method -----

        public InValidData[] Validation(TextDataAsset asset)
        {
            var inValidTargets = new List<InValidData>();

            foreach(var content in asset.Contents)
            {
                var categoryGuid = content.Guid;

                foreach(var item in content.Texts)
                {
                    var inValid = IsInValid(item.Text);

                    if (inValid)
                    {
                        var inValidData = new InValidData()
                        {
                            categoryGuid = categoryGuid,
                            textGuid = item.Guid,
                        };

                        inValidTargets.Add(inValidData);
                    }
                }
            }

            return inValidTargets.ToArray();
        }

        public virtual bool IsInValid(string text)
        {
            return string.IsNullOrEmpty(text);
        }
    }
}