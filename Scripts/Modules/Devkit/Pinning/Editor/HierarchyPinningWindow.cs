﻿﻿
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Extensions.Devkit;
using Modules.Devkit.Prefs;

namespace Modules.Devkit.Pinning
{
    public class HierarchyPinningWindow : PinningWindow<HierarchyPinningWindow>
    {
        //----- params -----

        //----- field -----

        private Scene currentScene;
        private int[] pinnedObjectIds = null;

        //----- property -----

        protected override string WindowTitle { get { return "Hierarchy Pin"; } }

        protected override string PinnedPrefsKey
        {
            get
            {
                return string.Format("HierarchyPinningPrefs-Pinned-{0}", currentScene.path.GetHashCode());
            }
        }

        //----- method -----

        public static void Open()
        {
            Instance.Initialize();
        }

        [InitializeOnLoadMethod]
        private static void InitializeOnLoadMethod()
        {
            if (IsExsist)
            {
                Instance.Load();
            }
        }

        public override void Initialize()
        {
            base.Initialize();

            EditorApplication.hierarchyChanged += () => { UpdatePinnedObjectIds(); };
        }

        protected override void Save()
        {
            currentScene = SceneManager.GetActiveScene();

            UpdatePinnedObjectIds();

            ProjectPrefs.SetString(PinnedPrefsKey, string.Join(",", pinnedObjectIds.Select(x => x.ToString()).ToArray()));
        }

        protected override void Load()
        {
            currentScene = SceneManager.GetActiveScene();

            var pinned = ProjectPrefs.GetString(PinnedPrefsKey);

            if (string.IsNullOrEmpty(pinned)) { return; }

            // Hierarchy上のGameObjectを検索して取得.
            var hierarchyObjects = UnityEditorUtility.FindAllObjectsInHierarchy();

            pinnedObject = pinned
                .Split(',')
                .Select(x => hierarchyObjects.FirstOrDefault(y => LocalIdentifierInFile.Get(y).ToString() == x) as Object)
                .Where(x => x != null)
                .ToList();

            UpdatePinnedObjectIds();
        }

        private void UpdatePinnedObjectIds()
        {
            pinnedObjectIds = pinnedObject
                .Select(x => x as GameObject)
                .Where(x => x != null)
                .Select(x => LocalIdentifierInFile.Get(x))
                .ToArray();
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
                var fileId = LocalIdentifierInFile.Get(item);

                // Sceneに保存されていないGameObjectは登録不可.
                if (fileId == item.GetInstanceID()){ return false; }

                // Hierarchyのオブジェクト以外が登録不可.
                if (EditorUtility.IsPersistent(item)){ return false; }
            }

            return true;
        }

        protected override void UpdatePinnedObject()
        {
            var scene = SceneManager.GetActiveScene();

            if (currentScene != scene)
            {
                Load();
            }

            base.UpdatePinnedObject();
        }
    }
}
