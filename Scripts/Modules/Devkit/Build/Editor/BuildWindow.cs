﻿﻿﻿﻿﻿﻿﻿
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Extensions.Devkit;
using Modules.Devkit.Prefs;

using Object = UnityEngine.Object;

namespace Modules.Devkit.Build
{
    public abstract class BuildWindow<T> : SingletonEditorWindow<T> where T : BuildWindow<T>
    {
        //----- params -----

        private static class Prefs
        {
            public static string selectionName
            {
                get { return ProjectPrefs.GetString("BuildWindowPrefs-selectionName", null); }
                set { ProjectPrefs.SetString("BuildWindowPrefs-selectionName", value); }
            }
        }

        protected readonly Color BackgroundColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);
        protected readonly Color LabelColor = new Color(0.8f, 0.8f, 0.8f, 0.8f);

        //----- field -----

        protected List<BuildParamInfo> buildParamInfos = null;
        protected BuildParamInfo selectionBuildParam = null;

        protected bool isEditMode = false;
        protected bool isloaded = false;
        protected bool initialized = false;

        private BuildConfig buildConfig = null;

        //----- property -----

        protected virtual Vector2 WindowSize { get { return new Vector2(400f, 250f); } }

        //----- method -----

        public void Initialize()
        {
            if (initialized) { return; }

            titleContent = new GUIContent("Build Application");
            maxSize = WindowSize;
            minSize = maxSize;
            ShowUtility();

            LoadContents();

            initialized = true;
        }

        protected virtual void LoadContents()
        {
            buildConfig = BuildConfig.Instance;

            if (isloaded || buildConfig == null) { return; }

            isEditMode = false;
            buildParamInfos = buildConfig.CustomBuildParams.ToList();

            var selectionName = Prefs.selectionName;

            if (!string.IsNullOrEmpty(selectionName))
            {
                selectionBuildParam = buildParamInfos.FirstOrDefault(x => x.ToString() == selectionName);
            }

            isloaded = true;
        }

        void OnGUI()
        {
            var requireBuild = false;

            if (!initialized) { return; }

            if (!isloaded)
            {
                LoadContents();
                return;
            }

            GUILayout.Space(5f);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(5f);

                using (new EditorGUILayout.VerticalScope())
                {
                    // 環境毎のビルド設定.
                    EditorLayoutTools.Title("Parameter", BackgroundColor, LabelColor);
                    {
                        EditorGUILayout.Separator();

                        DrawCustomParameterSelect();

                        EditorGUILayout.Separator();
                    }

                    EditorGUILayout.Separator();

                    // ビルドオプション.
                    EditorLayoutTools.Title("Build Options", BackgroundColor, LabelColor);
                    {
                        EditorGUILayout.Separator();

                        DrawBuildOptions();

                        EditorGUILayout.Separator();
                    }

                    GUILayout.FlexibleSpace();

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Space(20f);

                        // 適用.
                        if (GUILayout.Button("Apply"))
                        {
                            GUI.FocusControl(string.Empty);

                            if (EditorUtility.DisplayDialog("Confirmation", "選択中の設定でビルド設定を更新します", "実行", "中止"))
                            {
                                var buildTarget = selectionBuildParam.buildTarget;
                                var buildParam = selectionBuildParam.buildParam;

                                BuildManager.Apply(buildTarget, buildParam);

                                Repaint();
                                return;
                            }
                        }

                        GUILayout.Space(20f);

                        // ビルド.
                        if (GUILayout.Button("Build"))
                        {
                            GUI.FocusControl(string.Empty);

                            if (EditorUtility.DisplayDialog("Confirmation", "選択中の設定でビルドを実行します", "実行", "中止"))
                            {
                                requireBuild = true;
                            }
                        }

                        GUILayout.Space(20f);
                    }

                    EditorGUILayout.Separator();
                }

