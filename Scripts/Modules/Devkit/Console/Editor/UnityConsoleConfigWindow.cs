
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Extensions.Devkit;
using Modules.Devkit.Prefs;

namespace Modules.Devkit.Console
{
    public sealed class UnityConsoleConfigWindow : SingletonEditorWindow<UnityConsoleConfigWindow>
    {
        //----- params -----

        private static readonly Vector2 WindowSize = new Vector2(250f, 400f);

        //----- field -----

        private Vector2 scrollPosition = Vector2.zero;

        private List<ConsoleInfo> consoleInfos = null;

        private bool initialized = false;

        //----- property -----

        //----- method -----

        public static void Open()
        {
            Instance.Initialize();

            Instance.ShowUtility();
        }

        private void Initialize()
        {
            if (initialized) { return; }

            titleContent = new GUIContent("UnityConsole");

            minSize = WindowSize;
            maxSize = WindowSize;

            LoadInfos();

            initialized = true;
        }

        private void LoadInfos()
        {
            var unityConsoleManager = UnityConsoleManager.Instance;
            
            consoleInfos = unityConsoleManager.Load().ToList();

            DeleteUndefinedInfos();

            SetupDefaultInfos();
        }

        void OnGUI()
        {
            var unityConsoleManager = UnityConsoleManager.Instance;

            EditorGUILayout.Separator();

            var isEnable = unityConsoleManager.IsEnable();

            EditorGUI.BeginChangeCheck();

            isEnable = EditorGUILayout.Toggle("UnityConsole Enable", isEnable);

            if (EditorGUI.EndChangeCheck())
            {
                unityConsoleManager.SetEnable(isEnable);
            }

            GUILayout.Space(2f);
            
            using (new ContentsScope())
            {
                using (var scrollViewScope = new EditorGUILayout.ScrollViewScope(scrollPosition))
                {
                    var removeList = new List<ConsoleInfo>();

                    foreach (var consoleInfo in consoleInfos)
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            using (new DisableScope(consoleInfo.eventName == UnityConsole.InfoEvent.ConsoleEventName))
                            {
                                consoleInfo.eventName = EditorGUILayout.DelayedTextField(consoleInfo.eventName);
                            }

                            GUILayout.Space(5f);

                            using (new LabelWidthScope(0f))
                            {
                                consoleInfo.enable = EditorGUILayout.Toggle(consoleInfo.enable, GUILayout.Width(18f));
                            }

                            var disableDelete = false;

                            disableDelete |= consoleInfo.eventName == UnityConsole.InfoEvent.ConsoleEventName;
                            disableDelete |= IsDefinedInfo(consoleInfo.eventName);

                            using (new DisableScope(disableDelete))
                            {
                                if (GUILayout.Button("-", GUILayout.Height(18f)))
                                {
                                    removeList.Add(consoleInfo);
                                }
                            }
                        }
                    }

                    foreach (var item in removeList)
                    {
                        consoleInfos.Remove(item);
                    }

                    scrollPosition = scrollViewScope.scrollPosition;
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("+"))
                {
                    var newInfo = new ConsoleInfo()
                    {
                        eventName = string.Empty,
                        enable = true,
                    };

                    consoleInfos.Add(newInfo);
                }

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Save"))
                {
                    unityConsoleManager.Save(consoleInfos);
                }
            }
        }

        private void SetupDefaultInfos()
        {
            var config = UnityConsoleConfig.Instance;

            if (config == null){ return; }

            var unityConsoleManager = UnityConsoleManager.Instance;
            
            var definedInfos = config.GetDefinedInfos();

            var hasChange = false;

            foreach (var info in definedInfos)
            {
                if (consoleInfos.Any(x => x.eventName == info.eventName)){ continue; }

                var newInfo = new ConsoleInfo()
                {
                    eventName = info.eventName,
                    enable = info.enable,
                };

                consoleInfos.Add(newInfo);

                hasChange = true;
            }

            if (hasChange)
            {
                unityConsoleManager.Save(consoleInfos);
            }
        }

        private void DeleteUndefinedInfos()
        {
            var config = UnityConsoleConfig.Instance;

            if (config == null) { return; }

            var unityConsoleManager = UnityConsoleManager.Instance;
            
            var defaultInfos = config.GetDefinedInfos();

            consoleInfos = unityConsoleManager.ConsoleInfos.ToList();

            var removeInfos = new List<ConsoleInfo>();

            foreach (var info in consoleInfos)
            {
                if (info.eventName == UnityConsole.InfoEvent.ConsoleEventName){ continue; }

                if (defaultInfos.Any(x => x.eventName == info.eventName)){ continue; }

                removeInfos.Add(info);
            }
            
            foreach (var info in removeInfos)
            {
                consoleInfos.Remove(info);
            }

            if (removeInfos.Any())
            {
                unityConsoleManager.Save(consoleInfos);
            }
        }

        private bool IsDefinedInfo(string eventName)
        {
            var config = UnityConsoleConfig.Instance;

            if (config == null) { return false; }

            var definedInfos = config.GetDefinedInfos();

            return definedInfos.Any(x => x.eventName == eventName);
        }
    }
}
