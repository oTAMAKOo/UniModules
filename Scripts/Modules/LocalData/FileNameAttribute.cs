
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

        //----- method -----

        public FileNameAttribute(string fileName)
        {
            this.FileName = fileName;
        }
    }
}
