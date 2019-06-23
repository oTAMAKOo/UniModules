
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using Extensions;
using Modules.Devkit.Prefs;

#if ENABLE_SOFT_MASK

using SoftMasking;

#endif

namespace Modules.Devkit.HierarchyComponentIcon
{
    public static class HierarchyComponentIcon
    {
        public static class Prefs
        {
            public static bool enable
            {
                get { return ProjectPrefs.GetBool("HierarchyComponentIconPrefs-enable", true); }
                set { ProjectPrefs.SetBool("HierarchyComponentIconPrefs-enable", value); }
            }
        }
    }

    public abstract class HierarchyComponentIconDrawer<TInstance> :
        Singleton<TInstance> where TInstance : HierarchyComponentIconDrawer<TInstance>
    {
        //----- params -----

        private const float IconSize = 13f;

        protected static readonly Type[] DefaultDrawIconTypes = new Type[]
        {
            typeof(Camera),
            typeof(ParticleSystem),
            typeof(EventSystem),

            typeof(Canvas),
            typeof(Text),
            typeof(Image),
            typeof(RawImage),
            typeof(Slider),
            typeof(ScrollRect),
            typeof(Scrollbar),
            typeof(Button),
            typeof(Dropdown),
            typeof(InputField),
            typeof(Toggle),
            typeof(Mask),
            typeof(RectMask2D),
            typeof(LayoutElement),
            typeof(HorizontalLayoutGroup),
            typeof(VerticalLayoutGroup),
            typeof(GridLayoutGroup),

            #if ENABLE_SOFT_MASK

            typeof(SoftMask),

            #endif
        };

        //----- field -----

        private Dictionary<Type, Texture> iconTextureCache = null;

        private bool initialized = false;

        //----- property -----

        //----- method -----
        
        protected void Initialize()
        {
            if (initialized) { return; }

            iconTextureCache = new Dictionary<Type, Texture>();

            var targetTypes = GetDrawIconTypes();

            foreach (var type in targetTypes)
            {
                var texture = EditorGUIUtility.ObjectContent(null, type.UnderlyingSystemType).image;

                iconTextureCache.Add(type, texture);
            }

            EditorApplication.hierarchyWindowItemOnGUI += OnDrawHierarchy;

            initialized = true;
        }

        private void OnDrawHierarchy(int instandeID, Rect rect)
        {
            if (Application.isPlaying){ return; }

            if (!HierarchyComponentIcon.Prefs.enable) { return; }

            var go = EditorUtility.InstanceIDToObject(instandeID) as GameObject;

            if (go == null){ return; }

            var components = UnityUtility.GetComponents<Component>(go)
                .Where(x => x != null)
                .ToArray();

            if (components.Any())
            {
                var originIconSize = EditorGUIUtility.GetIconSize();

                EditorGUIUtility.SetIconSize(new Vector2(IconSize, IconSize));

                var padding = new Vector2(5, 0);
                var iconOffectX = IconSize + 1.5f;

                var iconDrawRect = new Rect(rect.xMax - (IconSize + padding.x), rect.yMin, rect.width, rect.height);

                foreach (var component in components)
                {
                    var type = component.GetType();

                    var item = iconTextureCache.FirstOrDefault(x => type == x.Key || type.IsSubclassOf(x.Key));

                    if (item.Equals(default(KeyValuePair<Type, Texture>))) { continue; }

                    DrawComponentIcon(iconDrawRect, item.Value);

                    iconDrawRect.center = Vector.SetX(iconDrawRect.center, iconDrawRect.center.x - iconOffectX);
                }

                EditorGUIUtility.SetIconSize(originIconSize);
            }
        }

        private void DrawComponentIcon(Rect rect, Texture texture)
        {
            var iconGUIContent = new GUIContent(texture);

            EditorGUI.LabelField(rect, iconGUIContent);
        }

        protected virtual Type[] GetDrawIconTypes()
        {
            return DefaultDrawIconTypes;
        }
    }
}
