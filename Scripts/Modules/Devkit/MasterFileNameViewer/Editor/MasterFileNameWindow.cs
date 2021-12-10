
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using Extensions;
using Extensions.Devkit;

namespace Modules.Master
{
    public abstract class MasterFileNameWindow<TInstance> : SingletonEditorWindow<TInstance> where TInstance : MasterFileNameWindow<TInstance>
    {
        //----- params -----

        private readonly Vector2 WindowSize = new Vector2(500f, 400f);

        private const string MasterSuffix = "Master";

        //----- field -----

        [NonSerialized]
        private bool initialized = false;

        private IMaster[] masters = null;

        private Dictionary<IMaster, string> masterNameDictionary = null;
        private Dictionary<IMaster, string> masterFileNameDictionary = null;

        private string searchText = null;

        private Vector2 scrollPosition = Vector2.zero;

        private IMaster[] displayContents = null;

        //----- property -----

        //----- method -----

        protected void Initialize(IMaster[] masters, AesCryptoKey cryptoKey)
        {
            if (initialized){ return; }

            titleContent = new GUIContent("MasterFileNameViewer");

            minSize = WindowSize;

            this.masters = masters;

            var masterManager = MasterManager.Instance;

            masterManager.SetCryptoKey(cryptoKey);

            masterNameDictionary = new Dictionary<IMaster, string>();

            foreach (var master in masters)
            {
                var masterName = GetMasterDisplayName(master);

                masterNameDictionary[master] = masterName;
            }

            masterFileNameDictionary = new Dictionary<IMaster, string>();

            foreach (var master in masters)
            {
                var fileName = MasterManager.Instance.GetMasterFileName(master.GetType());

                masterFileNameDictionary[master] = fileName;
            }

            displayContents = GetDisplayMasters();

            initialized = true;
        }

        void OnGUI()
        {
            if (!initialized){ return; }

            // Toolbar.

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar, GUILayout.Height(15f)))
            {
                Action<string> onChangeSearchText = x =>
                {
                    searchText = x;
                    displayContents = GetDisplayMasters();
                };

                Action onSearchCancel = () =>
                {
                    searchText = string.Empty;
                    displayContents = GetDisplayMasters();
                };

                EditorLayoutTools.DrawToolbarSearchTextField(searchText, onChangeSearchText, onSearchCancel, GUILayout.MinWidth(150f));

                GUILayout.FlexibleSpace();
            }

            // ScrollView.

            if (displayContents != null)
            {
                EditorGUILayout.Space(2);

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("MasterName", EditorStyles.miniButton, GUILayout.Height(15f));

                    EditorGUILayout.Space(2f, false);

                    EditorGUILayout.LabelField("FileName", EditorStyles.miniButton, GUILayout.Height(15f));
                }

                EditorGUILayout.Separator();

                using (var scrollViewScope = new EditorGUILayout.ScrollViewScope(scrollPosition))
                {
                    for (var i = 0; i < displayContents.Length; i++)
                    {
                        var master = displayContents[i];

                        var masterName = masterNameDictionary.GetValueOrDefault(master);
                        var fileName = masterFileNameDictionary.GetValueOrDefault(master);

                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.SelectableLabel(masterName, EditorStyles.textField, GUILayout.Height(18f));

                            EditorGUILayout.Space(2f, false);

                            EditorGUILayout.SelectableLabel(fileName, EditorStyles.textField, GUILayout.Height(18f));
                        }

                        EditorGUILayout.Space(2f);
                    }

                    scrollPosition = scrollViewScope.scrollPosition;
                }

                EditorGUILayout.Space(5f);
            }
        }

        private string GetMasterDisplayName(IMaster master)
        {
            var masterName = string.Empty;

            var type = master.GetType();

            masterName = type.Name;

            // 末尾が「Master」だったら末尾を削る.
            if (masterName.EndsWith(MasterSuffix))
            {
                masterName = masterName.SafeSubstring(0, masterName.Length - MasterSuffix.Length);
            }

            return masterName;
        }

        private IMaster[] GetDisplayMasters()
        {
            if (string.IsNullOrEmpty(searchText)) { return masters; }

            var list = new List<IMaster>();

            var keywords = searchText.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            for (var i = 0; i < keywords.Length; ++i)
            {
                keywords[i] = keywords[i].ToLower();
            }

            foreach (var master in masters)
            {
                var type = master.GetType();

                if (type.Name.IsMatch(keywords))
                {
                    list.Add(master);
                }

                var fileName = masterFileNameDictionary.GetValueOrDefault(master);

                if (!string.IsNullOrEmpty(fileName))
                {
                    if (fileName.IsMatch(keywords))
                    {
                        list.Add(master);
                    }
                }
            }

            return list.ToArray();
        }
    }
}
