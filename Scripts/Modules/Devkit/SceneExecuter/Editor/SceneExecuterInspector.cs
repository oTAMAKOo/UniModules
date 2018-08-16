﻿
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System;
using System.IO;
using System.Linq;
using UniRx;
using Extensions.Devkit;
using Modules.Devkit.EditorSceneChange;
using Modules.Devkit.Prefs;

namespace Modules.Devkit.SceneExecuter.Editor
{
    [CustomEditor(typeof(SceneExecuter))]
    public class SceneExecuterInspector : UnityEditor.Editor
    {
        //----- params -----

        private enum LaunchState
        {
            None = 0,
            WaitOpenScene,
            WaitSceneChange,
            SceneStart,
        }

        private static class Prefs
        {
            public static bool launch
            {
                get { return ProjectPrefs.GetBool("SceneExecuterPrefs-launch", false); }
                set { ProjectPrefs.SetBool("SceneExecuterPrefs-launch", value); }
            }

            public static bool standbyInitializer
            {
                get { return ProjectPrefs.GetBool("SceneExecuterPrefs-standbyInitializer", false); }
                set { ProjectPrefs.SetBool("SceneExecuterPrefs-standbyInitializer", value); }
            }

            public static int[] enableInstanceIds
            {
                get { return ProjectPrefs.Get<int[]>("SceneExecuterPrefs-enableInstanceIds", new int[0]); }
                set { ProjectPrefs.Set<int[]>("SceneExecuterPrefs-enableInstanceIds", value); }
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

            Prefs.enableInstanceIds = rootObjects.Select(y => y.gameObject.GetInstanceID()).ToArray();
        }

        /// <summary>
        /// 非アクティブ化したObjectを復帰.
        /// </summary>
        public static void ResumeSceneInstance()
        {
            var enableInstanceIds = Prefs.enableInstanceIds;

            var rootObjects = UnityEditorUtility.FindRootObjectsInHierarchy();

            rootObjects = rootObjects.Where(x => enableInstanceIds.Contains(x.gameObject.GetInstanceID())).ToArray();

            // 非アクティブ化したオブジェクトを復元.
            foreach (var rootObject in rootObjects)
            {
                rootObject.SetActive(true);
            }
        }

        [InitializeOnLoad]
        private class SceneResume
        {
            private const int CheckInterval = 180;

            private static int frameCount = 0;
            private static bool forceUpdate = false;
            private static IDisposable disposable = null;

            static SceneResume()
            {
                EditorApplication.update += ResumeScene;
                EditorApplication.playModeStateChanged += PlaymodeStateChanged;
            }

            private static void PlaymodeStateChanged(PlayModeStateChange state)
            {
                if (!Application.isPlaying && Prefs.launch)
                {
                    forceUpdate = true;
                }
            }

            private static void ResumeScene()
            {
                if (Application.isPlaying) { return; }

                if (!Prefs.launch) { return; }

                if (forceUpdate || CheckInterval < frameCount++)
                {
                    var waitScene = EditorSceneChangerPrefs.waitScene;
                    var lastScene = EditorSceneChangerPrefs.lastScene;

                    if (string.IsNullOrEmpty(waitScene) && !string.IsNullOrEmpty(lastScene))
                    {
                        if (disposable != null)
                        {
                            disposable.Dispose();
                            disposable = null;
                        }

                        disposable = EditorSceneChanger.SceneResume(() => Prefs.launch = false).Subscribe();
                    }
                    // 現在のシーンから起動されたのでResumeされない.
                    else
                    {
                        Prefs.launch = false;
                        ResumeSceneInstance();
                    }

                    frameCount = 0;
                    forceUpdate = false;
                }
            }
        }

        //----- field -----

        private SceneExecuter instance = null;

        //----- property -----

        //----- method -----

        public override void OnInspectorGUI()
        {
            instance = target as SceneExecuter;

            serializedObject.Update();

            var targetScenePathProperty = serializedObject.FindProperty("targetScenePath");

            EditorLayoutTools.SetLabelWidth(120f);

            EditorGUILayout.Separator();

            using(new EditorGUILayout.HorizontalScope())
            {
                var sceneName = Path.GetFileName(targetScenePathProperty.stringValue);

                if (EditorLayoutTools.DrawPrefixButton("Scene", GUILayout.Width(80f)))
                {
                    SceneSelectorPrefs.selectedScenePath = targetScenePathProperty.stringValue;
                    SceneSelector.Open().Subscribe(OnSelectScene);
                }

                var sceneNameStyle = GUI.skin.GetStyle("TextArea");
                sceneNameStyle.alignment = TextAnchor.MiddleLeft;

                EditorGUILayout.SelectableLabel(sceneName, sceneNameStyle, GUILayout.Height(18f));

                // 下記条件時は再生ボタンを非アクティブ:.
                // ・実行中.
                // ・ビルド中.
                // ・遷移先シーンが未選択.
                GUI.enabled = !(EditorApplication.isPlaying ||
                                EditorApplication.isCompiling ||
                                string.IsNullOrEmpty(targetScenePathProperty.stringValue));

                if (GUILayout.Button("Launch", GUILayout.ExpandWidth(true), GUILayout.Width(100f)))
                {
                    SceneSelectorPrefs.selectedScenePath = targetScenePathProperty.stringValue;
                    Launch().Subscribe();
                }

                GUI.enabled = true;

                GUILayout.Space(15f);
            }

            EditorGUILayout.Separator();
        }

        private IObservable<Unit> Launch()
        {
            return EditorSceneChanger.SceneChange(instance.TargetScenePath)
                .Do(x =>
                    {
                        if (x)
                        {
                            Prefs.launch = true;
                            Prefs.standbyInitializer = true;

                            SuspendSceneInstance();

                            // 実行状態にする.
                            // ※ 次のフレームでメモリ内容が消滅する.
                            EditorApplication.isPlaying = true;
                        }
                    })
                .AsUnitObservable();
        }


        [InitializeOnLoadMethod()]
        public static void InitializeOnLoadMethod()
        {
            if (Prefs.standbyInitializer)
            {
                EditorApplication.CallbackFunction execCallbackFunction = null;

                execCallbackFunction = () =>
                {
                    ExecSceneInitializer().Subscribe();

                    EditorApplication.delayCall -= execCallbackFunction;
                };

                EditorApplication.delayCall += execCallbackFunction;
                
                Prefs.standbyInitializer = false;
            }
        }

        private static IObservable<Unit> ExecSceneInitializer()
        {
            // ScriptableObjectの初期化を待つ為1フレーム待機.
            return Observable.NextFrame()
                .Do(_ => ResumeSceneInstance())
                .AsUnitObservable();
        }

        private void OnSelectScene(string targetScenePath)
        {
            serializedObject.Update();

            var targetScenePathProperty = serializedObject.FindProperty("targetScenePath");

            UnityEditorUtility.RegisterUndo("SceneExecuterInspector Undo", instance);

            targetScenePathProperty.stringValue = targetScenePath;

            serializedObject.ApplyModifiedProperties();

            SceneSelectorPrefs.selectedScenePath = targetScenePathProperty.stringValue;

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