                GUILayout.Space(5f);
            }

            GUILayout.Space(5f);

            // スコープの途中でアセンブリのリロードが実行されるとエラーが出るためここでビルドを開始.
            if(requireBuild)
            {
                Close();
                EditorApplication.delayCall += Build;
            }
        }

        private void DrawCustomParameterSelect()
        {
            if (isEditMode)
            {
                // ビルドターゲット.
                selectionBuildParam.buildGroup = (BuildTargetGroup)EditorGUILayout.EnumPopup("BuildTargetGroup", selectionBuildParam.buildGroup);
                selectionBuildParam.buildTarget = (BuildTarget)EditorGUILayout.EnumPopup("BuildTarget", selectionBuildParam.buildTarget);

                GUILayout.Space(2f);

                // 名前.
                using (new GUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("BuildName", GUILayout.Width(148f));
                    selectionBuildParam.name = EditorGUILayout.TextField(selectionBuildParam.name);
                }

                GUILayout.Space(2f);

                // パラメータ定義ファイル.
                selectionBuildParam.buildParam = EditorGUILayout.ObjectField(selectionBuildParam.buildParam, typeof(BuildParam), false) as BuildParam;

                GUILayout.Space(2f);

                using (new EditorGUILayout.HorizontalScope())
                {
                    var disable = string.IsNullOrEmpty(selectionBuildParam.name) || selectionBuildParam.buildParam == null;

                    EditorGUI.BeginDisabledGroup(disable);
                    {
                        if (GUILayout.Button("Add"))
                        {
                            if (buildParamInfos.All(x => x.ToString() != selectionBuildParam.ToString()))
                            {
                                buildParamInfos.Add(selectionBuildParam);
                                buildConfig.CustomBuildParams = buildParamInfos.ToArray();

                                UnityEditorUtility.SaveAsset(buildConfig);

                                isEditMode = false;
                            }
                            else
                            {
                                EditorUtility.DisplayDialog("Error", "It is already the name registered.", "Confirm");
                            }
                        }
                    }
                    EditorGUI.EndDisabledGroup();

                    GUILayout.Space(2f);

                    if (GUILayout.Button("Cancel"))
                    {
                        isEditMode = false;
                        selectionBuildParam = null;
                    }
                }
            }
            else
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    var index = 0;

                    if (selectionBuildParam != null)
                    {
                        index = buildParamInfos.IndexOf(x => x.ToString() == selectionBuildParam.ToString());
                    }

                    var labels = buildParamInfos
                        .Select(x => x.ToString())
                        .ToArray();

                    EditorGUI.BeginChangeCheck();

                    index = EditorGUILayout.Popup(index, labels, GUILayout.Width(300f));

                    if (EditorGUI.EndChangeCheck())
                    {
                        if (index != -1)
                        {
                            selectionBuildParam = buildParamInfos[index];
                            Prefs.selectionName = selectionBuildParam.ToString();
                        }
                        else
                        {
                            Prefs.selectionName = null;
                        }
                    }

                    if (GUILayout.Button("", new GUIStyle("OL Plus"), GUILayout.Width(18f)))
                    {
                        isEditMode = true;
                        selectionBuildParam = new BuildParamInfo();
                    }

                    if (GUILayout.Button("", new GUIStyle("OL Minus"), GUILayout.Width(18f)))
                    {
                        buildParamInfos.Remove(selectionBuildParam);
                        buildConfig.CustomBuildParams = buildParamInfos.ToArray();

                        UnityEditorUtility.SaveAsset(buildConfig);

                        selectionBuildParam = null;
                    }
                }
            }
        }

        // オプションの描画.
        protected virtual void DrawBuildOptions()
        {
            var originLabelWidth = EditorLayoutTools.SetLabelWidth(130f);

            if (selectionBuildParam != null && selectionBuildParam.buildParam != null)
            {
                var buildParam = selectionBuildParam.buildParam;

                EditorGUI.BeginChangeCheck();

                // Version.
                buildParam.Version = EditorGUILayout.DelayedTextField("Version", buildParam.Version);

                GUILayout.Space(2f);

                // BuildVersion.
                buildParam.BuildVersion = EditorGUILayout.DelayedIntField("BuildVersion", buildParam.BuildVersion);

                if (EditorGUI.EndChangeCheck())
                {
                    UnityEditorUtility.SaveAsset(buildParam);
                }
            }

            GUILayout.Space(2f);

            EditorLayoutTools.SetLabelWidth(originLabelWidth);
        }

        private void Build()
        {
            EditorApplication.LockReloadAssemblies();

            OnBuildStart();

            var buildGroup = selectionBuildParam.buildGroup;
            var buildTarget = selectionBuildParam.buildTarget;
            var buildParam = selectionBuildParam.buildParam;

            BuildManager.Build(buildGroup, buildTarget, buildParam, false);

            OnBuildComplete();

            EditorApplication.UnlockReloadAssemblies();
        }

        protected virtual void OnBuildStart() { }

        protected virtual void OnBuildComplete() { }
    }
}
