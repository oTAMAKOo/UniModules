
using UnityEngine;
using UnityEditor;
using UniRx;
using Extensions;

// Modules.
using Modules.PatternTexture;
using Modules.GameText.Editor;
using Modules.MessagePack;
using Modules.Master;
using Modules.ExternalResource;
using Modules.ExternalResource.Editor;
using Modules.BehaviorControl;
using Modules.Devkit.AssetBundles;
using Modules.Devkit.AssetDependencies;
using Modules.Devkit.AssemblyCompilation;
using Modules.Devkit.Generators;
using Modules.Devkit.Pinning;
using Modules.Devkit.Build;
using Modules.Devkit.EventHook;
using Modules.Devkit.CleanDirectory;
using Modules.Devkit.CleanComponent;
using Modules.Devkit.ShaderVariant;
using Modules.Devkit.Project;
using Modules.Devkit.SceneImporter;
using Modules.Devkit.SceneLaunch;
using Modules.Devkit.Hierarchy;
using Modules.Devkit.AssetTuning;
using Modules.Devkit.Console;
using Modules.Devkit.U2D;

#if ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_SOFDEC

using Modules.CriWare.Editor;

#endif

namespace Modules
{
    public class EditorMenu
    {
        protected const string MenuRoot = "Extension/";

        // ※ priorityは11以上差分があると区切り線が入る.
        protected const int SeparatorValue = 11;

        //===============================================================
        //  Generators.
        //===============================================================

        #region Generators

        protected const string GeneratorsMenu = MenuRoot + "Generators/";
        protected const string GeneratorsScripts = GeneratorsMenu + "Scripts/";

        [MenuItem(GeneratorsScripts + "All Scripts", priority = 0)]
        public static void GenerateAll()
        {
            var editorConfig = ProjectFolders.Instance;
            var sceneImporterConfig = SceneImporterConfig.Instance;

            // SceneNames.
            ScenesScriptGenerator.Generate(sceneImporterConfig.ManagedFolders, editorConfig.ConstantsScriptPath);

            // Tags.
            TagsScriptGenerator.Generate(editorConfig.ConstantsScriptPath);

            // Layers.
            LayersScriptGenerator.Generate(editorConfig.ConstantsScriptPath);

            // SortingLayers.
            SortingLayersScriptGenerator.Generate(editorConfig.ConstantsScriptPath);
        }

        [MenuItem(GeneratorsScripts + "  - Scenes.cs", priority = 1)]
        public static void GenerateSceneNames()
        {
            var editorConfig = ProjectFolders.Instance;
            var sceneImporterConfig = SceneImporterConfig.Instance;

            ScenesScriptGenerator.Generate(sceneImporterConfig.ManagedFolders, editorConfig.ConstantsScriptPath);
        }

        [MenuItem(GeneratorsScripts + "  - Tags.cs", priority = 2)]
        public static void GenerateTags()
        {
            var editorConfig = ProjectFolders.Instance;

            TagsScriptGenerator.Generate(editorConfig.ConstantsScriptPath);
        }

        [MenuItem(GeneratorsScripts + "  - Layers.cs", priority = 3)]
        public static void GenerateLayers()
        {
            var editorConfig = ProjectFolders.Instance;

            LayersScriptGenerator.Generate(editorConfig.ConstantsScriptPath);
        }

        [MenuItem(GeneratorsScripts + "  - SortingLayers.cs", priority = 4)]
        public static void GenerateSortingLayers()
        {
            var editorConfig = ProjectFolders.Instance;

            SortingLayersScriptGenerator.Generate(editorConfig.ConstantsScriptPath);
        }

        [MenuItem(itemName: GeneratorsMenu + "Generate ScenesInBuild", priority = 15)]
        public static void GenerateScenesInBuild()
        {
            ScenesInBuildGenerator.Generate();
        }

