﻿﻿
using UnityEngine;
using UnityEditor;
using System;
using UniRx;
using Modules.Devkit.Prefs;

namespace Modules.Devkit.AssemblyCompilation
{
    public static class CompileNotificationView
    {
        //----- params -----

        public static class Prefs
        {
            public static bool enable
            {
                get { return ProjectPrefs.GetBool(typeof(Prefs).FullName + "-enable", false); }
                set { ProjectPrefs.SetBool(typeof(Prefs).FullName + "-enable", value); }
            }
        }

        //----- field -----
        
        private static Texture backgroundTexture = null;

        private static IDisposable onCompileFinishDisposable = null;

        //----- property -----

        //----- method -----

        [InitializeOnLoadMethod]
        private static void InitializeOnLoadMethod()
        {
            SetEnable(Prefs.enable);
        }

        public static void SetEnable(bool state)
        {
            Prefs.enable = state;

            #if UNITY_2019_4_OR_NEWER

            SceneView.duringSceneGui -= OnSceneView;

            #else

            SceneView.onSceneGUIDelegate -= OnSceneView;

            #endif

            var labelStyleState = new GUIStyleState();
            labelStyleState.textColor = Color.white;

            if (onCompileFinishDisposable != null)
            {
                onCompileFinishDisposable.Dispose();
            }

            if (state)
            {
                backgroundTexture = EditorGUIUtility.whiteTexture;

                #if UNITY_2019_4_OR_NEWER

                SceneView.duringSceneGui += OnSceneView;

                #else

                SceneView.onSceneGUIDelegate += OnSceneView;

                #endif

                onCompileFinishDisposable = CompileNotification.OnCompileFinishAsObservable().Subscribe(_ => SceneView.RepaintAll());
            }
        }

        private static void OnSceneView(SceneView sceneView)
        {
            if (!EditorApplication.isCompiling) { return; }

            Handles.BeginGUI();
            {
                DrawBackground(sceneView);
            }
            Handles.EndGUI();
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
    }
}
