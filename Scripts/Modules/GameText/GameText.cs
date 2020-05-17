
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using Extensions;
using Modules.GameText.Components;

namespace Modules.GameText
{
    public sealed partial class GameText : GameTextBase<GameText>
    {
        //----- params -----

        //----- field -----

        private AesManaged aesManaged = null;

        private TextContent[] textContents = null;

        //----- property -----
        
        //----- method -----

        private GameText(){ }

        public AesManaged GetAesManaged()
        {
            return aesManaged ?? (aesManaged = AESExtension.CreateAesManaged(GetAesKey(), GetAesIv()));
        }

        public void Load(GameTextAsset asset)
        {
            cache.Clear();

            if (asset == null) { return; }
            
            textContents = asset.Contents.ToArray();

            var aesManaged = GetAesManaged();

            cache = textContents.ToDictionary(x => x.Guid, x => x.Text.Decrypt(aesManaged));
        }

        public void LoadFromResources(string assetPath)
        {
            var resourcesPath = UnityPathUtility.ConvertResourcesLoadPath(assetPath);

            var asset = Resources.Load<GameTextAsset>(resourcesPath);

            Load(asset);
        }

        public TextContent FindTextContentInfo(string textGuid)
        {
            return textContents.FirstOrDefault(x => x.Guid == textGuid);
        }
    }
}
