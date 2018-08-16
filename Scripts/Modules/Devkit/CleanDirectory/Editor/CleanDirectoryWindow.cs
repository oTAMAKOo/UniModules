﻿﻿﻿﻿﻿
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using UniRx;
using Extensions;
using Extensions.Devkit;

namespace Modules.Devkit.CleanDirectory
{
    public class CleanDirectoryWindow : EditorWindow
    {
        //----- params -----

        private static readonly string WindowTitle = "Clean Directory";

        //----- field -----

        private List<DirectoryInfo> emptyDirectorys = null;
        private Vector2 scrollPosition = Vector2.zero;

        private static CleanDirectoryWindow instance = null;

        //----- property -----
        
        private bool hasEmptyDirectorys
        {
            get
            {
                return emptyDirectorys != null && emptyDirectorys.Any();
            }
        }

        //----- method -----

        const float DIR_LABEL_HEIGHT = 21;
        
        public static void Open()
        {
            instance = EditorWindow.GetWindow<CleanDirectoryWindow>();

            if (instance != null)
            {
                instance.Initialize();
            }
        }

        private void Initialize()
        {
            titleContent = new GUIContent(WindowTitle);

            emptyDirectorys = new List<DirectoryInfo>();

            ShowUtility();
        }
        
        void OnGUI()
        {
            using (new EditorGUILayout.VerticalScope())
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Find Empty Dirs"))
                    {
                        CleanDirectoryUtility.FillEmptyDirList(out emptyDirectorys);

                        if (!hasEmptyDirectorys)
                        {
                            ShowNotification(new GUIContent("No Empty Directory"));
                        }
                        else
                        {
                            RemoveNotification();
                        }
                    }

                    if (EditorLayoutTools.ColorButton("Delete All", hasEmptyDirectorys, Color.red))
                    {
                        CleanDirectoryUtility.DeleteAllEmptyDirAndMeta(ref emptyDirectorys);
                        ShowNotification(new GUIContent("Deleted All"));
                    }
                }

                var cleanOnSave = CleanDirectoryUtility.Prefs.cleanOnSave;
                var toggle = GUILayout.Toggle(cleanOnSave, " Clean Empty Dirs Automatically On Save");

                if (cleanOnSave != toggle)
                {
                    CleanDirectoryUtility.Prefs.cleanOnSave = toggle;
                }

                GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));

                if (hasEmptyDirectorys)
                {
                    using (var scrollViewScope = new EditorGUILayout.ScrollViewScope(scrollPosition))
                    {
                        using (new EditorGUILayout.VerticalScope())
                        {
                            var folderContent = new GUIContent();

                            foreach (var dirInfo in emptyDirectorys)
                            {
                                var assetObj = AssetDatabase.LoadAssetAtPath("Assets", typeof(UnityEngine.Object));

                                if (assetObj != null)
                                {
                                    folderContent.text = CleanDirectoryUtility.GetRelativePath(dirInfo.FullName, Application.dataPath);
                                    GUILayout.Label(folderContent, GUILayout.Height(DIR_LABEL_HEIGHT));
                                }
                            }
                        }

                        scrollPosition = scrollViewScope.scrollPosition;
                    }
                }
            }
        }
    }
}