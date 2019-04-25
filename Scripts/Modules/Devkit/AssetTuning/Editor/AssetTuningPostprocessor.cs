
using UnityEngine;
using UnityEditor;
using System;
using Extensions.Devkit;

namespace Modules.Devkit.AssetTuning
{
    public class AssetTuningPostprocessor : AssetPostprocessor
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

        public override int GetPostprocessOrder()
        {
            return 50;
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
            var assetTuneManager = AssetTuneManager.Instance;

            var assetTuners = assetTuneManager.AssetTuners;

            try
            {
                AssetDatabase.StartAssetEditing();
                
                foreach (var tuner in assetTuners)
                {
                    tuner.OnBegin();
                }

                foreach (var tuner in assetTuners)
                {
                    foreach (var path in importedAssets)
                    {
                        if (!tuner.Validate(path)) { continue; }

                        if (assetTuneManager.IsFirstImport(path))
                        {
                            tuner.OnAssetCreate(path);
                        }

                        tuner.OnAssetImport(path);
                    }

                    foreach (var path in deletedAssets)
                    {
                        if (!tuner.Validate(path)) { continue; }

                        tuner.OnAssetDelete(path);
                    }

                    for (var i = 0; i < movedAssets.Length; i++)
                    {
                        if (!tuner.Validate(movedAssets[i])) { continue; }

                        tuner.OnAssetMove(movedAssets[i], movedFromPath[i]);
                    }
                }

                foreach (var path in importedAssets)
                {
                    assetTuneManager.FinishFirstImport(path);
                }

                foreach (var tuner in assetTuners)
                {
                    tuner.OnFinish();
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
        }
    }
}
