
namespace Modules.GameText.Editor
{
    public sealed class SheetData
    {
        public string guid { get; set; }

        public int index { get; set; }

        public string sheetName { get; set; }

        public string displayName { get; set; }
    }

    public sealed class RecordData
    {
        public string guid { get; set; }

        public string sheet { get; set; }

        public int line { get; set; }

        public string enumName { get; set; }

        public string description { get; set; }

        public string[] texts { get; set; }
    }
}
