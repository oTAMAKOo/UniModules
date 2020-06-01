
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

namespace Modules.Devkit.SceneLaunch
{
    public class SceneLaunchWindow : SingletonEditorWindow<SceneLaunchWindow>
    {
        //----- params -----

        private static class Prefs
        {
            public static string targetScenePath
            {
                get { return ProjectPrefs.GetString("SceneExecuterPrefs-targetScenePath"); }
                set { ProjectPrefs.SetString("SceneExecuterPrefs-targetScenePath", value); }
            }

            public static bool resume
            {
                get { return ProjectPrefs.GetBool("SceneExecuterPrefs-resume", false); }
                set { ProjectPrefs.SetBool("SceneExecuterPrefs-resume", value); }
            }

            public static bool standbyInitializer
            {
                get { return ProjectPrefs.GetBool("SceneExecuterPrefs-standbyInitializer", false); }
                set { ProjectPrefs.SetBool("SceneExecuterPrefs-standbyInitializer", value); }
            }

            public static string[] suspendObjectNames
            {
                get { return ProjectPrefs.Get<string[]>("SceneExecuterPrefs-suspendObjectNames", new string[0]); }
                set { ProjectPrefs.Set<string[]>("SceneExecuterPrefs-suspendObjectNames", value); }
            }
        }

        /// <summary>
        /// 全ヒエラルキーを非アクティブ化.
        /// </summary>
        public static void SuspendSceneInstance()
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
        public static void ResumeSceneInstance()
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
        private class SceneResume
        {
            private const int CheckInterval = 30;

            private static int frameCount = 0;

            private static IDisposable disposable = null;

            static SceneResume()
            {
                EditorApplication.update += UpdateCallback;
                EditorApplication.playModeStateChanged += PlaymodeStateChangedCallback;
            }

            private static void UpdateCallback()
            {
                if (Application.isPlaying) { return; }

                if (EditorApplication.isCompiling) { return; }

                if (!Prefs.resume) { return; }

                if (CheckInterval < frameCount++)
                {
                    ResumeScene();

                    frameCount = 0;
                }
            }

            private static void PlaymodeStateChangedCallback(PlayModeStateChange state)
            {
                if (!Prefs.resume) { return; }

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

                        disposable = EditorSceneChanger.SceneResume().Subscribe(_ => Prefs.resume = false);
                    }
                    else
                    {
                        ResumeSceneInstance();

                        Prefs.resume = false;
                    }

                    Prefs.suspendObjectNames = new string[0];
                }                
            }
        }

        //----- field -----

        private string targetScenePath = null;

        private GUIStyle sceneNameStyle = null;

        private bool initialized = false;

        //----- property -----

        //----- method -----

        public static void Open()
        {
            Instance.Initialize();

            Instance.Show();
        }

        public void Initialize()
        {
            if (initialized) { return; }

            titleContent = new GUIContent("Launch Scene");
            minSize = new Vector2(0f, 55f);

            targetScenePath = Prefs.targetScenePath;

            initialized = true;
        }

        void OnEnable()
        {
            Initialize();
        }

        void Update()
        {
            if (!initialized)
            {
                Initialize();
            }
        }

        void OnGUI()
        {
            EditorLayoutTools.SetLabelWidth(120f);

            EditorGUILayout.Separator();

            using (new EditorGUILayout.HorizontalScope())
            {
                targetScenePath = string.IsNullOrEmpty(targetScenePath) ? Prefs.targetScenePath : targetScenePath;

                var sceneName = Path.GetFileName(targetScenePath);

                if (EditorLayoutTools.DrawPrefixButton("Scene", GUILayout.Width(65f), GUILayout.Height(18f)))
                {
                    SceneSelectorPrefs.selectedScenePath = targetScenePath;
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

                    EditorGUILayout.SelectableLabel(sceneName, sceneNameStyle, GUILayout.Height(18f));
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
                var disable = EditorApplication.isPlaying || EditorApplication.isCompiling || string.IsNullOrEmpty(targetScenePath);

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
            return EditorSceneChanger.SceneChange(targetScenePath)
                .Do(x =>
                    {
                        if (x)
                        {
                            Prefs.resume = true;
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
            if (scene.path != Prefs.targetScenePath) { return; }
            
            ResumeSuspendObject(scene.GetRootGameObjects());

            Debug.Log("OnSceneLoaded");
        }

        private void OnSelectScene(string targetScenePath)
        {
            this.targetScenePath = targetScenePath;

            Prefs.targetScenePath = targetScenePath;

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
