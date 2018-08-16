﻿﻿
using UnityEngine;
using UnityEditor;
using Unity.Linq;
using System;
using System.Linq;
using UniRx;
using Modules.Devkit.Prefs;

namespace Modules.Devkit.CompileNotice
{
    public static class CompileNotificationViewPrefs
    {
        public static bool enable
        {
            get { return ProjectPrefs.GetBool("CompileNotificationViewPrefs-enable", false); }
            set { ProjectPrefs.SetBool("CompileNotificationViewPrefs-enable", value); }
        }
    }

    public static class CompileNotificationView
    {
        //----- params -----

        private const float UpdateInterval = 0.15f;

        //----- field -----

        private static Texture[] animationTextures = null;

        private static GUIStyle labelStyle = null;

        private static Texture backgroundTexture = null;
        private static Texture currentTexture = null;
        private static float updateTime = 0f;
        private static float prevTime = 0f;
        private static int animationIndex = 0;

        private static bool isAssetLoaded = false;

        private static IDisposable onCompileFinishDisposable = null;

        //----- property -----

        //----- method -----

        [InitializeOnLoadMethod]
        private static void InitializeOnLoadMethod()
        {
            SetEnable(CompileNotificationViewPrefs.enable);
        }

        public static void SetEnable(bool state)
        {
            CompileNotificationViewPrefs.enable = state;

            EditorApplication.update -= Update;
            SceneView.onSceneGUIDelegate -= OnSceneView;

            var labelStyleState = new GUIStyleState();
            labelStyleState.textColor = Color.white;

            labelStyle = new GUIStyle();
            labelStyle.normal = labelStyleState;

            if (onCompileFinishDisposable != null)
            {
                onCompileFinishDisposable.Dispose();
            }

            if (state)
            {
                EditorApplication.update += Update;
                SceneView.onSceneGUIDelegate += OnSceneView;

                onCompileFinishDisposable = CompileNotification.OnCompileFinishAsObservable().Subscribe(
                    _ =>
                    {
                        SceneView.RepaintAll();
                    });
            }
        }

        private static void LoadAsset()
        {
            if (isAssetLoaded) { return; }

            var compileNotificationConfig = CompileNotificationConfig.Instance;

            if (compileNotificationConfig != null)
            {
                animationTextures = compileNotificationConfig.AnimationTextures;

                backgroundTexture = EditorGUIUtility.whiteTexture;
                currentTexture = animationTextures.FirstOrDefault();

                prevTime = Time.realtimeSinceStartup;

                isAssetLoaded = true;
            }
        }

        private static void Update()
        {
            if (EditorApplication.isCompiling)
            {
                // InitializeOnLoadMethodの呼び出し時にはEditorSettingが未初期化で読み込めない為.
                // ここで読み込み処理を実行する.
                if (!isAssetLoaded)
                {
                    LoadAsset();
                }

                if (animationTextures != null)
                {
                    if (UpdateInterval < updateTime)
                    {
                        updateTime = 0f;
                        animationIndex = (animationIndex + 1) % animationTextures.Length;
                        currentTexture = animationTextures[animationIndex];

                        SceneView.RepaintAll();
                    }

                    if (Application.isPlaying)
                    {
                        updateTime += Time.deltaTime;
                    }
                    else
                    {
                        updateTime += Time.realtimeSinceStartup - prevTime;
                        prevTime = Time.realtimeSinceStartup;
                    }
                }
            }
        }

        private static void OnSceneView(SceneView sceneView)
        {
            if (EditorApplication.isCompiling && isAssetLoaded)
            {
                Handles.BeginGUI();
                {
                    DrawBackground(sceneView);
                    DrawNotification(sceneView);
                }
                Handles.EndGUI();
            }
        }

        public static void DrawBackground(SceneView sceneView)
        {
            if (backgroundTexture == null) { return; }
                
            var originColor = GUI.color;
            var viewSize = sceneView.position.size;

            GUI.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);

            GUI.DrawTexture(new Rect(0f, 0f, viewSize.x, viewSize.y), backgroundTexture);

            GUI.color = originColor;
        }

        public static void DrawNotification(SceneView sceneView)
        {
            if (backgroundTexture == null) { return; }

            if (currentTexture == null) { return; }

            var originColor = GUI.color;

            EditorGUILayout.BeginVertical();
            {
                GUILayout.Space(-10f);

                EditorGUILayout.BeginHorizontal();
                {
                    GUILayout.Space(10f);

                    var rect = EditorGUILayout.GetControlRect(GUILayout.Width(100f), GUILayout.Height(30f));

                    //----- Background -----

                    GUI.color = new Color(0.2f, 0.2f, 0.2f, 0.6f);

                    GUI.DrawTexture(rect, backgroundTexture);

                    GUI.color = originColor;

                    //----- Icon -----

                    var iconSize = new Vector2(20f, 20f);
                    var iconRect = new Rect(new Vector2(rect.xMin + iconSize.x * 0.5f + 0f, rect.center.y - iconSize.y * 0.5f), iconSize);

                    GUI.DrawTexture(iconRect, currentTexture);

                    GUILayout.FlexibleSpace();

                    //----- Label -----

                    var labelRect = new Rect(new Vector2(iconRect.xMax + 5f, iconRect.center.y - 6f), new Vector2(80f, 20f));

                    GUI.Label(labelRect, "compile", labelStyle);

                }
                EditorGUILayout.EndHorizontal();

                GUILayout.FlexibleSpace();
            }
            EditorGUILayout.EndVertical();

            GUI.color = originColor;
        }
    }
}
