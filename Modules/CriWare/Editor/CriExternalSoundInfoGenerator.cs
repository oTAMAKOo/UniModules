
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Cysharp.Threading.Tasks;
using Extensions;
using Modules.Devkit.Project;

#if ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_ADX_LE || ENABLE_CRIWARE_SOFDEC

using CriWare;

#endif

namespace Modules.CriWare.Editor
{
    public static class CriExternalSoundInfoGenerator
    {
        //----- params -----

        private const string AcbExtension = ".acb";

        public sealed class SoundInfo
        {
            public string Identifier { get; private set; }
            
            public string LoadPath { get; private set; }

            public string Cue { get; private set; }

            public SoundInfo(string loadPath, string cueName)
            {
                Identifier = CreateIdentifier(loadPath, cueName);

                LoadPath = loadPath;
                Cue = cueName;
            }
        }

        //----- field -----

        private static HashAlgorithm hashAlgorithm = null;

        //----- property -----

        //----- method -----

        public static async UniTask<SoundInfo[]> Generate()
        {
            var acbFiles = FindExternalAssetAcbFiles();

            await Initialize();

            var sounds = Build(acbFiles);

            return sounds;
        }

        private static string[] FindExternalAssetAcbFiles()
        {
            var projectResourceFolders = ProjectResourceFolders.Instance;

            var externalAssetPath = UnityPathUtility.ConvertAssetPathToFullPath(projectResourceFolders.ExternalAssetPath);

            return Directory.GetFiles(externalAssetPath, "*" + AcbExtension, SearchOption.AllDirectories)
                .Where(x => Path.GetExtension(x) == AcbExtension)
                .Select(x => PathUtility.ConvertPathSeparator(x))
                .ToArray();
        }

        private static async UniTask Initialize()
        {
            #if ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_ADX_LE || ENABLE_CRIWARE_SOFDEC

            if(CriWareInitializer.IsInitialized()){ return; }

            CriForceInitializer.Initialize();

            await UniTask.WaitUntil(() => CriWareInitializer.IsInitialized());

            #else

            await UniTask.NextFrame();

            #endif
        }

        private static SoundInfo[] Build(string[] acbFiles)
        {
            #if ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_ADX_LE || ENABLE_CRIWARE_SOFDEC

            var projectResourceFolders = ProjectResourceFolders.Instance;

            var list = new List<SoundInfo>();

            foreach (var acbFile in acbFiles)
            {
                var acb = CriAtomExAcb.LoadAcbFile(null, acbFile, "");

                if (acb == null) { continue; }

                var acbPath = UnityPathUtility.ConvertFullPathToAssetPath(acbFile);

                var loadPath = acbPath.Replace(projectResourceFolders.ExternalAssetPath, string.Empty).TrimStart('/');

                var infos = acb.GetCueInfoList();

                foreach (var info in infos)
                {
                    var data = new SoundInfo(loadPath, info.name);

                    list.Add(data);
                }

                acb.Dispose();
            }

            return list.OrderBy(x => x.LoadPath).ToArray();

            #else

            return new SoundInfo[0];

            #endif
        }

        private static string CreateIdentifier(string loadPath, string cueName)
        {
            var text = $"{loadPath}.{cueName}";

            if (hashAlgorithm == null)
            {
                hashAlgorithm = MD5.Create();
            }

            var builder = new StringBuilder();

            var bytes = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(text));
            
            foreach (var b in bytes)
            {
                builder.Append(b.ToString("x2"));
            }

            return builder.ToString();
        }
    }
}