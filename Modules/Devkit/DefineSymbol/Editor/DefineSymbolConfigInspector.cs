
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using Extensions;
using Extensions.Devkit;

namespace Modules.Devkit.DefineSymbol
{
    [CustomEditor(typeof(DefineSymbolConfig))]
    public sealed class DefineSymbolConfigInspector : UnityEditor.Editor
    {
        //----- params -----

        //----- field -----

        private bool isChanged = false;

        private Vector2 scrollPosition = Vector2.zero;

        private GUIContent toolbarPlusIcon = null;
        private GUIContent toolbarMinusIcon = null;

        private GUIStyle inputFieldStyle = null;

        [NonSerialized]
        private bool initialized = false;

        //----- property -----

        //----- method -----

        private void Initialize()
        {
            if (initialized){ return; }

            toolbarPlusIcon = EditorGUIUtility.IconContent("Toolbar Plus");
            toolbarMinusIcon = EditorGUIUtility.IconContent("Toolbar Minus");

            initialized = true;
        }

        private void InitializeStyle()
        {
            if (inputFieldStyle == null)
            {
                inputFieldStyle = new GUIStyle(EditorStyles.miniTextField);
            }
        }

        void OnEnable()
        {
            isChanged = false;
        }

        void OnDisable()
        {
            var config = DefineSymbolConfig.Instance;

            if (isChanged)
            {
                EditorUtility.SetDirty(config);
                AssetDatabase.SaveAssetIfDirty(config);
            }
        }

        public override void OnInspectorGUI()
        {
            Initialize();

            InitializeStyle();

            var config = DefineSymbolConfig.Instance;

            var infos = config.Infos.ToList();

            var removeInfos = new List<DefineSymbolConfig.DefineSymbolInfo>();

            var update = false;

            GUILayout.Space(4f);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                if (GUILayout.Button(toolbarPlusIcon, GUILayout.Width(50f), GUILayout.Height(16f)))
                {
                    var info = new DefineSymbolConfig.DefineSymbolInfo();

                    infos.Add(info);

                    update = true;
                }

                GUILayout.Space(8f);
            }

            var swapTargets = new List<Tuple<int, int>>();

            using (var scrollView = new EditorGUILayout.ScrollViewScope(scrollPosition, GUILayout.ExpandWidth(true)))
            {
                for (int i = 0; i < infos.Count; i++)
                {
                    var info = infos[i];

                    using (new ContentsScope())
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUI.BeginChangeCheck();

                            var index = EditorGUILayout.DelayedIntField("Index", i, inputFieldStyle, GUILayout.Height(16f), GUILayout.ExpandWidth(true));

                            if (EditorGUI.EndChangeCheck())
                            {
                                if (0 <= index && index < infos.Count)
                                {
                                    swapTargets.Add(Tuple.Create(i, index));
                                }
                            }

                            GUILayout.FlexibleSpace();

                            if (GUILayout.Button(toolbarMinusIcon, EditorStyles.miniButton, GUILayout.Width(35f)))
                            {
                                removeInfos.Add(info);
                            }
                        }

                        GUILayout.Space(2f);

                        EditorGUI.BeginChangeCheck();
                        
                        info.symbol = EditorGUILayout.DelayedTextField("DefineSymbol", info.symbol, inputFieldStyle, GUILayout.Height(16f), GUILayout.ExpandWidth(true));
                        
                        GUILayout.Space(2f);

                        info.description = EditorGUILayout.DelayedTextField("Description", info.description, inputFieldStyle, GUILayout.Height(16f), GUILayout.ExpandWidth(true));

                        if (EditorGUI.EndChangeCheck())
                        {
                            if (!string.IsNullOrEmpty(info.symbol))
                            {
                                info.symbol = info.symbol.ToUpper();
                            }

                            update = true;
                        }
                    }

                    GUILayout.Space(4f);
                }

                scrollPosition = scrollView.scrollPosition;
            }

            if (swapTargets.Any())
            {
                foreach (var swapTarget in swapTargets)
                {
                    infos = infos.Swap(swapTarget.Item1, swapTarget.Item2).ToList();
                }

                update = true;
            }

            if (removeInfos.Any())
            {
                foreach (var info in removeInfos)
                {
                    infos.Remove(info);
                }

                update = true;
            }

            if (update)
            {
                Reflection.SetPrivateField(config, "infos", infos.ToArray());

                isChanged = true;
            }
        }
    }
}