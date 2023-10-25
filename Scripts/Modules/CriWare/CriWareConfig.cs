
#if ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_ADX_LE || ENABLE_CRIWARE_SOFDEC
using Cysharp.Threading.Tasks;
using UnityEngine;
using Extensions;

namespace Modules.CriWare
{
    public abstract class CriWareConfig : ScriptableObject
    {
        //----- params -----

        //----- field -----

        [SerializeField]
        protected string key = string.Empty;

        //----- property -----

        //----- method -----

        public static CriWareConfig LoadInstance(string resourcesPath)
        {
            return Resources.Load<CriWareConfig>(resourcesPath);
        }

        public async UniTask<string> GetCriWareKey()
        {
            var cryptoKey = await GetCryptoKey();

            return key.Decrypt(cryptoKey);
        }

        public abstract UniTask<AesCryptoKey> GetCryptoKey();
    }
}

#endif