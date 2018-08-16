﻿﻿
using UnityEngine;
using UnityEditor;
using System.Collections;
using Modules.Devkit.Prefs;
using UniRx;

namespace Modules.Devkit
{
    [InitializeOnLoad]
    public class EditorIME
    {
        private const string PrefsKey = "Editor IMECompositionMode";

        //----- field -----

        private static IMECompositionMode mode = IMECompositionMode.On;

        //----- property -----

        public static IMECompositionMode Mode { get { return mode; } }

        //----- method -----

        static EditorIME()
        {
            if (ProjectPrefs.HasKey(PrefsKey))
            {
                mode = (IMECompositionMode)ProjectPrefs.GetInt(PrefsKey);
            }

            EditorApplication.update += Update;
        }

        public static void ToggleIMECompositionMode()
        {
            switch (mode)
            {
                case IMECompositionMode.Auto:
                case IMECompositionMode.Off:
                    mode = IMECompositionMode.On;
                    break;

                case IMECompositionMode.On:
                    mode = IMECompositionMode.Off;
                    break;
            }

            ProjectPrefs.SetInt(PrefsKey, (int)mode);
        }

        static void Update()
        {
            if (mode != Input.imeCompositionMode)
            {
                Input.imeCompositionMode = mode;

                ProjectPrefs.SetInt(PrefsKey, (int)mode);
            }
        }
    }
}