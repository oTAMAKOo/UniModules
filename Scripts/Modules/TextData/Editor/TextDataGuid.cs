
using System.Collections.Generic;
using Extensions;

namespace Modules.TextData.Editor
{
    public static class TextDataGuid
    {
        //----- params -----

        //----- field -----

        private static readonly Dictionary<string, string> guidCache = null;

        //----- property -----

        //----- method -----

        static TextDataGuid()
        {
            guidCache = new Dictionary<string, string>();
        }

        public static string Get(string identifier)
        {
            if (string.IsNullOrEmpty(identifier)) { return null; }

            if (!guidCache.ContainsKey(identifier))
            {
                var guid = identifier.GetHash();

                guidCache.Add(identifier, guid);
            }

            return guidCache[identifier];
        }
    }
}