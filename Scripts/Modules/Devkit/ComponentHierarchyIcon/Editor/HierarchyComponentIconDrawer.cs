
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

        private const float MissingIconSize = 16f;

        private const float ComponentIconSize = 13f;
        
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

        private Dictionary<Type, GUIContent> iconGUIContentDictionary = null;

        private GUIContent missingIconGUIContent = null;

        private bool initialized = false;

        //----- property -----

        //----- method -----
        
        protected void Initialize()
        {
            if (initialized) { return; }

            iconGUIContentDictionary = new Dictionary<Type, GUIContent>();

            missingIconGUIContent = EditorGUIUtility.IconContent("d_console.warnicon.sml");

            var targetTypes = GetDrawIconTypes();

            foreach (var type in targetTypes)
            {
                var texture = EditorGUIUtility.ObjectContent(null, type.UnderlyingSystemType).image;
                
                if (texture == null) { continue; }

                var guiContent = new GUIContent(texture);

                iconGUIContentDictionary.Add(type, guiContent);
            }
            
            EditorApplication.hierarchyWindowItemOnGUI += OnDrawHierarchy;

            initialized = true;
        }

        private void OnDrawHierarchy(int instanceID, Rect rect)
        {
            if (Application.isPlaying){ return; }

            if (!HierarchyComponentIcon.Prefs.enable) { return; }

            var go = EditorUtility.InstanceIDToObject(instanceID) as GameObject;

            if (go == null){ return; }

            var padding = new Vector2(18, 0);
            
            var iconDrawRect = new Rect(rect.xMax - padding.x, rect.yMin, rect.width, rect.height);

            var components = UnityUtility.GetComponents<Component>(go).ToArray();

            var originIconSize = EditorGUIUtility.GetIconSize();

            DrawMissingComponentIcon(components, ref iconDrawRect);

            DrawComponentIcon(components, ref iconDrawRect);

            EditorGUIUtility.SetIconSize(originIconSize);
        }

        private void DrawMissingComponentIcon(Component[] components, ref Rect rect)
        {
            var iconOffectX = MissingIconSize - 0.5f;

            var hasMissingComponent = components.Any(x => x == null);
            
            if (hasMissingComponent)
            {
                using (new EditorGUIUtility.IconSizeScope(new Vector2(MissingIconSize, MissingIconSize)))
                {
                    EditorGUI.LabelField(rect, missingIconGUIContent);
                }

                rect.center = Vector.SetX(rect.center, rect.center.x - iconOffectX);
            }
        }

        private void DrawComponentIcon(Component[] components, ref Rect rect)
        {
            if (components.Any())
            {
                using (new EditorGUIUtility.IconSizeScope(new Vector2(ComponentIconSize, ComponentIconSize)))
                {
                    var iconOffectX = ComponentIconSize + 1.5f;

                    foreach (var component in components)
                    {
                        if (component == null) { continue; }

                        var type = component.GetType();

                        var item = iconGUIContentDictionary.FirstOrDefault(x => type == x.Key || type.IsSubclassOf(x.Key));

                        if (item.IsDefault()) { continue; }

                        EditorGUI.LabelField(rect, item.Value);

                        rect.center = Vector.SetX(rect.center, rect.center.x - iconOffectX);
                    }
                }
            }
        }

        protected virtual Type[] GetDrawIconTypes()
        {
            return DefaultDrawIconTypes;
        }
    }
}
