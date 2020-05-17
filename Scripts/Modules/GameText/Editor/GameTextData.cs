
namespace Modules.GameText.Editor
{
    public sealed class SheetData
    {
        public string sheetName { get; set; } = null;
    
        public string displayName { get; set; } = null;

        public int index { get; set; } = 0;

        public string guid { get; set; } = null;

        public RecordData[] records { get; set; } = null;
    }

    public sealed class RecordData
    {
        public string enumName { get; set; } = null;

        public string description { get; set; } = null;

        public string guid { get; set; } = null;

        public ContentData[] contents { get; set; } = null;
    }

    public sealed class ContentData
    {
        public string text { get; set; } = null;
    }
}
