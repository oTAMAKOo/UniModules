
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
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

        public AesManaged FileNameEncryptor { get; private set; }

        //----- method -----

        private MasterManager()
        {
            All = new List<IMaster>();
        }

        public void SetInstallDirectory(string installDirectory)
        {
            InstallDirectory = PathUtility.Combine(installDirectory, "Master");
        }

        /// <summary> ファイル名暗号化オブジェクトを取得 </summary>
        public void SetFileNameEncryptor(AesManaged aesManaged)
        {
            FileNameEncryptor = aesManaged;
        }

        public string GetInstallPath<T>() where T : IMaster
        {
            return PathUtility.Combine(InstallDirectory, GetMasterFileName<T>());
        }

        public string GetMasterFileName<T>() where T : IMaster
        {
            return GetMasterFileName(typeof(T));
        }

        public string GetMasterFileName(Type type)
        {
            const string MasterSuffix = "Master";

            var fileName = string.Empty;

            if (!typeof(IMaster).IsAssignableFrom(type))
            {
                throw new InvalidDataException(string.Format("Type error require IMaster interface. : {0}", type.FullName));
            }

            // 通常はクラス名をそのままマスター名として扱う.
            fileName = type.Name;

            // 末尾が「Master」だったら末尾を削る.
            if (fileName.EndsWith(MasterSuffix))
            {
                fileName = fileName.SafeSubstring(0, fileName.Length - MasterSuffix.Length) + MasterFileExtension;
            }

            // FileNameAttributeを持っている場合はそちらの名前を採用する.

            var fileNameAttribute = type.GetCustomAttributes(typeof(FileNameAttribute), false)
                .Cast<FileNameAttribute>()
                .FirstOrDefault();

            if (fileNameAttribute != null)
            {
                fileName = fileNameAttribute.FileName + MasterFileExtension;
            }

            // 暗号化オブジェクトが設定されていたら暗号化.
            
            if (FileNameEncryptor != null)
            {
                fileName = fileName.Encrypt(FileNameEncryptor);
            }
            
            return fileName;
        }
    }
}
