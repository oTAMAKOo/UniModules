
using UnityEditor;

namespace Modules.Devkit.AssetDependencies
{
    public static class FindDependenciesInProject
    {
		private const string MenuItemLabel = "Assets/Find Dependencies In Project";
		
		[MenuItem(MenuItemLabel, validate = true)]
		public static bool CanExecute()
		{
			var target = Selection.activeObject;

			return target != null && AssetDatabase.IsMainAsset(target);
		}

		[MenuItem(MenuItemLabel, priority = 28)]
		public static void Execute()
		{
			var target = Selection.activeObject;

			if(!AssetDatabase.IsMainAsset(target)) { return; }

			AssetDependenciesWindow.Open(target);
		}
    }
}