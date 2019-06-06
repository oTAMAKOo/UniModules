
using UnityEngine;
using UnityEditor;
using System.IO;
using Extensions;
using UniRx;

// Modules.
using Modules.Dicing;
using Modules.MessagePack;
using Modules.Master;
using Modules.ExternalResource;
using Modules.ExternalResource.Editor;
using Modules.Devkit.AssetBundles;
using Modules.Devkit.AssetDependencies;
using Modules.Devkit.CompileNotice;
using Modules.Devkit.Generators;
using Modules.Devkit.Pinning;
using Modules.Devkit.EditorStyleViewer;
using Modules.Devkit.Build;
using Modules.Devkit.EventHook;
using Modules.Devkit.CleanDirectory;
using Modules.Devkit.CleanComponent;
using Modules.Devkit.ShaderVariant;
using Modules.Devkit.Project;
using Modules.Devkit.SceneImporter;
using Modules.Devkit.SceneLaunch;

#if ENABLE_CRIWARE

using Modules.CriWare.Editor;
using Modules.SoundManagement.Editor;

#endif

namespace Modules
{
    public class EditorMenu
    {
        private const int SeparatorValue = 11;

        public const string MenuRoot = "Extension/";

        // ※ priorityは11以上差分があると区切り線が入る.

        //===============================================================
        //  Generators.
        //===============================================================

        #region Generators

        public const string GeneratorsMenu = MenuRoot + "Generators/";
        public const string GeneratorsScripts = GeneratorsMenu + "Scripts/";

        [MenuItem(GeneratorsScripts + "All Scripts", priority = 0)]
        public static void GenerateAll()
        {
            var editorConfig = ProjectFolders.Instance;
            var sceneImporterConfig = SceneImporterConfig.Instance;

            // SceneNames.
            ScenesScriptGenerator.Generate(sceneImporterConfig.ManagedFolders, editorConfig.ScriptPath);

            // Tags.
            TagsScriptGenerator.Generate(editorConfig.ScriptPath);

            // Layers.
            LayersScriptGenerator.Generate(editorConfig.ScriptPath);

            // SortingLayers.
            SortingLayersScriptGenerator.Generate(editorConfig.ScriptPath);
        }

        [MenuItem(GeneratorsScripts + "  - Scenes.cs", priority = 1)]
        public static void GenerateSceneNames()
        {
            var editorConfig = ProjectFolders.Instance;
            var sceneImporterConfig = SceneImporterConfig.Instance;

            ScenesScriptGenerator.Generate(sceneImporterConfig.ManagedFolders, editorConfig.ScriptPath);
        }

        [MenuItem(GeneratorsScripts + "  - Tags.cs", priority = 2)]
        public static void GenerateTags()
        {
            var editorConfig = ProjectFolders.Instance;

            TagsScriptGenerator.Generate(editorConfig.ScriptPath);
        }

        [MenuItem(GeneratorsScripts + "  - Layers.cs", priority = 3)]
        public static void GenerateLayers()
        {
            var editorConfig = ProjectFolders.Instance;

            LayersScriptGenerator.Generate(editorConfig.ScriptPath);
        }

        [MenuItem(GeneratorsScripts + "  - SortingLayers.cs", priority = 4)]
        public static void GenerateSortingLayers()
        {
            var editorConfig = ProjectFolders.Instance;

            SortingLayersScriptGenerator.Generate(editorConfig.ScriptPath);
        }

        [MenuItem(itemName: GeneratorsMenu + "Generate ScenesInBuild", priority = 15)]
        public static void GenerateScenesInBuild()
        {
            ScenesInBuildGenerator.Generate();
        }

        [MenuItem(itemName: GeneratorsMenu + "Generate MessagePack", priority = 19)]
        public static void GenerateMessagePackCode()
        {
            MessagePackCodeGenerator.Compile();
        }

        [MenuItem(itemName: GeneratorsMenu + "Generate ScriptableObject", priority = 100)]
        public static void GenerateScriptableObject()
        {
            ScriptableObjectGenerator.Generate();
        }

        #endregion

        //===============================================================
        //  Master.
        //===============================================================

        #region Master

        public const string MasterMenu = MenuRoot + "Master/";

        //------ AssetDataBaseから読込 ------

        [MenuItem(itemName: MasterMenu + "Use CachedMasterFile", priority = 0)]
        public static void ToggleUseCachedMasterFile()
        {
            MasterManager.Prefs.checkVersion = !MasterManager.Prefs.checkVersion;
        }

        [MenuItem(itemName: MasterMenu + "Use CachedMasterFile", isValidateFunction: true)]
        public static bool ToggleUseCachedMasterFileValidate()
        {
            Menu.SetChecked(MasterMenu + "Use CachedMasterFile", !MasterManager.Prefs.checkVersion);
            return true;
        }

