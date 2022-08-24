
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using Extensions;
using Extensions.Devkit;

namespace Modules.Devkit.DefineSymbol
{
    public sealed class DefineSymbolWindow : SingletonEditorWindow<DefineSymbolWindow>
    {
        //----- params -----

        private readonly Vector2 WindowSize = new Vector2(700f, 500f);
        
        private sealed class DefineSymbolInfo
        {
            public bool enable = false;
            public string symbol = null;
            public string description = null;
        }

        //----- field -----

        private DefineSymbolInfo[] defineSymbolInfos = null;

        private Vector2 scrollPosition = Vector2.zero;

        [NonSerialized]
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
            
            titleContent = new GUIContent("DefineSymbols");

            minSize = WindowSize;

            defineSymbolInfos = BuilDefineSymbolInfos();

            initialized = true;
        }

        void OnGUI()
        {
            Initialize();

            GUILayout.Space(2f);

            EditorLayoutTools.ContentTitle("Current");
            
            using (new ContentsScope())
            {
                var defineSymbols = defineSymbolInfos.Where(x => x.enable).Select(x => x.symbol).ToArray();

                var defineSymbolStr= string.Join(";", defineSymbols);

                EditorGUILayout.SelectableLabel(defineSymbolStr, EditorStyles.miniTextField, GUILayout.Height(16f),  GUILayout.ExpandWidth(true));
            }

            EditorLayoutTools.ContentTitle("Define");
            
            using (new ContentsScope())
            {
                using (var scrollView = new EditorGUILayout.ScrollViewScope(scrollPosition, GUILayout.ExpandWidth(true)))
                {
                    foreach (var defineSymbolInfo in defineSymbolInfos)
                    {
                        using (new ContentsScope())
                        {
                            DrawInfoGUI(defineSymbolInfo);
                        }

                        GUILayout.Space(2f);
                    }

                    scrollPosition = scrollView.scrollPosition;
                }
            }

            GUILayout.Space(2f);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Apply", GUILayout.Width(100f)))
                {
                    var defineSymbols = defineSymbolInfos.Where(x => x.enable).Select(x => x.symbol).ToArray();

                    DefineSymbol.Set(defineSymbols, true);
                }
            }
        }

        private void DrawInfoGUI(DefineSymbolInfo info)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                info.enable = EditorGUILayout.Toggle(string.Empty, info.enable, GUILayout.Width(20f));

                EditorGUILayout.SelectableLabel(info.symbol, EditorStyles.miniTextField, GUILayout.Height(16f),  GUILayout.ExpandWidth(true));
            }
  
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(26f);

                EditorGUILayout.SelectableLabel(info.description, EditorStyles.miniTextField, GUILayout.Height(16f), GUILayout.ExpandWidth(true));
            }
        }

        private DefineSymbolInfo[] BuilDefineSymbolInfos()
        {
            var list = new List<DefineSymbolInfo>();

            var config = DefineSymbolConfig.Instance;

            var infos = config.Infos;

            var currentSymbols = DefineSymbol.Current.ToArray();

            foreach (var info in infos)
            {
                var item = new DefineSymbolInfo()
                {
                symbol = info.symbol,
                description = info.description,
                };

                // 現在定義されてる場合.
                item.enable = currentSymbols.Contains(info.symbol);

                list.Add(item);
            }

            return list.ToArray();
        }
    }
}