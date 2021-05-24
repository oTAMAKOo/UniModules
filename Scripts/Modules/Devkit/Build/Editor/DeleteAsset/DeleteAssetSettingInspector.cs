
using UnityEditor;
using System.Linq;
using UniRx;
using Extensions;
using Extensions.Devkit;
using Modules.Devkit.Inspector;

namespace Modules.Devkit.Build
{
    [CustomEditor(typeof(DeleteAssetSetting))]
    public sealed class DeleteAssetSettingInspector : UnityEditor.Editor
    {
        private AssetRegisterScrollView assetListView = null;

        private DeleteAssetSetting instance = null;

        void OnEnable()
        {
            instance = target as DeleteAssetSetting;

            var guids = Reflection.GetPrivateField<DeleteAssetSetting, string[]>(instance, "targetGuids");

            assetListView = new AssetRegisterScrollView("Delete Assets", "DeleteAssetSettingInspector-Delete Assets");
            assetListView.RemoveChildrenAssets = true;
            assetListView.SetContents(guids);

            assetListView.OnUpdateContentsAsObservable().Subscribe(x => OnUpdateContents(x));
        }

        public override void OnInspectorGUI()
        {
            instance = target as DeleteAssetSetting;

            assetListView.DrawGUI();
        }

        private void OnUpdateContents(AssetRegisterScrollView.AssetInfo[] infos)
        {
            var guids = infos.Select(x => x.guid)
                .Where(x => !string.IsNullOrEmpty(x))
                .ToArray();

            UnityEditorUtility.RegisterUndo("DeleteAssetSettingInspector Undo", instance);

            Reflection.SetPrivateField<DeleteAssetSetting, string[]>(instance, "targetGuids", guids);

            UnityEditorUtility.SaveAsset(instance);
        }
    }
}
