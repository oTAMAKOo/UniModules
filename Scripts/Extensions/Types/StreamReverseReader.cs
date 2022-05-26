
using System;
using System.IO;
using System.Text;

namespace Extensions
{
    public sealed class StreamReverseReader : IDisposable
    {
        //----- params -----

        private const int BufferSize = 1024;

        //----- field -----

        private FileStream fileStream = null;

        private string lineEnd = null;

        private string data = null;

        //----- property -----

        public bool EndOfStream { get; private set; }

		public long Position { get; private set; }

		public long Length { get { return fileStream != null ? fileStream.Length : 0; } }

        //----- method -----

        public StreamReverseReader(FileStream fileStream, string lineEnd = "\r\n")
        {
            this.fileStream = fileStream;
            this.lineEnd = lineEnd;

            if (fileStream != null)
            {
				Position = fileStream.Seek(0, SeekOrigin.End);
                EndOfStream = false;
                data = string.Empty;
            }
            else
            {
                EndOfStream = true;
            }
        }

        private byte[] ReadStream()
        {
            byte[] bytes = null;

            var size = BufferSize;

            if (Position != 0)
            {
                bytes = new byte[BufferSize];

                var oldPosition = Position;

                if (Position >= BufferSize)
                {
					Position = fileStream.Seek(-1 * BufferSize, SeekOrigin.Current);
                }
                else
                {
					Position = fileStream.Seek(-1 * Position, SeekOrigin.Current);

                    size = (int)(oldPosition - Position);
                    bytes = new byte[size];
                }

                fileStream.Read(bytes, 0, size);
                fileStream.Seek(-1 * size, SeekOrigin.Current);
            }

            return bytes;
        }

        public string ReadLine()
        {
            string line = "";

            while (!EndOfStream && !data.Contains(lineEnd))
            {
                byte[] bytes = ReadStream();

                if (bytes != null)
                {
                    string temp = Encoding.UTF8.GetString(bytes);
                    data = data.Insert(0, temp);
                }

                EndOfStream = Position == 0;
            }


            var lastReturn = data.LastIndexOf(lineEnd, StringComparison.CurrentCulture);

            if (lastReturn == -1)
            {
                if (data.Length > 0)
                {
                    line = data;
                    data = string.Empty;
                }
                else
                {
                    line = null;
                }
            }
            else
            {
                line = data.Substring(lastReturn + 2);
                data = data.Remove(lastReturn);
            }

            return line;
        }

        public void Close()
        {
            fileStream.Close();
        }

        public void Dispose()
        {
            fileStream.Dispose();

            data = string.Empty;

			Position = -1;
        }
    }
}
