
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;
using Extensions;
using Extensions.Devkit;

namespace Modules.Devkit.ShaderVariant
{
    public sealed class ShaderVariantUpdateWindow : SingletonEditorWindow<ShaderVariantUpdateWindow>
    {
        //----- params -----

        private readonly Vector2 WindowSize = new Vector2(500f, 500f);

        private enum AssetViewMode
        {
            Asset,
            Path
        }

        private sealed class ShaderInfo
        {
            public string assetPath = null;
            public Shader shader = null;
            public bool add = false;
            public ShaderVariantCollection.ShaderVariant shaderVariant;
        }

        //----- field -----

        private ShaderVariantCollection shaderVariantCollection = null;
        private List<ShaderInfo> shaderInfos = null;
        private AssetViewMode assetViewMode = AssetViewMode.Asset;
        private UnityEngine.Object targetFolder = null;

        private Vector2 scrollPosition = Vector2.zero;

        //----- property -----

        //----- method -----

        public static void Open()
        {
            Instance.Initialize();

            Instance.Show();
        }

        private void Initialize()
        {
            titleContent = new GUIContent("ShaderVariantUpdate");

            minSize = WindowSize;

            shaderInfos = new List<ShaderInfo>();
        }

        void OnGUI()
        {
            GUILayout.Space(5f);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(5f);

                using (new EditorGUILayout.VerticalScope())
                {
                    DrawContents();
                }

                GUILayout.Space(5f);
            }

            GUILayout.Space(5f);
        }

        private void DrawContents()
        {
            var labelBackgroundColor = new Color(0.2f, 0.8f, 0.5f, 0.8f);

            EditorLayoutTools.DrawLabelWithBackground("ShaderVariantCollection", labelBackgroundColor, EditorLayoutTools.LabelColor);

            EditorGUI.BeginChangeCheck();

            shaderVariantCollection = EditorLayoutTools.ObjectField(shaderVariantCollection, false);

            if (EditorGUI.EndChangeCheck())
            {
                targetFolder = null;
                shaderInfos.Clear();
                scrollPosition = Vector2.zero;
            }

            GUILayout.Space(2f);

            EditorLayoutTools.DrawLabelWithBackground("TargetFolder", labelBackgroundColor, EditorLayoutTools.LabelColor);

            EditorGUI.BeginChangeCheck();

            targetFolder = EditorLayoutTools.ObjectField(targetFolder, false);

            if (EditorGUI.EndChangeCheck())
            {
                if (!UnityEditorUtility.IsFolder(targetFolder))
                {
                    targetFolder = null;
                }

                shaderInfos.Clear();
                scrollPosition = Vector2.zero;
            }

            GUILayout.Space(2f);
            
            if (shaderVariantCollection != null)
            {
                if (shaderInfos.IsEmpty())
                {
                    EditorGUILayout.HelpBox("Please search shader.", MessageType.Info);
                }
                else
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorLayoutTools.DrawLabelWithBackground("Shaders", labelBackgroundColor, EditorLayoutTools.LabelColor);

                        assetViewMode = (AssetViewMode)EditorGUILayout.EnumPopup(assetViewMode, GUILayout.Width(60f));
                    }

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("add all", GUILayout.Width(120f)))
                        {
                            shaderInfos.ForEach(x => x.add = true);
                        }

                        if (GUILayout.Button("remove all", GUILayout.Width(120f)))
                        {
                            shaderInfos.ForEach(x => x.add = false);
                        }

                        GUILayout.FlexibleSpace();
                    }

                    using (var scrollViewScope = new EditorGUILayout.ScrollViewScope(scrollPosition))
                    {
                        using (new ContentsScope())
                        {
                            foreach (var shaderInfo in shaderInfos)
                            {
                                using (new EditorGUILayout.HorizontalScope())
                                {
                                    shaderInfo.add = EditorGUILayout.Toggle(shaderInfo.add, GUILayout.Width(20f));

                                    switch (assetViewMode)
                                    {
                                        case AssetViewMode.Asset:
                                            EditorLayoutTools.ObjectField(shaderInfo.shader, false, GUILayout.Height(15f));
                                            break;

                                        case AssetViewMode.Path:
                                            EditorGUILayout.SelectableLabel(shaderInfo.assetPath, GUILayout.Height(15f));
                                            break;
                                    }
                                }

                                GUILayout.Space(2f);
                            }
                        }

                        scrollPosition = scrollViewScope.scrollPosition;
                    }
                }

                GUILayout.Space(2f);

                GUILayout.FlexibleSpace();

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("Search", GUILayout.Width(150f)))
                    {
                        BuildShaderInfos();
                    }

                    GUILayout.Space(20f);

                    using (new DisableScope(shaderInfos.IsEmpty()))
                    {
                        if (GUILayout.Button("Update", GUILayout.Width(150f)))
                        {
                            ApplyShaderVariantCollection();
                        }
                    }

                    GUILayout.FlexibleSpace();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Select shaderVariant file.", MessageType.Info);
            }
        }

        private void BuildShaderInfos()
        {
            var shaderPaths = AssetDatabase.FindAssets("t:shader")
                .Select(x => AssetDatabase.GUIDToAssetPath(x))
                .ToArray();

            if(targetFolder != null)
            {
                var folderPath = AssetDatabase.GetAssetPath(targetFolder);

                shaderPaths = shaderPaths.Where(x => x.StartsWith(folderPath)).ToArray();
            }

            shaderInfos.Clear();

            foreach (var shaderPath in shaderPaths)
            {
                var shader = AssetDatabase.LoadMainAssetAtPath(shaderPath) as Shader;

                if (shader == null) { continue; }

                var shaderVariant = new ShaderVariantCollection.ShaderVariant();

                shaderVariant.shader = shader;

                var info = new ShaderInfo()
                {
                    assetPath = shaderPath,
                    shader = shader,
                    add = shaderVariantCollection.Contains(shaderVariant),
                    shaderVariant = shaderVariant,
                };

                shaderInfos.Add(info);
            }
        }

        private void ApplyShaderVariantCollection()
        {
            shaderVariantCollection.Clear();

            foreach (var shaderInfo in shaderInfos)
            {
                if (!shaderInfo.add) { continue; }

                shaderVariantCollection.Add(shaderInfo.shaderVariant);
            }

            EditorUtility.DisplayDialog("Result", "ShaderVariantCollection update complete.", "OK");
        }
    }
}
