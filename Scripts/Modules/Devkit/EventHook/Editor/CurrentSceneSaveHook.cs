﻿﻿
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using System;

namespace Modules.Devkit.EventHook
{
    public class CurrentSceneSaveHook : UnityEditor.AssetModificationProcessor
    {
        public static Action onSave = null;

        public static string[] OnWillSaveAssets(string[] paths)
        {
            var currentScenePath = SceneManager.GetActiveScene().path;

            foreach (string path in paths)
            {
                if (path.Contains(".unity"))
                {
                    if (currentScenePath == path)
                    {
                        if (onSave != null)
                        {
                            onSave();
                        }
                    }
                }
            }

            return paths;
        }
    }
}