﻿
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using UniRx;
using Extensions;
using Modules.Devkit.Prefs;

#if UNITY_IOS
    using UnityEditor.iOS.Xcode;
#endif

namespace Modules.Devkit.Build
{
    #if UNITY_IOS

    public abstract class ProcessForXCode
    {
        //----- params -----

        public static class Prefs
        {
            public static bool enable
            {
                get { return ProjectPrefs.GetBool("ProcessForXCode-enable", false); }
                set { ProjectPrefs.SetBool("ProcessForXCode-enable", value); }
            }
        }

        //----- field -----

        private PBXProject pbxproj = null;
        private string targetGuid = string.Empty;

        //----- property -----

        //----- method -----

        protected void UpdatePbxproj(BuildTarget buildTarget, string path)
        {
            if (buildTarget != BuildTarget.iOS) { return; }

            pbxproj = new PBXProject();

            var pbxprojPath = path + "/Unity-iPhone.xcodeproj/project.pbxproj";
            
            pbxproj.ReadFromString(File.ReadAllText(pbxprojPath));

            targetGuid = pbxproj.TargetGuidByName("Unity-iPhone");

            PbxprojEdit();

            File.WriteAllText(pbxprojPath, pbxproj.WriteToString());

            pbxproj = null;
            targetGuid = null;
        }

        protected void SetBuildProperty(string name, string value)
        {
            pbxproj.SetBuildProperty(targetGuid, name, value);
        }

        public abstract void PbxprojEdit();
    }

    #endif
}