
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Extensions;

namespace Modules.Master
{
    public sealed partial class MasterManager : Singleton<MasterManager>
    {
        //----- params -----

        private const string MasterFileExtension = ".master";

        //----- field -----

        //----- property -----

        public List<IMaster> All { get; private set; }

        public string InstallDirectory { get; private set; }

        //----- method -----

        private MasterManager()
        {
            All = new List<IMaster>();
        }

        public void SetInstallDirectory(string installDirectory)
        {
            InstallDirectory = PathUtility.Combine(installDirectory, "Master");
        }

        public string GetInstallPath<T>() where T : IMaster
        {
            return PathUtility.Combine(InstallDirectory, GetMasterFileName<T>());
        }

        public static string GetMasterFileName<T>() where T : IMaster
        {
            return GetMasterFileName(typeof(T));
        }

        public static string GetMasterFileName(Type type)
        {
            if (!typeof(IMaster).IsAssignableFrom(type))
            {
                throw new InvalidDataException(string.Format("Type error : {0}", type.FullName));
            }

            // 通常はクラス名をそのままマスター名として扱う.
            var fileName = type.Name;

            // FileNameAttributeを持っている場合はそちらの名前を採用する.

            var fileNameAttribute = type.GetCustomAttributes(typeof(FileNameAttribute), false)
                .Cast<FileNameAttribute>()
                .FirstOrDefault();

            if (fileNameAttribute != null)
            {
                fileName = fileNameAttribute.FileName;
            }

            return fileName + MasterFileExtension;
        }
    }
}
