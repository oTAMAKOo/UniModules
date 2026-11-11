
using System;

namespace Modules.Master
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
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
