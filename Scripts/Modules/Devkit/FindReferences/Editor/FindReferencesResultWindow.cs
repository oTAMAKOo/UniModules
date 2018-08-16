﻿﻿
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Extensions.Devkit;

using Object = UnityEngine.Object;

namespace Modules.Devkit.FindReferences
{
    public class FindReferencesResultWindow : SingletonEditorWindow<FindReferencesResultWindow>
    {
        //----- params -----

        private enum DisplayType
        {
            Asset,
            Path,
        }

        private class ReferenceInfo
        {
            public string TargetPath { get; private set; }
            public Object Target { get; private set; }
            public Object Object { get; private set; }
            public Dictionary<string, Object> Dependencies { get; private set; }

            public ReferenceInfo(AssetReferenceInfo info)
            {
                TargetPath = info.TargetPath;
                Target = info.Target;
                Object = AssetDatabase.LoadAssetAtPath(info.TargetPath, typeof(Object));
                Dependencies = new Dictionary<string, Object>();

                foreach (var item in info.Dependencies)
                {
                    Dependencies.Add(item, AssetDatabase.LoadAssetAtPath(item, typeof(Object)));
                }
            }
        }

        //----- field -----

        private Object targetAsset = null;
        private ReferenceInfo referenceInfo = null;
        private Vector2 scrollPosition = Vector2.zero;
        private DisplayType displayType = DisplayType.Asset;

        //----- property -----

        //----- method -----

        public static void Open(Object targetAsset, AssetReferenceInfo assetReferenceInfo)
        {
            if(!IsExsist)
            {
                Instance.titleContent = new GUIContent("Find References In Project");
                Instance.Show();
            }
            
            Instance.SetParams(targetAsset, assetReferenceInfo);
        }

        private void SetParams(Object targetAsset, AssetReferenceInfo assetReferenceInfo)
        {
            this.targetAsset = targetAsset;

            referenceInfo = assetReferenceInfo != null ? new ReferenceInfo(assetReferenceInfo) : null;

            displayType = DisplayType.Asset;
            scrollPosition = Vector2.zero;
        }

        void OnGUI()
        {
            var originLabelWidth = EditorLayoutTools.SetLabelWidth(50f);

            if (referenceInfo != null)
            {
                GUILayout.Space(15f);

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Space(5f);

                    using (new EditorGUILayout.VerticalScope())
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.ObjectField("Target", targetAsset, typeof(Object), false, GUILayout.Width(250f));

                            GUILayout.Space(15f);

                            displayType = (DisplayType)EditorGUILayout.EnumPopup(displayType, GUILayout.Width(60f));

                            GUILayout.FlexibleSpace();
                        }

                        GUILayout.Space(5f);

                        EditorLayoutTools.DrawLabelWithBackground("References", EditorLayoutTools.BackgroundColor, EditorLayoutTools.LabelColor);

                        using (var scrollViewScope = new EditorGUILayout.ScrollViewScope(scrollPosition))
                        {
                            foreach (var item in referenceInfo.Dependencies)
                            {
                                switch (displayType)
                                {
                                    case DisplayType.Asset:
                                        EditorGUILayout.ObjectField(item.Value, typeof(Object), false);
                                        break;

                                    case DisplayType.Path:
                                        using (new EditorGUILayout.HorizontalScope())
                                        {
                                            EditorGUILayout.SelectableLabel(item.Key, EditorStyles.textArea, GUILayout.Height(18f));

                                            GUILayout.Space(5f);

                                            if(GUILayout.Button("select", GUILayout.Width(50f)))
                                            {
                                                Selection.activeObject = item.Value;
                                            }
                                        }
                                        break;
                                }

                                GUILayout.Space(2f);
                            }

                            scrollPosition = scrollViewScope.scrollPosition;
                        }

                        GUILayout.Space(5f);
                    }

                    GUILayout.Space(5f);
                }

                GUILayout.Space(15f);
            }
            else
            {
                EditorGUILayout.HelpBox("This object is not referenced.", MessageType.Info);
            }

            EditorLayoutTools.SetLabelWidth(originLabelWidth);
        }
    }
}