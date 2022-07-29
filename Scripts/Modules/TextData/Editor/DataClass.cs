
namespace Modules.TextData.Editor
{
    public sealed class SheetData
    {
        public string sheetName { get; set; } = null;
    
        public string displayName { get; set; } = null;

        public int index { get; set; } = 0;

        public string guid { get; set; } = null;

        public RecordData[] records { get; set; } = new RecordData[0];
    }

    public sealed class RecordData
    {
        public string enumName { get; set; } = null;

        public string description { get; set; } = null;

        public string guid { get; set; } = null;

        public string[] texts { get; set; } = null;
    }
}
