
#if ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_SOFDEC

using UnityEngine;
using UnityEditor;
using Extensions.Devkit;

namespace Modules.CriWare.Editor
{
    public sealed class CriAssetUpdateWindow : SingletonEditorWindow<CriAssetUpdateWindow>
    {
        //----- params -----

        private readonly Vector2 WindowSize = new Vector2(280f, 60f);

        //----- field -----

        private bool initialized = false;

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

            titleContent = new GUIContent("CriAssetUpdater");

            minSize = WindowSize;
            maxSize = WindowSize;

            initialized = true;
        }

        void OnGUI()
        {
            EditorGUILayout.Separator();

            var backgroundColor = new Color(0.3f, 0.3f, 0.3f);
            var labelColor = new Color(0.8f, 0.8f, 0.8f, 0.8f);

            EditorLayoutTools.Title("Import from AtomCraft folder", backgroundColor, labelColor);

            if (GUILayout.Button("Import"))
            {
                CriAssetUpdater.Execute();
            }

            EditorGUILayout.Separator();
        }
    }
}

#endif