        [MenuItem(itemName: MasterMenu + "Open MasterFile Directory", priority = 12)]
        public static void OpenMasterFileDirectory()
        {
            var masterDownloadDirectory = MasterManager.Instance.InstallDirectory;

            if (!Directory.Exists(masterDownloadDirectory))
            {
                Directory.CreateDirectory(masterDownloadDirectory);
            }

            System.Diagnostics.Process.Start(masterDownloadDirectory);
        }

        #endregion

        //===============================================================
        //  Resource.
        //===============================================================

        #region Resource

        public const string ResourcesMenu = MenuRoot + "ExternalResources/";

        //------ AssetDataBaseから読込 ------

        [MenuItem(itemName: ResourcesMenu + "Simulate Mode", priority = 0)]
        public static void ToggleSimulateExternalResources()
        {
            ExternalResources.Prefs.isSimulate = !ExternalResources.Prefs.isSimulate;
        }

        [MenuItem(itemName: ResourcesMenu + "Simulate Mode", isValidateFunction: true)]
        public static bool ToggleSimulateExternalResourcesValidate()
        {
            Menu.SetChecked(ResourcesMenu + "Simulate Mode", ExternalResources.Prefs.isSimulate);
            return true;
        }

        //------ 外部アセット群作成 ------

        [MenuItem(itemName: ResourcesMenu + "Build", priority = 1)]
        public static void ExternalResourceBuild()
        {
            ExternalResourceBuildWindow.Open();
        }

        //------ 外部アセット管理ウィンドウ ------

        [MenuItem(itemName: ResourcesMenu + "Open AssetManageWindow", priority = 12)]
        public static void OpenAssetManageWindow()
        {
            var projectFolders = ProjectFolders.Instance;

            AssetManageWindow.Open(projectFolders.ExternalResourcesPath);
        }

        [MenuItem(itemName: ResourcesMenu + "Open AssetNavigationWindow", priority = 13)]
        public static void OpenAssetNavigationWindow()
        {
            var projectFolders = ProjectFolders.Instance;

            AssetNavigationWindow.Open(projectFolders.ExternalResourcesPath);
        }

        #endregion

        //===============================================================
        //  CriWare.
        //===============================================================

        #region CriWare

        public const string CriWareMenu = MenuRoot + "CriWare/";

        [MenuItem(itemName: CriWareMenu + "Open CriAssetUpdateWindow", priority = 0)]
        public static void OpenCriAssetUpdateWindow()
        {
            CriAssetUpdateWindow.Open();
        }

        [MenuItem(itemName: CriWareMenu + "Open CueNavigationWindow", priority = 1)]
        public static void OpenCueNavigationWindow()
        {
            CueNavigationWindow.Open();
        }

        [MenuItem(itemName: CriWareMenu + "UpdateCriAssets", priority = 20)]
        public static void UpdateCriAssets()
        {
            CriAssetUpdater.Execute();
        }

        #endregion

        //===============================================================
        //  Tools.
        //===============================================================

        #region Tools

        public const string ToolsMenu = MenuRoot + "Tools/";

        [MenuItem(itemName: ToolsMenu + "Dicing Packer/Open DicingPacker", priority = 1)]
        public static void OpenDicingPacker()
        {
            DicingPacker.Open();
        }

        [MenuItem(itemName: ToolsMenu + "AssetBundle/Dependency", priority = 2)]
        public static void OpenAssetBundleDependency()
        {
            FindDependencyAssetsWindow.Open();
        }

        [MenuItem(itemName: ToolsMenu + "Cleaner/AutoClean Text On", priority = 0)]
        public static void ToggleTextCleanerAutoMode()
        {
            TextComponentCleaner.Prefs.autoClean = !TextComponentCleaner.Prefs.autoClean;
        }

        [MenuItem(itemName: ToolsMenu + "Cleaner/AutoClean Text On", validate = true)]
        public static bool ToggleTextCleanerAutoModeValidate()
        {
            UnityEditor.Menu.SetChecked(ToolsMenu + "Cleaner/AutoClean Text On", TextComponentCleaner.Prefs.autoClean);
            return true;
        }

        [MenuItem(itemName: ToolsMenu + "Cleaner/AutoClean ParticleSystem On", priority = 0)]
        public static void ToggleParticleSystemCleanerAutoMode()
        {
            TextComponentCleaner.Prefs.autoClean = !ParticleComponentCleaner.Prefs.autoClean;
        }

        [MenuItem(itemName: ToolsMenu + "Cleaner/AutoClean ParticleSystem On", validate = true)]
        public static bool ToggleParticleSystemCleanerAutoModeValidate()
        {
            UnityEditor.Menu.SetChecked(ToolsMenu + "Cleaner/AutoClean ParticleSystem On", ParticleComponentCleaner.Prefs.autoClean);
            return true;
        }

        [MenuItem(itemName: ToolsMenu + "Cleaner/Execute", priority = 1)]
        public static void ExecComponentCleaner()
        {
            ComponentCleaner.Execute();
        }