        [MenuItem(itemName: GeneratorsMenu + "Generate MessagePack", priority = 19)]
        public static void GenerateMessagePackCode()
        {
            MessagePackCodeGenerator.Generate().Subscribe();
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

        protected const string MasterMenu = MenuRoot + "Master/";

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

        #endregion

        //===============================================================
        //  GameText.
        //===============================================================

        #region GameText

        protected const string GameTextMenu = MenuRoot + "GameText/";

        [MenuItem(itemName: GameTextMenu + "Open BuiltIn Window", priority = 0)]
        public static void OpenBuiltInGameTextWindow()
        {
            BuiltInGameTextWindow.Open();
        }

        [MenuItem(itemName: GameTextMenu + "Open Extend Window", priority = 1)]
        public static void OpenExtendnGameTextWindow()
        {
            ExtendGameTextWindow.Open();
        }

        [MenuItem(itemName: GameTextMenu + "Open Extend Window", true)]
        private static bool ValidateOpenExtendnGameTextWindow()
        {
            return GameTextConfig.Instance.ExtendGameText.Enable;
        }

        #endregion

        //===============================================================
        //  Resource.
        //===============================================================

        #region Resource

        protected const string ResourcesMenu = MenuRoot + "ExternalResources/";

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
            BuildWindow.Open();
        }

        //------ 外部アセット管理ウィンドウ ------

