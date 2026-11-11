
using System;

namespace Modules.LocalData
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class FileNameAttribute : Attribute
    {
        //----- params -----

        //----- field -----

        //----- property -----

        public string FileName { get; private set; }

        public bool Encrypt { get; private set; }

        //----- method -----

        public FileNameAttribute(string fileName, bool encrypt = true)
        {
            this.FileName = fileName;
            this.Encrypt = encrypt;
        }
    }
}
