
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using Extensions;
using Extensions.Devkit;
using Modules.Devkit.Prefs;

using Object = UnityEngine.Object;

namespace Modules.Devkit.Pinning
{
    public sealed class HierarchyPinningWindow : PinningWindow<HierarchyPinningWindow>
    {
        //----- params -----

        private const string PinnedPrefsKeyFormat = "HierarchyPinningPrefs-Pinned-{0}";

        [Serializable]
        private sealed class SaveData
        {
            public long localIdentifierInFile = -1;

            public string comment = null;
        }

        //----- field -----

        private string currentScenePath = null;

        //----- property -----

        protected override string WindowTitle { get { return "Hierarchy Pin"; } }

        //----- method -----

        public static void Open()
        {
            Instance.Initialize();
        }

        [InitializeOnLoadMethod]
        private static void InitializeOnLoadMethod()
        {
            if (IsExist)
            {
                Instance.Load();
            }
        }

        protected override void Save()
        {
            var prefsKey = GetPinnedPrefsKey();

            var saveData = new List<SaveData>();

            foreach (var item in pinning)
            {
                if (item == null || item.target == null) { continue; }

                var localIdentifierInFile = UnityEditorUtility.GetLocalIdentifierInFile(item.target);

                if (localIdentifierInFile == -1){ continue; }

                var data = new SaveData()
                {
                    localIdentifierInFile = localIdentifierInFile,
                    comment = item.comment,
                };

                saveData.Add(data);
            }

            if (saveData.Any())
            {
                ProjectPrefs.Set(prefsKey, saveData);
            }
            else
            {
                if (ProjectPrefs.HasKey(prefsKey))
                {
                    ProjectPrefs.DeleteKey(prefsKey);
                }
            }
        }

        protected override void Load()
        {
            pinning = new List<PinnedItem>();

            var prefsKey = GetPinnedPrefsKey();

            var saveData = ProjectPrefs.Get<List<SaveData>>(prefsKey, null);

            if (saveData == null) { return; }

            if (saveData.Any())
            {
                // Hierarchy上のGameObjectを検索して取得.
                var hierarchyObjects = UnityEditorUtility.FindAllObjectsInHierarchy();

                foreach (var data in saveData)
                {
                    if (data.localIdentifierInFile == -1){ continue; }

                    var targetObject = hierarchyObjects.FirstOrDefault(y => UnityEditorUtility.GetLocalIdentifierInFile(y) == data.localIdentifierInFile) as Object;

                    if (targetObject == null) { continue; }

                    var item = new PinnedItem()
                    {
                        target = targetObject,
                        comment = data.comment,
                    };

                    pinning.Add(item);
                }
            }
        }

        private string GetPinnedPrefsKey()
        {
            var currentScene = SceneManager.GetActiveScene();
            
            return string.Format(PinnedPrefsKeyFormat, currentScene.path.GetCRC());
        }

        protected override string GetToolTipText(Object item)
        {
            var gameObject = item as GameObject;
            var hierarchyPath = string.Empty;

            if(gameObject != null)
            {
                hierarchyPath = PathUtility.Combine(UnityUtility.GetChildHierarchyPath(null, gameObject), gameObject.transform.name);
            }

            return hierarchyPath;
        }

        protected override string GetLabelName(Object item)
        {
            return item.name;
        }

        protected override bool ValidatePinned(Object[] items)
        {
            if (Application.isPlaying) { return false; }

            foreach (var item in items)
            {
                var localIdentifierInFile = UnityEditorUtility.GetLocalIdentifierInFile(item);

                // Sceneに保存されていないGameObjectは登録不可.
                if (localIdentifierInFile <= 0){ return false; }

                // Hierarchyのオブジェクト以外が登録不可.
                if (EditorUtility.IsPersistent(item)){ return false; }
            }

            return true;
        }

        protected override void UpdatePinnedObject()
        {
            var scene = SceneManager.GetActiveScene();

            if (!string.IsNullOrEmpty(currentScenePath))
            {
                if (currentScenePath != scene.path)
                {
                    Load();
                }
            }

            currentScenePath = scene.path;

            base.UpdatePinnedObject();
        }

        protected override void OnMouseLeftDown(Object item, bool doubleClick)
        {
            Selection.activeObject = item;

            EditorGUIUtility.PingObject(item);
        }
    }
}
