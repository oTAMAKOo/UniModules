
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

        private long position = 0;

        //----- property -----

        public bool EndOfStream { get; private set; }

        //----- method -----

        public StreamReverseReader(FileStream fileStream, string lineEnd = "\r\n")
        {
            this.fileStream = fileStream;
            this.lineEnd = lineEnd;

            if (fileStream != null)
            {
                position = fileStream.Seek(0, SeekOrigin.End);
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

            if (position != 0)
            {
                bytes = new byte[BufferSize];

                var oldPosition = position;

                if (position >= BufferSize)
                {
                    position = fileStream.Seek(-1 * BufferSize, SeekOrigin.Current);
                }
                else
                {
                    position = fileStream.Seek(-1 * position, SeekOrigin.Current);

                    size = (int)(oldPosition - position);
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

                EndOfStream = position == 0;
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

            position = -1;
        }
    }
}
