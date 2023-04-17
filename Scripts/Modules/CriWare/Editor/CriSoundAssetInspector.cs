
#if ENABLE_CRIWARE_ADX

using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using CriWare;
using UniRx;
using Extensions;
using Extensions.Devkit;
using Modules.Devkit.Inspector;
using Modules.CriWare.Editor;
using Modules.Sound;

namespace Modules.CriWare
{
    public sealed class CriSoundAssetInspector : ExtendInspector
    {
        //----- params -----

        //----- field -----

        private List<CueInfo> cueInfos = null;

        private GUIContent clipboardIcon = null;

        private Vector2 scrollPosition = Vector2.zero;

        private LifetimeDisposable lifetimeDisposable = new LifetimeDisposable();

        //----- property -----

        public override int Priority { get { return 0; } }

        //----- method -----

        [InitializeOnLoadMethod]
        private static void InitializeOnLoadMethod()
        {
            DefaultAssetInspector.AddExtendInspector<CriSoundAssetInspector>();
        }

        public override bool Validation(UnityEngine.Object target)
        {
            var assetPath = AssetDatabase.GetAssetPath(target);

            var extension = Path.GetExtension(assetPath);

            return extension == ".acb";
        }

        public override void OnEnable(UnityEngine.Object target)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                // CriWareInitializerの初期化を待つ.
                Observable.EveryUpdate()
                    .SkipWhile(_ => !CriWareInitializer.IsInitialized())
                    .First()
                    .Subscribe(_ => LoadCueInfo(target))
                    .AddTo(lifetimeDisposable.Disposable);
            }
            else
            {
                CriForceInitializer.Initialize();

                LoadCueInfo(target);
            }

            clipboardIcon = EditorGUIUtility.IconContent("Clipboard");
        }

        public override void DrawInspectorGUI(UnityEngine.Object target)
        {
            if (cueInfos == null) { return; }
            
            GUILayout.Space(4f);
            
            var acbPath = AssetDatabase.GetAssetPath(target);

            EditorLayoutTools.Title("Asset Path");

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.SelectableLabel(acbPath, EditorStyles.textArea, GUILayout.Height(18f));

                if (GUILayout.Button(clipboardIcon, GUILayout.Width(20f)))
                {
                    GUIUtility.systemCopyBuffer = acbPath;
                }
            }

            GUILayout.Space(4f);

            if (cueInfos.IsEmpty())
            {
                EditorGUILayout.HelpBox("CueSheet dont have Cue.", MessageType.Info);
            }
            else
            {
                EditorLayoutTools.Title("Contents");

                using (new ContentsScope())
                {
                    using (var scrollViewScope = new EditorGUILayout.ScrollViewScope(scrollPosition, GUILayout.MaxHeight(500f)))
                    {
                        foreach (var cueInfo in cueInfos)
                        {
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                EditorGUILayout.LabelField(cueInfo.Cue, EditorStyles.textArea, GUILayout.Height(18f));

                                if (GUILayout.Button(clipboardIcon, GUILayout.Width(20f)))
                                {
                                    GUIUtility.systemCopyBuffer = cueInfo.Cue;
                                }
                            }

                            GUILayout.Space(3f);
                        }

                        scrollPosition = scrollViewScope.scrollPosition;
                    }
                }
            }

            EditorGUILayout.Separator();
        }

        private void LoadCueInfo(UnityEngine.Object acbAsset)
        {
            cueInfos = new List<CueInfo>();

            // 指定したACBファイル名(キューシート名)を指定してキュー情報を取得.
            var assetPath = AssetDatabase.GetAssetPath(acbAsset);
            var fullPath = UnityPathUtility.GetProjectFolderPath() + assetPath;
            var acb = CriAtomExAcb.LoadAcbFile(null, fullPath, "");

            if (acb != null)
            {
				var list = acb.GetCueInfoList();

                foreach (var item in list)
                {
                    var path = PathUtility.GetPathWithoutExtension(assetPath);

                    var cueInfo = new CueInfo(string.Empty, path, item.name);

                    cueInfos.Add(cueInfo);
                }

                acb.Dispose();
            }
        }
    }
}

#endif
