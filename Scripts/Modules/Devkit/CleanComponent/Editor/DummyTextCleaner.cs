
#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System.Reflection;
using Cysharp.Threading.Tasks;
using Extensions;
using Unity.Linq;
using Modules.TextData.Components;
using Modules.UI.DummyContent;

namespace Modules.Devkit.CleanComponent
{
    public sealed class DummyTextCleaner : AssetModificationProcessor
    {
        //----- params -----

        //----- field -----

		private static MethodInfo dummyTextCleanMethodInfo = null;
		private static MethodInfo textSetterCleanMethodInfo = null;

		private static bool initialized = false;

        //----- property -----

        //----- method -----

		public static void Initialize()
		{
			if (initialized) { return; }

			dummyTextCleanMethodInfo = Reflection.GetMethodInfo(typeof(DummyText), "CleanDummyText", BindingFlags.NonPublic | BindingFlags.Instance);
			textSetterCleanMethodInfo = Reflection.GetMethodInfo(typeof(TextSetter), "CleanDummyText", BindingFlags.NonPublic | BindingFlags.Instance);

			initialized = true;
		}

		// 初回以降はOnDestroyで破棄処理を行うので実行不要.

		public static void OnWillCreateAsset(string assetPath)
		{
			Initialize();
			
			if (!assetPath.EndsWith(".prefab")){ return; }

			ModifyPrefabContents(assetPath).Forget();
		}

		private static async UniTask ModifyPrefabContents(string assetPath)
		{
			await UniTask.NextFrame();

			var changed = false;

			var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

			if (prefab == null) { return; }

			// DummyText.
			{
				var items = prefab.DescendantsAndSelf().OfComponent<DummyText>();

				foreach (var item in items)
				{
					changed |= (bool)dummyTextCleanMethodInfo.Invoke(item, null);
				}
			}

			// TextSetter.
			{
				var items = prefab.DescendantsAndSelf().OfComponent<TextSetter>();

				foreach (var item in items)
				{
					changed |= (bool)textSetterCleanMethodInfo.Invoke(item, null);
				}
			}

			if (changed)
			{
				PrefabUtility.SavePrefabAsset(prefab);
			}
		}
    }
}

#endif