        [MenuItem(itemName: ResourcesMenu + "Open AssetManageWindow", priority = 12)]
        public static void OpenAssetManageWindow()
        {
            var projectFolders = ProjectFolders.Instance;

            ManageWindow.Open(projectFolders.ExternalResourcesPath);
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

        #if ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_SOFDEC

        protected const string CriWareMenu = MenuRoot + "CriWare/";

        [MenuItem(itemName: CriWareMenu + "Open CriAssetUpdateWindow", priority = 0)]
        public static void OpenCriAssetUpdateWindow()
        {
            CriAssetUpdateWindow.Open();
        }

        [MenuItem(itemName: CriWareMenu + "UpdateCriAssets", priority = 20)]
        public static void UpdateCriAssets()
        {
            CriAssetUpdater.Execute();
        }

        #endif

        #endregion

        //===============================================================
        //  Tools.
        //===============================================================

        #region Tools

        protected const string ToolsMenu = MenuRoot + "Tools/";

        [MenuItem(itemName: ToolsMenu + "Cleaner/Clean CanvasRenderer On", priority = 0)]
        public static void ToggleCanvasRendererCleanerAutoMode()
        {
            CanvasRendererCleaner.Prefs.autoClean = !CanvasRendererCleaner.Prefs.autoClean;
        }

        [MenuItem(itemName: ToolsMenu + "Cleaner/Clean CanvasRenderer On", validate = true)]
        public static bool ToggleCanvasRendererCleanerAutoModeValidate()
        {
            UnityEditor.Menu.SetChecked(ToolsMenu + "Cleaner/Clean CanvasRenderer On", CanvasRendererCleaner.Prefs.autoClean);
            return true;
        }

        [MenuItem(itemName: ToolsMenu + "Cleaner/Clean Text On", priority = 1)]
        public static void ToggleTextCleanerAutoMode()
        {
            TextComponentCleaner.Prefs.autoClean = !TextComponentCleaner.Prefs.autoClean;
        }

        [MenuItem(itemName: ToolsMenu + "Cleaner/Clean Text On", validate = true)]
        public static bool ToggleTextCleanerAutoModeValidate()
        {
            UnityEditor.Menu.SetChecked(ToolsMenu + "Cleaner/Clean Text On", TextComponentCleaner.Prefs.autoClean);
            return true;
        }

        [MenuItem(itemName: ToolsMenu + "Cleaner/Clean ParticleSystem On", priority = 2)]
        public static void ToggleParticleSystemCleanerAutoMode()
        {
            TextComponentCleaner.Prefs.autoClean = !ParticleComponentCleaner.Prefs.autoClean;
        }

        [MenuItem(itemName: ToolsMenu + "Cleaner/Clean ParticleSystem On", validate = true)]
        public static bool ToggleParticleSystemCleanerAutoModeValidate()
        {
            UnityEditor.Menu.SetChecked(ToolsMenu + "Cleaner/Clean ParticleSystem On", ParticleComponentCleaner.Prefs.autoClean);
            return true;
        }

        [MenuItem(itemName: ToolsMenu + "Cleaner/Execute Clean Scene", priority = 20)]
        public static void ExecComponentCleaner()
        {
            ComponentCleaner.Execute();
        }

        [MenuItem(itemName: ToolsMenu + "Open PatternTexturePacker")]
        public static void OpenPatternTexturePacker()
        {
            PatternTexturePacker.Open();
        }

        [MenuItem(itemName: ToolsMenu + "Open AssetBundleDependencyChecker")]
        public static void OpenAssetBundleDependency()
        {
            FindDependencyAssetsWindow.Open();
        }

        [MenuItem(itemName: ToolsMenu + "Open TextureCompressChecker")]
        public static void OpenTextureCompressChecker()
        {
            CompressCheckWindow.Open();
        }

        #endregion

        //===============================================================
        //  Settings.
        //===============================================================

        #region Settings

        protected const string SettingsMenu = MenuRoot + "Settings/";
        
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

        //------ Hierarchy表示 ------

        protected const string SettingsHierarchyMenu = SettingsMenu + "Hierarchy/";

        [MenuItem(itemName: SettingsHierarchyMenu + "ComponentIcon", priority = 1)]
        public static void ToggleHierarchyComponentIcon()
        {
            ComponentIconDrawer.Prefs.enable = !ComponentIconDrawer.Prefs.enable;
        }

        [MenuItem(itemName: SettingsHierarchyMenu + "ComponentIcon", validate = true)]
        public static bool ToggleHierarchyComponentIconValidate()
        {
            UnityEditor.Menu.SetChecked(SettingsHierarchyMenu + "ComponentIcon", ComponentIconDrawer.Prefs.enable);
            return true;
        }

        [MenuItem(itemName: SettingsHierarchyMenu + "MissingComponent", priority = 2)]
        public static void ToggleHierarchyMissingComponent()
        {
            MissingComponentDrawer.Prefs.enable = !MissingComponentDrawer.Prefs.enable;
        }

        [MenuItem(itemName: SettingsHierarchyMenu + "MissingComponent", validate = true)]
        public static bool ToggleHierarchyMissingComponentValidate()
        {
            UnityEditor.Menu.SetChecked(SettingsHierarchyMenu + "MissingComponent", MissingComponentDrawer.Prefs.enable);
            return true;
        }

        [MenuItem(itemName: SettingsHierarchyMenu + "ActiveToggle", priority = 3)]
        public static void ToggleHierarchyActiveToggle()
        {
            ActiveToggleDrawer.Prefs.enable = !ActiveToggleDrawer.Prefs.enable;
        }

        [MenuItem(itemName: SettingsHierarchyMenu + "ActiveToggle", validate = true)]
        public static bool ToggleHierarchyActiveToggleValidate()
        {
            UnityEditor.Menu.SetChecked(SettingsHierarchyMenu + "ActiveToggle", ActiveToggleDrawer.Prefs.enable);
            return true;
        }

        //------ コンポーネント調整無効化 ------

        [MenuItem(itemName: SettingsMenu + "Auto Component Tuning/Disable", priority = 4)]
        public static void AutoComponentTuningDisable()
        {
            ComponentTuning.Prefs.Enable = !ComponentTuning.Prefs.Enable;
        }

        [MenuItem(itemName: SettingsMenu + "Auto Component Tuning/Disable", validate = true)]
        public static bool AutoComponentTuningDisableValidate()
        {
            UnityEditor.Menu.SetChecked(SettingsMenu + "Auto Component Tuning/Disable", !ComponentTuning.Prefs.Enable);
            return true;
        }

        //------ コンポーネント調整時のログ表示 ------

        [MenuItem(itemName: SettingsMenu + "Auto Component Tuning/Log", priority = 4)]
        public static void ToggleAutoComponentTuningLog()
        {
            ComponentTuning.Prefs.LogEnable = !ComponentTuning.Prefs.LogEnable;
        }

        [MenuItem(itemName: SettingsMenu + "Auto Component Tuning/Log", validate = true)]
        public static bool ToggleAutoComponentTuningValidate()
        {
            UnityEditor.Menu.SetChecked(SettingsMenu + "Auto Component Tuning/Log", ComponentTuning.Prefs.LogEnable);
            return true;
        }

        //------ コンポーネント自動追加無効化 ------

        [MenuItem(itemName: SettingsMenu + "Auto Add Component/Disable", priority = 5)]
        public static void AutoAddComponentDisable()
        {
            AdditionalComponent.Prefs.Enable = !AdditionalComponent.Prefs.Enable;
        }

        [MenuItem(itemName: SettingsMenu + "Auto Add Component/Disable", validate = true)]
        public static bool AutoAddComponentDisableValidate()
        {
            UnityEditor.Menu.SetChecked(SettingsMenu + "Auto Add Component/Disable", !AdditionalComponent.Prefs.Enable);
            return true;
        }

        //------ コンポーネント自動追加時のログ出力 ------

        [MenuItem(itemName: SettingsMenu + "Auto Add Component/Log", priority = 6)]
        public static void AdditionalComponentLog()
        {
            AdditionalComponent.Prefs.LogEnable = !AdditionalComponent.Prefs.LogEnable;
        }

        [MenuItem(itemName: SettingsMenu + "Auto Add Component/Log", validate = true)]
        public static bool AdditionalComponentLogValidate()
        {
            UnityEditor.Menu.SetChecked(SettingsMenu + "Auto Add Component/Log", AdditionalComponent.Prefs.LogEnable);
            return true;
        }

        #endregion

        //===============================================================
        //  Window.
        //===============================================================

        #region Windows

        protected const string WindowsMenu = MenuRoot + "Window/";

        [MenuItem(itemName: WindowsMenu + "Open SceneLaunchWindow", priority = 0)]
        public static void OpenSceneLaunchWindow()
        {
            SceneLaunchWindow.Open();
        }

        [MenuItem(itemName: WindowsMenu + "Open RaycastViewerWindow", priority = 1)]
        public static void OpenRaycastViewerWindow()
        {
            RaycastViewerWindow.Open();
        }

        [MenuItem(itemName: WindowsMenu + "Open ProjectPinWindow", priority = 2)]
        public static void OpenProjectPinWindow()
        {
            ProjectPinningWindow.Open();
        }

        [MenuItem(itemName: WindowsMenu + "Open HierarchyPinWindow", priority = 3)]
        public static void OpenHierarchyPinWindow()
        {
            HierarchyPinningWindow.Open();
        }

        [MenuItem(itemName: WindowsMenu + "Open AssetDependenciesWindow", priority = 4)]
        public static void OpenAssetDependenciesWindow()
        {
            AssetDependenciesWindow.Open();
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

        [MenuItem(itemName: WindowsMenu + "Open UnityConsoleConfigWindow", priority = 7)]
        public static void OpenUnityConsoleConfigWindow()
        {
            UnityConsoleConfigWindow.Open();
        }

        [MenuItem(itemName: WindowsMenu + "Open BehaviorControlMonitor", priority = 8)]
        public static void OpenBehaviorControlMonitor()
        {
            BehaviorControlMonitor.Open();
        }

        #endregion

        //===============================================================
        //  Build.
        //===============================================================

        #region Build

        protected const string BuildMenu = MenuRoot + "Build/";

        [MenuItem(itemName: BuildMenu + "Open BuiltInAssetsWindow", priority = 50)]
        public static void OpenBuiltInAssetsWindow()
        {
            BuiltInAssetsWindow.Open();
        }

        #endregion

        //===============================================================
        //  Directory.
        //===============================================================

        #region Directory

        protected const string DirectoryMenu = MenuRoot + "Directory/";

        [MenuItem(itemName: DirectoryMenu + "Open PersistentDataPath", priority = 0)]
        public static void OpenPersistentDataPath()
        {
            OpenDirectory(Application.persistentDataPath);
        }

        [MenuItem(itemName: DirectoryMenu + "Open TemporaryCachePath", priority = 1)]
        public static void OpenTemporaryCachePath()
        {
            OpenDirectory(Application.temporaryCachePath);
        }

        [MenuItem(itemName: DirectoryMenu + "Open StreamingAssetsPath", priority = 2)]
        private static void OpenStreamingAssetsPath()
        {
            OpenDirectory(Application.streamingAssetsPath);
        }

        [MenuItem(itemName: DirectoryMenu + "Open OpenConsoleLogPath", priority = 3)]
        private static void OpenConsoleLogPath()
        {
            OpenDirectory(Application.consoleLogPath);
        }

        private static void OpenDirectory(string path)
        {
            path = PathUtility.ConvertPathSeparator(path);

            EditorUtility.RevealInFinder(path);
        }

        #endregion

        //===============================================================
        //  Prerelease.
        //===============================================================

        #region Prerelease

        protected const string PrereleaseMenu = MenuRoot + "Prerelease/";

        #endregion
    }
}
