
using UnityEngine;
using UnityEditor;
using Modules.Devkit.Prefs;

namespace Modules.Devkit.Hierarchy
{
    public sealed class ActiveToggleDrawer : ItemContentDrawer
    {
        //----- params -----

        private const float ToggleSize = 16f;

        public static class Prefs
        {
            public static bool enable
            {
                get { return ProjectPrefs.GetBool("ActiveToggleDrawerPrefs-enable", false); }
                set { ProjectPrefs.SetBool("ActiveToggleDrawerPrefs-enable", value); }
            }
        }

        //----- field -----

        //----- property -----

        public override int Priority { get { return 0; } }

        public override bool Enable { get { return Prefs.enable; } }

        //----- method -----

        public override void Initialize() { }

        public override Rect Draw(GameObject targetObject, Rect rect)
        {
            var toggleRect = rect;

            rect.center = Vector.SetX(rect.center, rect.center.x - ToggleSize);

            EditorGUI.BeginChangeCheck();

            var activeSelf = GUI.Toggle(toggleRect, targetObject.activeSelf, string.Empty);

            if (EditorGUI.EndChangeCheck())
            {
                targetObject.SetActive(activeSelf);

                EditorUtility.SetDirty(targetObject);
            }

            return rect;
        }
    }
}
