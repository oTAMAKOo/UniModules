
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using Extensions;
using Extensions.Serialize;
using Modules.GameText.Components;

namespace Modules.GameText
{
    public sealed partial class GameText : GameTextBase<GameText>
    {
        //----- params -----

        public const string AESKey = "8FweHH7BpESL2eJUtntTFCM3ZVx3B3JT";
        public const string AESIv = "AizA3xRfstJfPtpI";

        [Serializable]
        public sealed class GameTextDictionary : SerializableDictionary<string, string> { }

        //----- field -----

        //----- property -----

        public Dictionary<string, string> Cache { get; private set; }

        //----- method -----

        private GameText()
        {
            BuildGenerateContents();

            Cache = new Dictionary<string, string>();
        }

        public void Load(GameTextAsset asset)
        {
            Cache.Clear();

            if (asset == null) { return; }

            var aesManaged = AESExtension.CreateAesManaged(AESKey, AESIv);

            Cache = asset.contents
                .SelectMany(x =>  x)
                .ToDictionary(x => x.Key, x => x.Value.Decrypt(aesManaged));
        }

        public void LoadFromResources(string assetPath)
        {
            var resourcesPath = UnityPathUtility.ConvertResourcesLoadPath(assetPath);

            var asset = Resources.Load<GameTextAsset>(resourcesPath);

            Load(asset);
        }
    }
}
