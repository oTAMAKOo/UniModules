
#if UNITY_IOS && UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using UnityEditor.iOS.Xcode;
using System;
using System.IO;
using Extensions;

namespace Modules.Devkit.Build
{
    /// <summary>
    /// PostProcessBuildなどの中で呼び出しを行いPBXProjectを編集する.
    /// </summary>
    public abstract class ProcessForXCode
    {
        //----- params -----
        
        protected const string EntitlementFileName = "Entitlements.entitlements";

        //----- field -----

        //----- property -----

        protected PBXProject PbxProj { get; private set; }

        protected ProjectCapabilityManager CapabilityManager { get; private set; }
    
        protected string TargetGuid { get; private set; }

        //----- method -----

        protected void UpdatePbxProj(BuildTarget buildTarget, string path)
        {
            if (buildTarget != BuildTarget.iOS) { return; }

            try 
            {
                PbxProj = new PBXProject();

                var projectPath = PBXProject.GetPBXProjectPath(path);
                
                PbxProj.ReadFromFile(projectPath);

                #if UNITY_2020_2_OR_NEWER

                TargetGuid = PbxProj.GetUnityMainTargetGuid();
            
                #else
            
                TargetGuid = PbxProj.TargetGuidByName("Unity-iPhone");
            
                #endif

				EditPbxProj();

                CapabilityManager = new ProjectCapabilityManager(projectPath, EntitlementFileName, null, TargetGuid);

				EditCapability();

                CapabilityManager.WriteToFile();

                PbxProj.WriteToFile(projectPath);

				PbxProj = null;
                CapabilityManager = null;
                TargetGuid = null;
            } 
            catch (Exception e) 
            {
                Debug.LogError($"UpdatePbxProj error {e}");
            }
        }

        protected void SetBuildProperty(string name, string value)
        {
            PbxProj.SetBuildProperty(TargetGuid, name, value);
        }

        protected void AddFrameworkToProject(string name, bool weak = false)
        {
            PbxProj.AddFrameworkToProject(TargetGuid, name, weak);
        }

        protected void SetPlistString(string plistPath, string key, string value)
        {
            var plist = new PlistDocument();

            plist.ReadFromFile(plistPath);

            plist.root.SetString(key, value);

            plist.WriteToFile(plistPath);
        }


        protected void SetPlistBoolean(string plistPath, string key, bool value)
        {
            var plist = new PlistDocument();

            plist.ReadFromFile(plistPath);

            plist.root.SetBoolean(key, value);

            plist.WriteToFile(plistPath);
        }

        protected void SetPlistArray(string plistPath, string key, string value)
        {
            var plist = new PlistDocument();

            plist.ReadFromFile(plistPath);

            var array = plist.root.CreateArray(key);

            array.AddString(value);

            plist.WriteToFile(plistPath);
        }

        protected abstract void EditPbxProj();

		protected abstract void EditCapability();
    }
}

#endif