
using UnityEngine;
using Extensions;
using Modules.Devkit.ScriptableObjects;

namespace Modules.Devkit.Project
{
    public sealed class ProjectCryptoKey : SingletonScriptableObject<ProjectCryptoKey>
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