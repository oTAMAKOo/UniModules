
using UnityEngine;
using UnityEditor;
using Unity.Linq;
using System;
using System.Collections.Generic;
using Extensions;
using Extensions.Devkit;
using Modules.Devkit.Prefs;

namespace Modules.Devkit.Hierarchy
{
    public sealed class MissingComponentDrawer : ItemContentDrawer
    {
        //----- params -----

        private static readonly Vector2 MissingIconSize = new Vector2(16f, 14f);

        private const int UpdateInterval = 3000;

        public static class Prefs
        {
            public static bool enable
            {
                get { return ProjectPrefs.GetBool(typeof(Prefs).FullName + "-enable", true); }
                set { ProjectPrefs.SetBool(typeof(Prefs).FullName + "-enable", value); }
            }
        }

        //----- field -----

        private GUIContent missingIconGUIContent = null;

        private Dictionary<GameObject, bool> missingSearchDictionary = null;

        private int frameCount = 0;

        private Func<GameObject, bool> missingDetectionCallBack = null;

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
            SearchMissing(targetObject);

            var hasMissing = missingSearchDictionary.GetValueOrDefault(targetObject);
            
            if (hasMissing)
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

        public void SetMissingDetectionCallBack(Func<GameObject, bool> callback)
        {
            missingDetectionCallBack = callback;
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

        private bool SearchMissing(GameObject targetObject)
        {
            // 既に検索済みなら検索しない.
            if (missingSearchDictionary.ContainsKey(targetObject))
            {
                return missingSearchDictionary[targetObject];
            }

            var hasMissing = UnityEditorUtility.HasMissingReference(targetObject);

            if (hasMissing)
            {
                if (missingDetectionCallBack != null)
                {
                    var changed = missingDetectionCallBack.Invoke(targetObject);

                    if (changed)
                    {
                        hasMissing = UnityEditorUtility.HasMissingReference(targetObject);
                    }
                }
            }

            var gameObjects = targetObject.Children();

            foreach (var gameObject in gameObjects)
            {
                hasMissing |= SearchMissing(gameObject);
            }

            // 子階層にMissingがある場合は自身もMissingを持つ状態にする.
            missingSearchDictionary.Add(targetObject, hasMissing);

            return hasMissing;
        }
    }
}
