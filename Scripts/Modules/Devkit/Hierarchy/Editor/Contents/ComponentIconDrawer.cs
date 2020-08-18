
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

namespace Modules.Devkit.Hierarchy
{
    public sealed class ComponentIconDrawer : ItemContentDrawer
    {
        //----- params -----

        private const float MissingIconSize = 16f;

        private const float ComponentIconSize = 13f;
        
        private static readonly Type[] DefaultDrawIconTypes = new Type[]
        {
            typeof(Camera),
            typeof(ParticleSystem),
            typeof(Animator),
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
            typeof(Selectable),
            typeof(Toggle),
            typeof(ToggleGroup),
            typeof(Shadow),
            typeof(Outline),
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

        public static class Prefs
        {
            public static bool enable
            {
                get { return ProjectPrefs.GetBool("ComponentIconDrawerPrefs-enable", true); }
                set { ProjectPrefs.SetBool("ComponentIconDrawerPrefs-enable", value); }
            }
        }

        //----- field -----

        private Dictionary<Type, GUIContent> iconGUIContentDictionary = null;

        //----- property -----

        public override int Priority { get { return 50; } }

        public override bool Enable { get { return Prefs.enable; } }

        //----- method -----

        public override void Initialize()
        {
            iconGUIContentDictionary = new Dictionary<Type, GUIContent>();

            SetDisplayIconTypes(DefaultDrawIconTypes);
        }

        public override Rect Draw(GameObject targetObject, Rect rect)
        {
            rect.center = Vector.SetX(rect.center, rect.center.x - 2f);

            var components = UnityUtility.GetComponents<Component>(targetObject).ToArray();

            var originIconSize = EditorGUIUtility.GetIconSize();

            if (components.Any())
            {
                using (new EditorGUIUtility.IconSizeScope(new Vector2(ComponentIconSize, ComponentIconSize)))
                {
                    var iconOffsetX = ComponentIconSize + 1.5f;

                    foreach (var component in components)
                    {
                        if (component == null) { continue; }

                        var type = component.GetType();

                        var item = iconGUIContentDictionary.FirstOrDefault(x => type == x.Key || type.IsSubclassOf(x.Key));

                        if (item.IsDefault()) { continue; }

                        var drawRect = rect;

                        drawRect.center = Vector.SetY(rect.center, rect.center.y + 1f);

                        EditorGUI.LabelField(drawRect, item.Value);

                        rect.center = Vector.SetX(rect.center, rect.center.x - iconOffsetX);
                    }
                }
            }

            EditorGUIUtility.SetIconSize(originIconSize);

            return rect;
        }

        public void SetDisplayIconTypes(Type[] displayTypes)
        {
            iconGUIContentDictionary.Clear();

            foreach (var type in displayTypes)
            {
                if (type == null){ continue; }

                var content = EditorGUIUtility.ObjectContent(null, type.UnderlyingSystemType);

                if (content == null){ continue; }

                if (content.image == null) { continue; }

                iconGUIContentDictionary.Add(type, new GUIContent(content.image));
            }
        }
    }
}
