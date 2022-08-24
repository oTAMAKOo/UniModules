
namespace Modules.Lua.Text
{
	public sealed class BookData
	{
		public string bookName = null;

		public string sourceDirectory = null;

		public string destDirectory = null;

		public string[] sheets = null;
		
		public string hash = null;

		public string SourcePath { get { return $"{sourceDirectory}/{bookName}"; } }

		public string DestPath { get { return $"{destDirectory}/{bookName}"; } }
	}

	public sealed class SheetData
    {
        public int index = 0;

        public string sheetName = null;

        public string summary = null;

        public RecordData[] records = null;
    }

    public sealed class RecordData
    {
        public string id = null;

		public string[] texts = null;
    }
}