﻿
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using Unity.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using Extensions.Devkit;
using Modules.Devkit.Prefs;
using UniRx;

namespace Modules.Devkit.EventHook
{
    public abstract class AdditionalComponent
    {
        public static class Prefs
        {
            public static bool enable
            {
                get { return ProjectPrefs.GetBool("AdditionalComponentPrefs-enable", true); }
                set { ProjectPrefs.SetBool("AdditionalComponentPrefs-enable", value); }
            }
        }

        public class RequireComponentSettings
        {
            public Type Type { get; set; }
            public ComponentSettings ComponentSettings { get; set; }

            public RequireComponentSettings(Type type, ComponentSettings componentSettings)
            {
                Type = type;
                ComponentSettings = componentSettings;
            }
        }

        public class ComponentSettings
        {
            public Type Component { get; set; }
            public bool RequireOrderChange { get; set; }

            public ComponentSettings(Type component, bool requireOrderChange)
            {
                Component = component;
                RequireOrderChange = requireOrderChange;
            }
        }
    }
}
