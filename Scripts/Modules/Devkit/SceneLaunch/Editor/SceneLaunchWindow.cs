
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System;
using UniRx;
using Extensions.Devkit;
using Modules.Devkit.Prefs;

namespace Modules.Devkit.SceneLaunch
{
    public sealed class SceneLaunchWindow : SingletonEditorWindow<SceneLaunchWindow>
    {
        //----- params -----

		private static class Prefs
        {
			public static string targetSceneGuid
            {
                get { return ProjectPrefs.GetString(typeof(Prefs).FullName + "-targetSceneGuid"); }
                set { ProjectPrefs.SetString(typeof(Prefs).FullName + "-targetSceneGuid", value); }
            }

			public static bool requestLaunch
			{
				get { return ProjectPrefs.GetBool(typeof(Prefs).FullName + "-requestLaunch"); }
				set { ProjectPrefs.SetBool(typeof(Prefs).FullName + "-requestLaunch", value); }
			}
		}

		//----- field -----

        private SceneAsset sceneAsset = null;

        private GUIStyle sceneNameStyle = null;

        private bool initialized = false;

        [NonSerialized]
        private bool isSceneLoaded = false;

        //----- property -----

        //----- method -----

        public static void Open()
        {
            Instance.Initialize();

            Instance.Show();
        }

        private void Initialize()
        {
            if (initialized) { return; }

            titleContent = new GUIContent("Launch Scene");

            minSize = new Vector2(250f, 60f);

            LoadSceneAsset();
            
            initialized = true;
        }

        private void LoadSceneAsset()
        {
            var sceneGuid = Prefs.targetSceneGuid;
            
            sceneAsset = UnityEditorUtility.FindMainAsset(sceneGuid) as SceneAsset;

            isSceneLoaded = true;
        }

        void OnEnable()
        {
            Initialize();
        }

        void Update()
        {
            if (!isSceneLoaded)
            {
                LoadSceneAsset();
            }
        }

        void OnGUI()
        {
            EditorLayoutTools.SetLabelWidth(120f);

            EditorGUILayout.Separator();

            using (new EditorGUILayout.HorizontalScope())
            {
                if (EditorLayoutTools.PrefixButton("Scene", GUILayout.Width(65f), GUILayout.Height(18f)))
                {
                    SceneSelector.Prefs.selectedScenePath = sceneAsset != null ? AssetDatabase.GetAssetPath(sceneAsset) : null;

                    SceneSelector.Open().Subscribe(OnSelectScene).AddTo(Disposable);
                }

                if (sceneNameStyle == null)
                {
                    sceneNameStyle = EditorStyles.textArea;
                    sceneNameStyle.alignment = TextAnchor.MiddleLeft;
                }

                using (new EditorGUILayout.VerticalScope())
                {
                    GUILayout.Space(3f);

                    EditorGUI.BeginChangeCheck();

                    sceneAsset = EditorLayoutTools.ObjectField(sceneAsset, false);

                    if (EditorGUI.EndChangeCheck())
                    {
                        Prefs.targetSceneGuid = UnityEditorUtility.GetAssetGUID(sceneAsset);
                    }
                }

                GUILayout.Space(5f);
            }

            GUILayout.Space(5f);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(5f);

                // 下記条件時は再生ボタンを非アクティブ:.
                // ・実行中.
                // ・ビルド中.
                // ・遷移先シーンが未選択.
                var disable = EditorApplication.isPlaying || EditorApplication.isCompiling || sceneAsset == null;

                using (new DisableScope(disable))
                {
                    if (GUILayout.Button("Launch"))
                    {
                        Launch();
                    }
                }

                GUILayout.Space(5f);
            }
        }

        private void Launch()
        {
			Prefs.requestLaunch = true;

			EditorSceneManager.playModeStartScene = sceneAsset;

			EditorApplication.isPlaying = true;
		}

		private void OnSelectScene(string targetScenePath)
        {
            Prefs.targetSceneGuid = AssetDatabase.AssetPathToGUID(targetScenePath);

            LoadSceneAsset();

            SceneSelector.Prefs.selectedScenePath = targetScenePath;

            Repaint();
        }

		[InitializeOnLoadMethod]
		private static void InitializeOnLoadMethod()
		{
			if (!Prefs.requestLaunch) { return; }

			EditorSceneManager.playModeStartScene = null;    
			
			Prefs.requestLaunch = false;
		}
	}
}
