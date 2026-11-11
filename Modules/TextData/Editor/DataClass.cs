
using System.Collections.Generic;

namespace Modules.TextData.Editor
{
    public sealed class IndexData
    {
        public string[] sheetNames { get; set; } = null;
    }

    public sealed class SheetData
    {
        public int index { get; set; } = 0;

        public string sheetName { get; set; } = null;
    
        public string displayName { get; set; } = null;

        public string guid { get; set; } = null;

        public string hash { get; set; } = null;

        public List<RecordData> records { get; set; } = new List<RecordData>();
    }

    public sealed class RecordData
    {
        public string enumName { get; set; } = null;

        public string description { get; set; } = null;

        public string guid { get; set; } = null;

        public string[] texts { get; set; } = null;
    }
}
