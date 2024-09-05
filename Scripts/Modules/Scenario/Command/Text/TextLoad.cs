
#if ENABLE_XLUA

using UnityEngine;
using System;
using XLua;
using Cysharp.Threading.Tasks;
using Extensions;
using Modules.Lua.Text;
using Modules.ExternalAssets;

namespace Modules.Scenario.Command
{
	[CSharpCallLua]
    public sealed class TextLoad : AssetLoad<LuaTextAsset> 
    {
        //----- params -----

        //----- field -----

        //----- property -----

		public override string LuaName { get { return "TextLoad"; } }

        public override string Callback 
        {
            get { return BuildCallName<TextLoad>(nameof(LuaCallback)); }
        }

		public Func<string, string> EditAssetPathCallback { get; set; }

		//----- method -----

		protected override async UniTask LoadAsset(string assetPath)
		{
			if (EditAssetPathCallback != null)
			{
				assetPath = EditAssetPathCallback.Invoke(assetPath);
			}

			var asset = await ExternalAsset.LoadAsset<LuaTextAsset>(assetPath);
			
			if (asset != null)
			{
				scenarioController.LuaText.Set(asset);
			}
			else
			{
				Debug.LogErrorFormat("Text load error : {0}", assetPath);
			}
		}
	}
}

#endif