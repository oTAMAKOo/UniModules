using UnityEngine;
using UnityEditor;
using System.Linq;
using System.IO;
using Extensions;
using Modules.Devkit.Generators;
using Modules.Devkit.Project;

namespace Modules.Devkit.SceneImporter
{
    public sealed class SceneAssetPostprocessor : AssetPostprocessor
    {
        //----- params -----

        //----- field -----
        
        private const string BuildSettingsFileName = "EditorBuildSettings.asset";

        //----- property -----

        //----- method -----

        public override int GetPostprocessOrder()
        {
            return 500;
        }

        /// <summary>
        /// あらゆる種類の任意の数のアセットがインポートが完了したときに呼ばれる処理です。
        /// </summary>
        /// <param name="importedAssets"> インポートされたアセットのファイルパス。 </param>
        /// <param name="deletedAssets"> 削除されたアセットのファイルパス。 </param>
        /// <param name="movedAssets"> 移動されたアセットのファイルパス。 </param>
        /// <param name="movedFromPath"> 移動されたアセットの移動前のファイルパス。 </param>
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromPath)
        {
            var editorConfig = ProjectFolders.Instance;
            var sceneImporterConfig = SceneImporterConfig.Instance;

            if (sceneImporterConfig == null) { return; }

            var sceneFileExtension = SceneImporterConfig.SceneFileExtension;

            // ビルドセッティングファイルの更新時.
            if (importedAssets.Any(x => Path.GetFileName(x) == BuildSettingsFileName))
            {
                var buildTargetScenes = EditorBuildSettings.scenes;
                var scenes = buildTargetScenes.DistinctBy(x => x.path).ToArray();

                if (scenes.Length != buildTargetScenes.Length)
                {
                    EditorBuildSettings.scenes = scenes;
                    ScenesScriptGenerator.Generate(sceneImporterConfig.ManagedFolders, editorConfig.EditorScriptPath);
                    AssetDatabase.SaveAssets();

                    EditorUtility.DisplayDialog("SceneAssetPostprocessor", "ビルドターゲットに入っているシーンの\n重複項目を削除しました", "確認");
                }
            }

            var assetPaths = new[] { importedAssets, deletedAssets, movedFromPath, movedAssets }.SelectMany(x => x);

            if (assetPaths.Any(x => Path.GetExtension(x) == sceneFileExtension))
            {   
                var isChanged = false;

                var autoAdditionFolders = sceneImporterConfig.ManagedFolders;
                var buildTargetScenes = EditorBuildSettings.scenes.ToDictionary(x => x.path);

                //--------------------------------------------------------------------
                // 移動 (ビルド対象から外れる場合).
                //--------------------------------------------------------------------

                for (var i = 0; i < movedAssets.Length; ++i)
                {
                    isChanged = true;

                    if (!buildTargetScenes.ContainsKey(movedAssets[i])) { continue; }

                    // ビルド対象フォルダ内の移動は除外処理しない.
                    if (autoAdditionFolders.Any(y => movedAssets[i].StartsWith(y))){ continue; }

                    // ビルドターゲットに入っているシーンが移動した場合ビルド対象から外す.
                    buildTargetScenes.Remove(movedAssets[i]);
                }

                //--------------------------------------------------------------------
                // 移動 (ビルド対象に追加される場合.).
                //--------------------------------------------------------------------

                // .unity かつ、自動追加対象フォルダ以下のファイルパスを取得.
                var movedInScenes = movedAssets
                    .Where(x => Path.GetExtension(x) == sceneFileExtension)
                    .Where(x => autoAdditionFolders.Any(y => x.StartsWith(y)));

                foreach (var movedInScene in movedInScenes)
                {
                    if (buildTargetScenes.ContainsKey(movedInScene)){ continue; }

                    // buildTargetScenesに入っていない物を追加.
                    buildTargetScenes[movedInScene] = new EditorBuildSettingsScene(movedInScene, true);

                    isChanged = true;
                }

                //--------------------------------------------------------------------
                // リネーム.
                //--------------------------------------------------------------------

                for (var i = 0; i < movedAssets.Length; ++i)
                {
                    if (!buildTargetScenes.ContainsKey(movedAssets[i])) { continue; }

                    // 別ディレクトリなら「ファイル移動」なので処理しない.
                    if (Path.GetDirectoryName(movedAssets[i]) != Path.GetDirectoryName(movedFromPath[i])){ continue; }

                    if (Path.GetFileName(movedAssets[i]) != Path.GetFileName(movedFromPath[i]))
                    {
                        isChanged = true;
                    }
                }

                //--------------------------------------------------------------------
                // 削除.
                //--------------------------------------------------------------------

                foreach (var deletedAsset in deletedAssets)
                {
                    if (!buildTargetScenes.ContainsKey(deletedAsset)){ continue; }

                    // ビルドターゲットに入っているシーンが消されたらビルド対象から外す.
                    buildTargetScenes.Remove(deletedAsset);
                    isChanged = true;
                }

                //--------------------------------------------------------------------
                // 追加.
                //--------------------------------------------------------------------

                // .unity かつ、自動追加対象フォルダ以下のファイルパスを取得.
                var addScenes = importedAssets
                    .Where(x => Path.GetExtension(x) == sceneFileExtension)
                    .Where(x => autoAdditionFolders.Any(y => x.StartsWith(y)));

                foreach (var addScene in addScenes)
                {
                    if (buildTargetScenes.ContainsKey(addScene)){ continue; }

                    // buildTargetScenesに入っていない物を追加.
                    buildTargetScenes[addScene] = new EditorBuildSettingsScene(addScene, true);
                    isChanged = true;
                }

                if (isChanged)
                {
                    ScenesInBuildGenerator.UpdateBuildTargetScenes(sceneImporterConfig, editorConfig.ScriptPath, buildTargetScenes);
                }
            }
        }
    }
}
