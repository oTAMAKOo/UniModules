
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
        //----- params -----

        //----- field -----

        private AssetRegisterScrollView assetListView = null;

        private bool changed = false;
        
        private DeleteAssetSetting instance = null;

        //----- property -----

        //----- method -----

        void OnEnable()
        {
            instance = target as DeleteAssetSetting;

            changed = false;
            
            var guids = instance.Guids.ToArray();

            assetListView = new AssetRegisterScrollView("Delete Targets", "DeleteAssetSettingInspector-Delete Targets");
            assetListView.RemoveChildrenAssets = true;
            assetListView.SetContents(guids);

            assetListView.OnUpdateContentsAsObservable().Subscribe(x => OnUpdateContents(x));
        }

        void OnDisable()
        {
            if (changed)
            {
                UnityEditorUtility.SaveAsset(instance);
            }
        }

        public override void OnInspectorGUI()
        {
            instance = target as DeleteAssetSetting;

            EditorLayoutTools.ContentTitle("Label");

            using (new ContentsScope())
            {
                EditorGUI.BeginChangeCheck();

                var tag = EditorGUILayout.DelayedTextField(instance.Tag);

                if (EditorGUI.EndChangeCheck())
                {
                    UnityEditorUtility.RegisterUndo(instance);

                    Reflection.SetPrivateField(instance, "tag", tag);

                    UnityEditorUtility.SaveAsset(instance);
                }
            }
            
            assetListView.DrawGUI();
        }

        private void OnUpdateContents(AssetRegisterScrollView.AssetInfo[] infos)
        {
            var guids = infos.Select(x => x.guid)
                .Where(x => !string.IsNullOrEmpty(x))
                .ToArray();

            UnityEditorUtility.RegisterUndo(instance);

            Reflection.SetPrivateField(instance, "guids", guids);

            changed = true;
        }
    }
}
