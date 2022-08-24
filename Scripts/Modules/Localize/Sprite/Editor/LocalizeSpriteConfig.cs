
using UnityEngine;
using Extensions;
using Modules.Devkit.ScriptableObjects;

namespace Modules.Localize
{
    public sealed class LocalizeSpriteConfig : SingletonScriptableObject<LocalizeSpriteConfig>
    {
        //----- params -----

        //----- field -----

		[SerializeField]
		private string cyptoKey = null;
		[SerializeField]
		private string cyptoIv = null;

        //----- property -----

        //----- method -----

		public AesCryptoKey GetCryptoKey()
		{
			return new AesCryptoKey(cyptoKey, cyptoIv);
		}
	}
}