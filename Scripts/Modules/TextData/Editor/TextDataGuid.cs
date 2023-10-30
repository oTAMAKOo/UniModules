
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
            var guid = string.Empty;

            if (!guidCache.ContainsKey(identifier))
            {
                guid = identifier.GetHash();

                guidCache.Add(identifier, guid);
            }

            return guid;
        }
    }
}