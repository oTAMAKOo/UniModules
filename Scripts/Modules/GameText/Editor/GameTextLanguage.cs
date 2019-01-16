
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Modules.Devkit.Prefs;

namespace Modules.GameText.Editor
{
    public static class GameTextLanguage
    {
        public static class Prefs
        {
            public static int selection
            {
                get { return ProjectPrefs.GetInt("GameTextLanguagePrefs-selection", -1); }
                set { ProjectPrefs.SetInt("GameTextLanguagePrefs-selection", value); }
            }
        }

        public static GameTextGenerateInfo[] GameTextInfos { get; set; }
    }
}