        #endregion

        //===============================================================
        //  Settings.
        //===============================================================

        #region Settings

        public const string SettingsMenu = MenuRoot + "Settings/";
        
        //------ コンパイル時のSceneView表示 ------

        [MenuItem(itemName: SettingsMenu + "Show CompilingView", priority = 2)]
        public static void ToggleCompileNotificationMode()
        {
            CompileNotificationView.SetEnable(!CompileNotificationViewPrefs.enable);
        }

        [MenuItem(itemName: SettingsMenu + "Show CompilingView", validate = true)]
        public static bool ToggleCompileNotificationModeValidate()
        {
            UnityEditor.Menu.SetChecked(SettingsMenu + "Show CompilingView", CompileNotificationViewPrefs.enable);
            return true;
        }

        //------ コンポーネント調整時のログ表示 ------

        [MenuItem(itemName: SettingsMenu + "ComponentTuner Log", priority = 3)]
        public static void ToggleComponentTunerLog()
        {
            ComponentTuning.ToggleTuningLog();
        }

        [MenuItem(itemName: SettingsMenu + "ComponentTuner Log", validate = true)]
        public static bool ToggleComponentTunerLogValidate()
        {
            UnityEditor.Menu.SetChecked(SettingsMenu + "ComponentTuner Log", ComponentTuning.LogEnable);
            return true;
        }

        //------ コンポーネント自動追加無効化 ------

        [MenuItem(itemName: SettingsMenu + "AdditionalComponent/Disable", priority = 4)]
        public static void AdditionalComponentDisable()
        {
            AdditionalComponent.Prefs.enable = !AdditionalComponent.Prefs.enable;
        }

        [MenuItem(itemName: SettingsMenu + "AdditionalComponent/Disable", validate = true)]
        public static bool AdditionalComponentDisableValidate()
        {
            UnityEditor.Menu.SetChecked(SettingsMenu + "AdditionalComponent/Disable", !AdditionalComponent.Prefs.enable);
            return true;
        }

        //------ コンポーネント自動追加時のログ出力 ------

        [MenuItem(itemName: SettingsMenu + "AdditionalComponent/Log", priority = 4)]
        public static void AdditionalComponentLog()
        {
            AdditionalComponent.Prefs.log = !AdditionalComponent.Prefs.log;
        }

        [MenuItem(itemName: SettingsMenu + "AdditionalComponent/Log", validate = true)]
        public static bool AdditionalComponentLogValidate()
        {
            UnityEditor.Menu.SetChecked(SettingsMenu + "AdditionalComponent/Log", AdditionalComponent.Prefs.log);
            return true;
        }

        #endregion


        //===============================================================
        //  Window.
        //===============================================================

        #region Windows

        public const string WindowsMenu = MenuRoot + "Window/";

        [MenuItem(itemName: WindowsMenu + "Open SceneLaunchWindow", priority = 0)]
        public static void OpenSceneLaunchWindow()
        {
            SceneLaunchWindow.Open();
        }

        [MenuItem(itemName: WindowsMenu + "Open ProjectPinWindow", priority = 1)]
        public static void OpenProjectPinWindow()
        {
            ProjectPinningWindow.Open();
        }

        [MenuItem(itemName: WindowsMenu + "Open HierarchyPinWindow", priority = 2)]
        public static void OpenHierarchyPinWindow()
        {
            HierarchyPinningWindow.Open();
        }

        [MenuItem(itemName: WindowsMenu + "Open AssetDependenciesWindow", priority = 3)]
        public static void OpenAssetDependenciesWindow()
        {
            AssetDependenciesWindow.Open();
        }

        [MenuItem(itemName: WindowsMenu + "Open StyleViewerWindow", priority = 4)]
        public static void OpenStyleViewerWindow()
        {
            EditorStyleViewerWindow.Open();
        }

        [MenuItem(itemName: WindowsMenu + "Open CleanDirectoryWindow", priority = 5)]
        public static void OpenCleanDirectoryWindow()
        {
            CleanDirectoryWindow.Open();
        }

        [MenuItem(itemName: WindowsMenu + "Open ShaderVariantWindow", priority = 6)]
        public static void OpenShaderVariantUpdateWindow()
        {
            ShaderVariantUpdateWindow.Open();
        }

        #endregion

        //===============================================================
        //  Build.
        //===============================================================

        #region Build

        public const string BuildMenu = MenuRoot + "Build/";

        [MenuItem(itemName: BuildMenu + "Open BuildInAssetsWindow", priority = 50)]
        public static void OpenBuildInAssetsWindow()
        {
            BuildInAssetsWindow.Open();
        }

        #endregion

        //===============================================================
        //  Prerelease.
        //===============================================================

        #region Prerelease

        public const string PrereleaseMenu = MenuRoot + "Prerelease/";

        #endregion
    }
}
