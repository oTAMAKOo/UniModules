
using UnityEngine;
using UnityEditor;
using Unity.Linq;
using System.Linq;
using System.Collections.Generic;
using Extensions;
using Modules.Devkit.Prefs;

namespace Modules.Devkit.Hierarchy
{
    public sealed class MissingComponentDrawer : ItemContentDrawer
    {
        //----- params -----

        private static readonly Vector2 MissingIconSize = new Vector2(16f, 14f);

        private const int UpdateInterval = 1000;

        public static class Prefs
        {
            public static bool enable
            {
                get { return ProjectPrefs.GetBool("MissingComponentDrawerPrefs-enable", true); }
                set { ProjectPrefs.SetBool("MissingComponentDrawerPrefs-enable", value); }
            }
        }

        //----- field -----

        private GUIContent missingIconGUIContent = null;

        private Dictionary<GameObject, bool> missingSearchDictionary = null;

        private int frameCount = 0;

        //----- property -----

        public override int Priority { get { return 10; } }

        public override bool Enable
        {
            get { return !Application.isPlaying && Prefs.enable; }
        }

        //----- method -----

        public override void Initialize()
        {
            missingIconGUIContent = EditorGUIUtility.IconContent("d_console.warnicon.sml");

            missingSearchDictionary = new Dictionary<GameObject, bool>();

            EditorApplication.update += OnEditorUpdate;
        }

        public override Rect Draw(GameObject targetObject, Rect rect)
        {
            SearchMissingComponents(targetObject);

            var hasMissingComponent = missingSearchDictionary.GetValueOrDefault(targetObject);
            
            if (hasMissingComponent)
            {
                var iconOffsetX = MissingIconSize.x - 0.5f;

                rect.center = Vector.SetX(rect.center, rect.center.x - 2f);

                rect.center = Vector.SetY(rect.center, rect.center.y);

                using (new EditorGUIUtility.IconSizeScope(MissingIconSize))
                {
                    EditorGUI.LabelField(rect, missingIconGUIContent);
                }

                rect.center = Vector.SetX(rect.center, rect.center.x - iconOffsetX);
            }

            return rect;
        }

        private void OnEditorUpdate()
        {
            frameCount++;

            if (UpdateInterval < frameCount)
            {
                missingSearchDictionary.Clear();

                frameCount = 0;
            }
        }

        private bool SearchMissingComponents(GameObject targetObject)
        {
            // 既に検索済みなら検索しない.
            if (missingSearchDictionary.ContainsKey(targetObject))
            {
                return missingSearchDictionary[targetObject];
            }

            var gameObjects = targetObject.Children();

            var hasMissingComponent = HasMissingComponent(targetObject);
            
            foreach (var gameObject in gameObjects)
            {
                hasMissingComponent |= SearchMissingComponents(gameObject);
            }

            // 子階層にMissingがある場合は自身もMissingを持つ状態にする.
            missingSearchDictionary.Add(targetObject, hasMissingComponent);

            return hasMissingComponent;
        }

        private bool HasMissingComponent(GameObject gameObject)
        {
            var components = UnityUtility.GetComponents<Component>(gameObject);

            return components.Any(x => x == null);
        }
    }
}
