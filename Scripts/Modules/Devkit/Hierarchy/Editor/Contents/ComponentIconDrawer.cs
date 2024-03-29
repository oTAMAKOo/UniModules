﻿
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using TMPro;
using Extensions;
using Modules.Devkit.Prefs;

namespace Modules.Devkit.Hierarchy
{
    public sealed class ComponentIconDrawer : ItemContentDrawer
    {
        //----- params -----

        private const float ComponentIconSize = 13f;

        private const int UpdateInterval = 1000;

        public static readonly Type[] DefaultDrawIconTypes = new Type[]
        {
            typeof(Camera),
            typeof(ParticleSystem),
            typeof(Animator),
            typeof(EventSystem),

            //----- uGUI -----

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
        };

        private const string TextMeshProPackageAssetPath = "Packages/com.unity.textmeshpro/Editor Resources/Gizmos/";

        public static readonly Dictionary<Type, string> TextMeshProIconInfoTable = new Dictionary<Type, string>
        {
            { typeof(TMP_Dropdown), "TMP - Dropdown Icon.psd" },
            { typeof(TMP_InputField), "TMP - Input Field Icon.psd" },
            { typeof(TextMeshProUGUI), "TMP - Text Component Icon.psd" },
        };

        public static class Prefs
        {
            public static bool enable
            {
                get { return ProjectPrefs.GetBool(typeof(Prefs).FullName + "-enable", true); }
                set { ProjectPrefs.SetBool(typeof(Prefs).FullName + "-enable", value); }
            }
        }

        //----- field -----

        private Dictionary<Type, GUIContent> iconGUIContentDictionary = null;

        private Dictionary<GameObject, Component[]> componentCacheDictionary = null;

        private Dictionary<Type, GUIContent> componentIconCahceDictionary = null;

        private int frameCount = 0;

        //----- property -----

        public override int Priority { get { return 50; } }

        public override bool Enable { get { return Prefs.enable; } }

        //----- method -----

        public override void Initialize()
        {
            iconGUIContentDictionary = new Dictionary<Type, GUIContent>();
            componentCacheDictionary = new Dictionary<GameObject, Component[]>();
            componentIconCahceDictionary = new Dictionary<Type, GUIContent>();

            SetDisplayIconTypes(DefaultDrawIconTypes);
            RegisterTextMeshProTypes();

            EditorApplication.update += OnEditorUpdate;
        }

        public override Rect Draw(GameObject targetObject, Rect rect)
        {
            rect.center = Vector.SetX(rect.center, rect.center.x - 2f);

            var components = GetTargetComponents(targetObject);

            var originIconSize = EditorGUIUtility.GetIconSize();

            if (components.Any())
            {
                using (new EditorGUIUtility.IconSizeScope(new Vector2(ComponentIconSize, ComponentIconSize)))
                {
                    var iconOffsetX = ComponentIconSize + 1.5f;

                    foreach (var component in components)
                    {
                        if (component == null) { continue; }

                        var icon = GetComponentIcon(component);

                        if (icon == null){ continue; }

                        var drawRect = rect;

                        drawRect.center = Vector.SetY(rect.center, rect.center.y + 1f);

                        EditorGUI.LabelField(drawRect, icon);

                        rect.center = Vector.SetX(rect.center, rect.center.x - iconOffsetX);
                    }
                }
            }

            EditorGUIUtility.SetIconSize(originIconSize);

            return rect;
        }

        public void SetDisplayIconTypes(Type[] displayTypes)
        {
            foreach (var type in displayTypes)
            {
                if (type == null) { continue; }

                var content = EditorGUIUtility.ObjectContent(null, type.UnderlyingSystemType);

                if (content == null) { continue; }

                if (content.image == null) { continue; }

                iconGUIContentDictionary.Add(type, new GUIContent(content.image));
            }
        }

        public void AddCustomDisplayType(Type type, string iconAssetPath)
        {
            var texture = AssetDatabase.LoadMainAssetAtPath(iconAssetPath) as Texture;

            AddCustomDisplayType(type, texture);
        }

        public void AddCustomDisplayType(Type type, Texture iconTextue)
        {
            if (iconTextue == null) { return; }

            AddCustomDisplayType(type, new GUIContent(iconTextue));
        }

        public void AddCustomDisplayType(Type type, GUIContent iconContent)
        {
            if (iconContent == null) { return; }

            iconGUIContentDictionary.Add(type, iconContent);
        }

        public void Clear()
        {
            if (iconGUIContentDictionary != null)
            {
                iconGUIContentDictionary.Clear();
            }
        }

        public void RegisterTextMeshProTypes()
        {
            foreach (var item in TextMeshProIconInfoTable)
            {
                var iconAssetPath = TextMeshProPackageAssetPath + item.Value;

                var texture = AssetDatabase.LoadAssetAtPath(iconAssetPath, typeof(Texture2D)) as Texture2D;

                AddCustomDisplayType(item.Key, texture);
            }
        }

        private Component[] GetTargetComponents(GameObject target)
        {
            Component[] components;
         
            if (componentCacheDictionary.ContainsKey(target))
            {
                components = componentCacheDictionary[target];
            }
            else
            {
                components = UnityUtility.GetComponents<Component>(target).ToArray();

                componentCacheDictionary.Add(target, components);
            }

            return components;
        }

        private GUIContent GetComponentIcon(Component component)
        {
            GUIContent icon = null;

            var type = component.GetType();

            if (componentIconCahceDictionary.ContainsKey(type))
            {
                icon = componentIconCahceDictionary[type];
            }
            else
            {
                var item = iconGUIContentDictionary.FirstOrDefault(x => type == x.Key || type.IsSubclassOf(x.Key));

                if (!item.IsDefault())
                {
                    icon = item.Value;
                }

                componentIconCahceDictionary.Add(type, icon);
            }

            return icon;
         }

        private void OnEditorUpdate()
        {
            frameCount++;

            if (UpdateInterval < frameCount)
            {
                componentCacheDictionary.Clear();
                componentIconCahceDictionary.Clear();

                frameCount = 0;
            }
        }
    }
}
