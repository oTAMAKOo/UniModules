
#if ENABLE_VSTU

using System.IO;
using System.Text;

namespace VisualStudioToolsUnity
{
    public class Utf8StringWriter : StringWriter
    {
        public override Encoding Encoding
        {
            get { return Encoding.UTF8; }
        }
    }
}

#endif
