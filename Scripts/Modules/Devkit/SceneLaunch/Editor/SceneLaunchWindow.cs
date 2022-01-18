
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using System;
using System.IO;
using System.Linq;
using UniRx;
using Extensions.Devkit;
using Modules.Devkit.EditorSceneChange;
using Modules.Devkit.Prefs;

using Object = UnityEngine.Object;

namespace Modules.Devkit.SceneLaunch
{
    public sealed class SceneLaunchWindow : SingletonEditorWindow<SceneLaunchWindow>
    {
        //----- params -----

        private enum Status
        {
            None = 0,

            ResumeScene,
            ResumeObject,
        }
        
        private static class Prefs
        {
            public static Status status
            {
                get { return ProjectPrefs.GetEnum("SceneLaunchPrefs-resume", Status.None); }
                set { ProjectPrefs.SetEnum("SceneLaunchPrefs-resume", value); }
            }

            public static string targetSceneGuid
            {
                get { return ProjectPrefs.GetString("SceneLaunchPrefs-targetSceneGuid"); }
                set { ProjectPrefs.SetString("SceneLaunchPrefs-targetSceneGuid", value); }
            }

            public static bool standbyInitializer
            {
                get { return ProjectPrefs.GetBool("SceneLaunchPrefs-standbyInitializer", false); }
                set { ProjectPrefs.SetBool("SceneLaunchPrefs-standbyInitializer", value); }
            }

            public static string[] suspendObjectNames
            {
                get { return ProjectPrefs.Get<string[]>("SceneLaunchPrefs-suspendObjectNames", new string[0]); }
                set { ProjectPrefs.Set<string[]>("SceneLaunchPrefs-suspendObjectNames", value); }
            }
        }
        
        /// <summary>
        /// 全ヒエラルキーを非アクティブ化.
        /// </summary>
        private static void SuspendSceneInstance()
        {
            var rootObjects = UnityEditorUtility.FindRootObjectsInHierarchy(false);

            // SceneInitializerの初期化を待つ(Awake, Startを走らせない)為.
            // 一時的にHierarchy上のオブジェクトを非アクティブ化.
            foreach (var rootObject in rootObjects)
            {
                rootObject.SetActive(false);
            }

            Prefs.suspendObjectNames = rootObjects.Select(y => y.gameObject.name).ToArray();
        }

        /// <summary>
        /// 非アクティブ化したObjectを復帰.
        /// </summary>
        private static void ResumeSceneInstance()
        {
            var rootObjects = UnityEditorUtility.FindRootObjectsInHierarchy();
            
            ResumeSuspendObject(rootObjects);
        }

        private static void ResumeSuspendObject(GameObject[] rootObjects)
        {
            var suspendObjectNames = Prefs.suspendObjectNames;

            var targetObjects = rootObjects.Where(x => suspendObjectNames.Contains(x.gameObject.name)).ToArray();

            // 非アクティブ化したオブジェクトを復元.
            foreach (var targetObject in targetObjects)
            {
                targetObject.SetActive(true);
            }
        }

        [InitializeOnLoad]
        private sealed class SceneResume
        {
            private const int CheckInterval = 30;

            private static int frameCount = 0;

            private static IDisposable disposable = null;

            static SceneResume()
            {
                EditorApplication.update += UpdateCallback;
                EditorApplication.playModeStateChanged += PlayModeStateChangedCallback;
            }

            private static void UpdateCallback()
            {
                if (Application.isPlaying) { return; }

                if (EditorApplication.isCompiling) { return; }

                if (Prefs.status == Status.None) { return; }

                if (CheckInterval < frameCount++)
                {
                    ResumeScene();

                    frameCount = 0;
                }
            }

            private static void PlayModeStateChangedCallback(PlayModeStateChange state)
            {
                if (Prefs.status == Status.None) { return; }

                if (state != PlayModeStateChange.EnteredEditMode){ return; }

                ResumeScene();
            }

            private static void ResumeScene()
            {
                // 遷移中ではない.
                if (EditorSceneChanger.State == SceneChangeState.None)
                {
                    var currentScene = GetCurrentScenePath();

                    var resumeScene = EditorSceneChangerPrefs.resumeScene;

                    var resume = true;

                    // 戻り先シーンがある.
                    resume &= !string.IsNullOrEmpty(resumeScene);
                    // 現在のシーンが戻り先のシーンではない.
                    resume &= currentScene != resumeScene;

                    if (resume)
                    {
                        if (disposable != null)
                        {
                            disposable.Dispose();
                            disposable = null;
                        }

                        disposable = EditorSceneChanger.SceneResume()
                            .Subscribe(_ => Prefs.status = Status.ResumeObject);
                    }
                    else
                    {
                        ResumeSceneInstance();

                        Prefs.status = Status.None;
                    }

                    Prefs.suspendObjectNames = new string[0];
                }                
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
                    SceneSelectorPrefs.selectedScenePath = sceneAsset != null ? AssetDatabase.GetAssetPath(sceneAsset) : null;

                    SceneSelector.Open().Subscribe(OnSelectScene);
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
                        Launch().Subscribe();
                    }
                }

                GUILayout.Space(5f);
            }
        }

        private IObservable<Unit> Launch()
        {
            var sceneGuid = Prefs.targetSceneGuid;

            var scenePath = string.IsNullOrEmpty(sceneGuid) ? null : AssetDatabase.GUIDToAssetPath(sceneGuid);

            return EditorSceneChanger.SceneChange(scenePath)
                .Do(x =>
                    {
                        if (x)
                        {
                            Prefs.status = Status.ResumeScene;
                            Prefs.standbyInitializer = true;

                            SuspendSceneInstance();

                            // 実行状態にする.
                            // ※ 次のフレームでメモリ内容が消滅する.
                            EditorApplication.isPlaying = true;
                        }
                    })
                .AsUnitObservable();
        }


        [InitializeOnLoadMethod]
        private static void InitializeOnLoadMethod()
        {
            if (!Prefs.standbyInitializer) { return; }

            // ScriptableObjectの初期化を待つ為少し待機.
            Observable.TimerFrame(5).Subscribe(_ => ResumeSceneInstance());

            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;

            Prefs.standbyInitializer = false;            
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            var sceneGuid = Prefs.targetSceneGuid;

            var scenePath = string.IsNullOrEmpty(sceneGuid) ? null : AssetDatabase.GUIDToAssetPath(sceneGuid);

            if (scene.path != scenePath) { return; }
            
            ResumeSuspendObject(scene.GetRootGameObjects());
        }

        private void OnSelectScene(string targetScenePath)
        {
            Prefs.targetSceneGuid = AssetDatabase.AssetPathToGUID(targetScenePath);

            LoadSceneAsset();

            SceneSelectorPrefs.selectedScenePath = targetScenePath;

            Repaint();
        }

        private static string GetCurrentScenePath()
        {
            var scene = EditorSceneManager.GetSceneAt(0);

            return scene.path;
        }

        private string AsSpacedCamelCase(string text)
        {
            var sb = new System.Text.StringBuilder(text.Length * 2);
            sb.Append(char.ToUpper(text[0]));

            for (var i = 1; i < text.Length; i++)
            {
                if (char.IsUpper(text[i]) && text[i - 1] != ' ')
                    sb.Append(' ');
                sb.Append(text[i]);
            }
            return sb.ToString();
        }
    }
}
