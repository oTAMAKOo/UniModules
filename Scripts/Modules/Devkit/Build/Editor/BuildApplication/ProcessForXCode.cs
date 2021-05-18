
#if UNITY_IOS

using UnityEditor;
using UnityEditor.iOS.Xcode;
using System.IO;
using Modules.Devkit.Prefs;

namespace Modules.Devkit.Build
{
    public abstract class ProcessForXCode
    {
        //----- params -----

        //----- field -----

        //----- property -----

        protected PBXProject PbxProj { get; private set; }
    
        protected string TargetGuid { get; private set; }

        //----- method -----

        protected void UpdatePbxProj(BuildTarget buildTarget, string path)
        {
            if (buildTarget != BuildTarget.iOS) { return; }

            PbxProj = new PBXProject();

            var pbxProjPath = path + "/Unity-iPhone.xcodeproj/project.pbxproj";

            PbxProj.ReadFromString(File.ReadAllText(pbxProjPath));

            TargetGuid = PbxProj.TargetGuidByName("Unity-iPhone");

            EditPbxProj();

            File.WriteAllText(pbxProjPath, PbxProj.WriteToString());

            PbxProj = null;
            TargetGuid = null;
        }

        protected void SetBuildProperty(string name, string value)
        {
            PbxProj.SetBuildProperty(TargetGuid, name, value);
        }

        protected void AddCapability(PBXCapabilityType capabilityType)
        {
            PbxProj.AddCapability(TargetGuid, capabilityType);
        }

        protected void AddFrameworkToProject(string name, bool weak = false)
        {
            PbxProj.AddFrameworkToProject(TargetGuid, name, weak);
        }

        protected void SetEntitlements(string entitlementSourcePath, string entitlementDestPath)
        {
            var src = entitlementSourcePath;
            var file_name = Path.GetFileName(src);
            var dst = entitlementDestPath + "/" + "Unity-iPhone" + "/" + file_name;

            FileUtil.CopyFileOrDirectory(src, dst);

            PbxProj.AddFile("Unity-iPhone" + "/" + file_name, file_name);
            PbxProj.AddBuildProperty(TargetGuid, "CODE_SIGN_ENTITLEMENTS", "Unity-iPhone" + "/" + file_name);
        }

        protected void SetPlist(string plistPath, string key, string value)
        {
            // Plistの設定のための初期化.
            var plist = new PlistDocument();

            plist.ReadFromFile(plistPath);

            var array = plist.root.CreateArray(key);

            array.AddString(value);

            // 設定を反映.
            plist.WriteToFile(plistPath);
        }

        protected abstract void EditPbxProj();
    }
}

#endif
