
using UnityEngine;
using UnityEditor;
using System.Linq;
using Modules.Devkit.Prefs;

namespace Modules.Devkit.Pinning
{
    public sealed class ProjectPinningWindow : PinningWindow<ProjectPinningWindow>
    {
        //----- params -----

        //----- field -----

        //----- property -----

        protected override string WindowTitle { get { return "Project Pin"; } }

        protected override string PinnedPrefsKey { get { return "ProjectPinningPrefs-Pinned"; } }

        //----- method -----

        public static void Open()
        {
            Instance.Initialize();
        }

        [InitializeOnLoadMethod]
        private static void InitializeOnLoadMethod()
        {
            if(IsExsist)
            {
                Instance.Load();
            }
        }

        protected override void Save()
        {
            var pinnedObjectGUIDs = pinnedObject
                .Where(x => x != null)
                .Select(x => AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(x)))
                .ToArray();

            ProjectPrefs.SetString(PinnedPrefsKey, string.Join(",", pinnedObjectGUIDs));
        }

        protected override void Load()
        {
            var pinned = ProjectPrefs.GetString(PinnedPrefsKey);

            if (string.IsNullOrEmpty(pinned)) { return; }

            pinnedObject = pinned
                .Split(',')
                .Select(x => AssetDatabase.GUIDToAssetPath(x))
                .Select(x => AssetDatabase.LoadAssetAtPath<Object>(x))
                .Where(x => x != null)
                .ToList();
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

        protected override void OnMouseLeftDown(Object item, int clickCount)
        {
            EditorUtility.FocusProjectWindow();

            Selection.activeObject = item;

            EditorGUIUtility.PingObject(item);

            if(clickCount == 2)
            {
                AssetDatabase.OpenAsset(item);
            }
        }

    }
}
