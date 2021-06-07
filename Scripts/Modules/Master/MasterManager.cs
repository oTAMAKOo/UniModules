
using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MessagePack;
using MessagePack.Resolvers;
using Extensions;
using Modules.MessagePack;

namespace Modules.Master
{
    public sealed partial class MasterManager : Singleton<MasterManager>
    {
        //----- params -----

        private const string MasterFileExtension = ".master";

        private const string MasterSuffix = "Master";

        //----- field -----

        private Dictionary<string, IMaster> masters = null;

        private bool lz4Compression = true;

        private MessagePackSerializerOptions serializerOptions = null;

        //----- property -----

        public IReadOnlyCollection<IMaster> Masters
        {
            get { return masters.Values; }
        }

        /// <summary> 保存先. </summary>
        public string InstallDirectory { get; private set; }

        /// <summary> ファイル名暗号化キー. </summary>
        public AesCryptKey FileNameCryptKey { get; private set; }

        /// <summary> LZ4圧縮を使用するか. </summary>
        public bool Lz4Compression
        {
            get { return lz4Compression; }
            set
            {
                lz4Compression = value;
                serializerOptions = null;
            }
        }

        //----- method -----

        private MasterManager()
        {
            masters = new Dictionary<string, IMaster>();

            // 保存先設定.
            SetInstallDirectory(Application.persistentDataPath);
        }

        public void Register(IMaster master)
        {
            var type = master.GetType();

            var fileName = GetMasterFileName(type);

            if (masters.ContainsKey(fileName))
            {
                var message = string.Format("File name has already been registered.\n\nClass : {0}\nFile : {1}", type.FullName, fileName);

                throw new Exception(message);
            }

            masters.Add(fileName, master);
        }

        public void Clear()
        {
            masters.Clear();
        }

        public void SetInstallDirectory(string installDirectory)
        {
            InstallDirectory = PathUtility.Combine(installDirectory, "Master");

            #if UNITY_IOS

            if (InstallDirectory.StartsWith(Application.persistentDataPath))
            {
                UnityEngine.iOS.Device.SetNoBackupFlag(InstallDirectory);
            }

            #endif
        }

        /// <summary> ファイル名暗号化オブジェクトを設定 </summary>
        public void SetFileNameCryptKey(AesCryptKey aesCryptKey)
        {
            FileNameCryptKey = aesCryptKey;
        }
        
        public string GetMasterFileName<T>() where T : IMaster
        {
            return GetMasterFileName(typeof(T));
        }

        public string GetMasterFileName(Type type, bool encrypt = true)
        {
            if (!typeof(IMaster).IsAssignableFrom(type))
            {
                throw new Exception(string.Format("Type error require IMaster interface. : {0}", type.FullName));
            }

            var fileName = string.Empty;

            // FileNameAttributeを持っている場合はそちらの名前を採用する.

            var fileNameAttribute = type.GetCustomAttributes(typeof(FileNameAttribute), false)
                .Cast<FileNameAttribute>()
                .FirstOrDefault();

            if (fileNameAttribute != null)
            {
                fileName = fileNameAttribute.FileName;
            }
            else
            {
                // クラス名をファイル名として採用.
                fileName = DeleteMasterSuffix(type.Name);
            }

            fileName = Path.ChangeExtension(fileName, MasterFileExtension);

            // 暗号化オブジェクトが設定されていたら暗号化.

            if (encrypt && FileNameCryptKey != null)
            {
                fileName = fileName.Encrypt(FileNameCryptKey, true);
            }

            return fileName;
        }

        public static string DeleteMasterSuffix(string fileName)
        {
            // 末尾が「Master」だったら末尾を削る.
            if (fileName.EndsWith(MasterSuffix))
            {
                fileName = fileName.SafeSubstring(0, fileName.Length - MasterSuffix.Length);
            }

            return fileName;
        }

        public MessagePackSerializerOptions GetSerializerOptions()
        {
            if (serializerOptions != null) { return serializerOptions; }

            var options = StandardResolverAllowPrivate.Options.WithResolver(UnityContractResolver.Instance);

            if (Lz4Compression)
            {
                options = options.WithCompression(MessagePackCompression.Lz4BlockArray);
            }

            serializerOptions = options;

            return serializerOptions;
        }
    }
}
