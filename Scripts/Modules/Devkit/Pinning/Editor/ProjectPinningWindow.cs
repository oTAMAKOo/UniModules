
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using Extensions;
using Extensions.Devkit;
using Modules.Devkit.Prefs;

using Object = UnityEngine.Object;

namespace Modules.Devkit.Pinning
{
    public sealed class ProjectPinningWindow : PinningWindow<ProjectPinningWindow>
    {
        //----- params -----

        private const string PinnedPrefsKey = "ProjectPinningPrefs-Pinned";

        [Serializable]
        private sealed class SaveData
        {
            public string guid = null;

            public string comment = null;
        }

        //----- field -----

        //----- property -----

        protected override string WindowTitle { get { return "Project Pin"; } }

        //----- method -----

        public static void Open()
        {
            Instance.Initialize();
        }

        [InitializeOnLoadMethod]
        private static void InitializeOnLoadMethod()
        {
            if(IsExist)
            {
                Instance.Load();
            }
        }

        protected override void Save()
        {
            var saveData = new List<SaveData>();

            foreach (var item in pinning)
            {
                if (item == null || item.target == null){ continue; }

                var guid = UnityEditorUtility.GetAssetGUID(item.target);

                var data = new SaveData()
                {
                    guid = guid,
                    comment = item.comment,
                };

                saveData.Add(data);
            }

            if (saveData.Any())
            {
                ProjectPrefs.Set(PinnedPrefsKey, saveData);
            }
            else
            {
                if (ProjectPrefs.HasKey(PinnedPrefsKey))
                {
                    ProjectPrefs.DeleteKey(PinnedPrefsKey);
                }
            }
        }

        protected override void Load()
        {
            pinning = new List<PinnedItem>();

            var saveData = ProjectPrefs.Get<List<SaveData>>(PinnedPrefsKey, null);

            if (saveData == null) { return; }

            foreach (var data in saveData)
            {
                if (string.IsNullOrEmpty(data.guid)){ continue; }

                var assetPath = AssetDatabase.GUIDToAssetPath(data.guid);

                if (string.IsNullOrEmpty(assetPath)){ continue;}

                var asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);

                if (asset == null) { continue; }

                var item = new PinnedItem()
                {
                    target = asset,
                    comment = data.comment,
                };

                pinning.Add(item);
            }
        }

        protected override string GetToolTipText(Object item)
        {
            return AssetDatabase.GetAssetPath(item);
        }

        protected override string GetLabelName(Object item)
        {
            return item.name;
        }

        protected override bool ValidatePinned(Object[] items)
        {
            return items.All(x => EditorUtility.IsPersistent(x));
        }

        protected override void OnMouseLeftDown(Object item, bool doubleClick)
        {
            if (doubleClick)
            {
                EditorUtility.FocusProjectWindow();

                Selection.activeObject = item;

                EditorGUIUtility.PingObject(item);
            }
        }

    }
}
