
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using UniRx;
using Extensions;

// Modules.
using Modules.PatternTexture;
using Modules.TextData.Editor;
using Modules.TextData.Components;
using Modules.MessagePack;
using Modules.Master;
using Modules.ExternalAssets;
using Modules.Net.WebRequest;
using Modules.BehaviorControl;
using Modules.InputControl;
using Modules.Devkit.AssetBundles;
using Modules.Devkit.AssetDependencies;
using Modules.Devkit.AssemblyCompilation;
using Modules.Devkit.AssetBundleViewer;
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
using Modules.Devkit.Console;
using Modules.Devkit.DefineSymbol;
using Modules.Devkit.ExternalAssets;
using Modules.Devkit.SerializeAssets;
using Modules.Devkit.TextureViewer;
using Modules.Devkit.UI;
using Modules.Devkit.U2D;
using Modules.Devkit.TextMeshPro;

#if ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_ADX_LE || ENABLE_CRIWARE_SOFDEC

using Modules.CriWare;
using Modules.CriWare.Editor;

#endif

#if ENABLE_XLUA

using Modules.Lua;

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
            var projectScriptFolders = ProjectScriptFolders.Instance;
            var sceneImporterConfig = SceneImporterConfig.Instance;

            var constantsScriptPath = projectScriptFolders.ConstantsScriptPath;

            // SceneNames.
            var managedFolderPaths = sceneImporterConfig.GetManagedFolderPaths();
            ScenesScriptGenerator.Generate(managedFolderPaths, constantsScriptPath);

            // Tags.
            TagsScriptGenerator.Generate(constantsScriptPath);

            // Layers.
            LayersScriptGenerator.Generate(constantsScriptPath);

            // SortingLayers.
            SortingLayersScriptGenerator.Generate(constantsScriptPath);
        }

        [MenuItem(GeneratorsScripts + "  - Scenes.cs", priority = 1)]
        public static void GenerateSceneNames()
        {
            var projectScriptFolders = ProjectScriptFolders.Instance;
            var sceneImporterConfig = SceneImporterConfig.Instance;

            var constantsScriptPath = projectScriptFolders.ConstantsScriptPath;
            var managedFolderPaths = sceneImporterConfig.GetManagedFolderPaths();

            ScenesScriptGenerator.Generate(managedFolderPaths, constantsScriptPath);
        }

        [MenuItem(GeneratorsScripts + "  - Tags.cs", priority = 2)]
        public static void GenerateTags()
        {
            var projectScriptFolders = ProjectScriptFolders.Instance;
            var constantsScriptPath = projectScriptFolders.ConstantsScriptPath;

            TagsScriptGenerator.Generate(constantsScriptPath);
        }

        [MenuItem(GeneratorsScripts + "  - Layers.cs", priority = 3)]
        public static void GenerateLayers()
        {
            var projectScriptFolders = ProjectScriptFolders.Instance;
            var constantsScriptPath = projectScriptFolders.ConstantsScriptPath;

            LayersScriptGenerator.Generate(constantsScriptPath);
        }

        [MenuItem(GeneratorsScripts + "  - SortingLayers.cs", priority = 4)]
        public static void GenerateSortingLayers()
        {
            var projectScriptFolders = ProjectScriptFolders.Instance;
            var constantsScriptPath = projectScriptFolders.ConstantsScriptPath;

            SortingLayersScriptGenerator.Generate(constantsScriptPath);
        }

        [MenuItem(itemName: GeneratorsMenu + "Generate ScenesInBuild", priority = 15)]
        public static void GenerateScenesInBuild()
        {
            ScenesInBuildGenerator.Generate();
        }

        #if !MESSAGEPACK_ANALYZER_CODE

        [MenuItem(itemName: GeneratorsMenu + "Generate MessagePack", priority = 19)]
        public static void GenerateMessagePackCode()
        {
            MessagePackCodeGenerator.Generate();
        }
      
        #endif

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

        //------ バージョンチェックを行わずローカルのマスターを読込 ------

        [MenuItem(itemName: MasterMenu + "Use CachedMasterFile", priority = 25)]
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
        //  TextData.
        //===============================================================

        #region TextData

        protected const string TextDataMenu = MenuRoot + "TextData/";

        [MenuItem(itemName: TextDataMenu + "Open Generate Window", priority = 0)]
        public static void OpenTextDataGenerateWindow()
        {
            GenerateWindow.Open();
        }

        [MenuItem(itemName: TextDataMenu + "Open Selector Window", priority = 1)]
        public static void OpenTextDataSelectorWindow()
        {
            SelectorWindow.Open();
        }

        [MenuItem(itemName: TextDataMenu + "Open Validation Window", priority = 2)]
        public static void OpenTextDataValidationWindow()
        {
            TextDataValidationWindow.Open();
        }

        //------ Excel保存時に自動更新 ------

        [MenuItem(itemName: TextDataMenu + "Output Converter ProcessCommand", priority = 2)]
        public static void ToggleOutputConverterProcessCommand()
        {
            TextDataExcel.Prefs.outputCommand = !TextDataExcel.Prefs.outputCommand;
        }

        [MenuItem(itemName: TextDataMenu + "Output Converter ProcessCommand", isValidateFunction: true)]
        public static bool ToggleOutputConverterProcessCommandValidate()
        {
            Menu.SetChecked(TextDataMenu + "Output Converter ProcessCommand", TextDataExcel.Prefs.outputCommand);
            return true;
        }

        //------ Excel保存時に自動更新 ------

        [MenuItem(itemName: TextDataMenu + "Updated when save Excel", priority = 20)]
        public static void ToggleUpdateOnSaveTextDataExcel()
        {
            TextDataAssetUpdater.Prefs.autoUpdate = !TextDataAssetUpdater.Prefs.autoUpdate;
        }

        [MenuItem(itemName: TextDataMenu + "Updated when save Excel", isValidateFunction: true)]
        public static bool ToggleUpdateOnSaveTextDataExcelValidate()
        {
            Menu.SetChecked(TextDataMenu + "Updated when save Excel", TextDataAssetUpdater.Prefs.autoUpdate);
            return true;
        }

        #endregion

        //===============================================================
        //  LuaText.
        //===============================================================

        #if ENABLE_XLUA

        #region LuaText

        protected const string LuaTextMenu = MenuRoot + "LuaText/";

        [MenuItem(itemName: LuaTextMenu + "Open Generate Window", priority = 0)]
        public static void OpenLuaTextWindow()
        {
            Lua.Text.GenerateWindow.Open();
        }

        //------ Excel保存時に自動更新 ------

        [MenuItem(itemName: LuaTextMenu + "Updated when save Excel", priority = 1)]
        public static void ToggleUpdateOnSaveLuaTextExcel()
        {
            Lua.Text.LuaTextAssetUpdater.Prefs.autoUpdate = !Lua.Text.LuaTextAssetUpdater.Prefs.autoUpdate;
        }

        [MenuItem(itemName: LuaTextMenu + "Updated when save Excel", isValidateFunction: true)]
        public static bool ToggleUpdateOnSaveLuaTextExcelValidate()
        {
            Menu.SetChecked(LuaTextMenu + "Updated when save Excel", Lua.Text.LuaTextAssetUpdater.Prefs.autoUpdate);
            return true;
        }

        #endregion

        #endif

        //===============================================================
        //  Resource.
        //===============================================================

        #region Resource

        protected const string ResourcesMenu = MenuRoot + "ExternalAsset/";

        //------ AssetDataBaseから読込 ------

        [MenuItem(itemName: ResourcesMenu + "Simulate Mode", priority = 500)]
        public static void ToggleSimulateExternalAsset()
        {
            ExternalAsset.Prefs.isSimulate = !ExternalAsset.Prefs.isSimulate;
        }

        [MenuItem(itemName: ResourcesMenu + "Simulate Mode", isValidateFunction: true)]
        public static bool ToggleSimulateExternalAssetValidate()
        {
            Menu.SetChecked(ResourcesMenu + "Simulate Mode", ExternalAsset.Prefs.isSimulate);
            return true;
        }

        //------ 外部アセット情報生成 ------

        [MenuItem(itemName: ResourcesMenu + "Generate AssetInfoManifest", priority = 1)]
        public static void OpenGenerateAssetInfoManifestWindow()
        {
            GenerateAssetInfoManifestWindow.Open();
        }

        //------ 外部アセット管理ウィンドウ ------

        [MenuItem(itemName: ResourcesMenu + "Open AssetManageWindow", priority = 12)]
        public static void OpenAssetManageWindow()
        {
            var projectResourceFolders = ProjectResourceFolders.Instance;

            ManageWindow.Open(projectResourceFolders.ExternalAssetPath, projectResourceFolders.ShareResourcesPath);
        }

        [MenuItem(itemName: ResourcesMenu + "Open AssetNavigationWindow", priority = 13)]
        public static void OpenAssetNavigationWindow()
        {
            var projectResourceFolders = ProjectResourceFolders.Instance;

            AssetNavigationWindow.Open(projectResourceFolders.ExternalAssetPath);
        }

        [MenuItem(itemName: ResourcesMenu + "Open AssetBundleDependencyChecker", priority = 14)]
        public static void OpenAssetBundleDependency()
        {
            FindDependencyAssetsWindow.Open();
        }

        [MenuItem(itemName: ResourcesMenu + "Open AssetBundleViewer", priority = 15)]
        public static void OpenAssetBundleViewer()
        {
            AssetBundleViewerWindow.Open();
        }

        [MenuItem(itemName: ResourcesMenu + "Open SimulationModeAssetFileTracker", priority = 16)]
        public static void OpenSimulationModeAssetFileTracker()
        {
            SimulationModeAssetFileTracker.Open();
        }

        //------ 全アセットバンドル名を再設定 ------

        [MenuItem(itemName: ResourcesMenu + "Apply AssetBundleNames", priority = 35)]
        public static void ApplyAssetBundleNames()
        {
            var assetManagement = AssetManagement.Instance;

            assetManagement.Initialize();

            assetManagement.ApplyAllAssetBundleName(true).Forget();
        }

        //------ 外部アセットビルド ------

        [MenuItem(itemName: ResourcesMenu + "Build ExternalAssets", priority = 36)]
        public static void BuildExecuteExternalAssets()
        {
            BuildExternalAssets.Execute().Forget();
        }

        #endregion

        //===============================================================
        //  CriWare.
        //===============================================================

        #region CriWare

        #if ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_ADX_LE || ENABLE_CRIWARE_SOFDEC

        protected const string CriWareMenu = MenuRoot + "CriWare/";

        [MenuItem(itemName: CriWareMenu + "Audio Mute", priority = 0)]
        public static void ToggleEditorCriWareMute()
        {
            EditorCriWareMute.Prefs.editorAudioMute = !EditorCriWareMute.Prefs.editorAudioMute;
        }

        [MenuItem(itemName: CriWareMenu + "Audio Mute", validate = true)]
        public static bool ToggleEditorCriWareMuteValidate()
        {
            UnityEditor.Menu.SetChecked(CriWareMenu + "Audio Mute", EditorCriWareMute.Prefs.editorAudioMute);

            return true;
        }

        [MenuItem(itemName: CriWareMenu + "Open CriAssetUpdateWindow", priority = 1)]
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
        //  Utility.
        //===============================================================

        #region Utility

        protected const string UtilityMenu = MenuRoot + "Utility/";

        [MenuItem(itemName: UtilityMenu + "Open SceneLaunchWindow", priority = 0)]
        public static void OpenSceneLaunchWindow()
        {
            SceneLaunchWindow.Open();
        }

        [MenuItem(itemName: UtilityMenu + "Open RayCastViewerWindow", priority = 1)]
        public static void OpenRaycastViewerWindow()
        {
            RaycastViewerWindow.Open();
        }

        [MenuItem(itemName: UtilityMenu + "Open TextureViewerWindow", priority = 2)]
        public static void OpenTextureViewerWindow()
        {
            TextureViewerWindow.Open();
        }

        [MenuItem(itemName: UtilityMenu + "Open BuiltInAssetsWindow", priority = 3)]
        public static void OpenBuiltInAssetsWindow()
        {
            BuiltInAssetsWindow.Open();
        }

        [MenuItem(itemName: UtilityMenu + "Open AssetDependenciesWindow", priority = 4)]
        public static void OpenAssetDependenciesWindow()
        {
            AssetDependenciesWindow.Open();
        }

        [MenuItem(itemName: UtilityMenu + "Open BlockInputMonitorWindow", priority = 5)]
        public static void OpenBlockInputMonitorWindow()
        {
            BlockInputMonitorWindow.Open();
        }

        [MenuItem(itemName: UtilityMenu + "Open BehaviorControlMonitor", priority = 6)]
        public static void OpenBehaviorControlMonitor()
        {
            BehaviorControlMonitor.Open();
        }

        [MenuItem(itemName: UtilityMenu + "SpriteAtlas/Fix SpriteAtlas Source", priority = 6)]
        public static void ExecuteFixSpriteAtlasSource()
        {
            FixSpriteAtlasSource.Modify(FixSpriteAtlasSource.DefaultTargetPlatforms);
        }

        #region Pining

        protected const string PiningMenu = UtilityMenu + "Pining/";

        [MenuItem(itemName: PiningMenu + "Open ProjectPinWindow", priority = 1)]
        public static void OpenProjectPinWindow()
        {
            ProjectPinningWindow.Open();
        }

        [MenuItem(itemName: PiningMenu + "Open HierarchyPinWindow", priority = 2)]
        public static void OpenHierarchyPinWindow()
        {
            HierarchyPinningWindow.Open();
        }

        #endregion

        #region ForceReSerialize

        protected const string ForceReSerializeMenu = UtilityMenu + "ForceReSerialize/";

        [MenuItem(itemName: ForceReSerializeMenu + "SelectionAssets", priority = 0)]
        public static void ForceReSerializeSelectionAssets()
        {
            ForceReSerializeAssets.ExecuteSelectionAssets();
        }

        [MenuItem(itemName: ForceReSerializeMenu + "All Prefabs", priority = 1)]
        public static void ForceReSerializeAllPrefabs()
        {
            ForceReSerializeAssets.ExecuteAllPrefabs();
        }

        #endregion

        #region Cleaner

        protected const string CleanerMenu = UtilityMenu + "Clean/";

        [MenuItem(itemName: CleanerMenu + "Open CleanDirectoryWindow", priority = 0)]
        public static void OpenCleanDirectoryWindow()
        {
            CleanDirectoryWindow.Open();
        }

        [MenuItem(itemName: CleanerMenu + "Clean DummyText (All Prefabs)", priority = 1)]
        public static void CleanAllPrefabDummyText()
        {
            PrefabDummyTextCleaner.CleanAllPrefabContents();
        }

        [MenuItem(itemName: CleanerMenu + "Clean ParticleSystem (Selection GameObject)", priority = 2)]
        public static void CleanParticleSystem()
        {
            ParticleSystemCleaner.CleanSelectionTarget();
        }

        #endregion

        #endregion

        //===============================================================
        //  Settings.
        //===============================================================

        #region Settings

        protected const string SettingsMenu = MenuRoot + "Settings/";

        [MenuItem(itemName: SettingsMenu + "Open DefineSymbolWindow", priority = 0)]
        public static void OpenDefineSymbolWindow()
        {
            DefineSymbolWindow.Open();
        }

        [MenuItem(itemName: SettingsMenu + "Open UnityConsoleConfigWindow", priority = 1)]
        public static void OpenUnityConsoleConfigWindow()
        {
            UnityConsoleConfigWindow.Open();
        }
        
        //------ コンパイル時のSceneView表示 ------

        [MenuItem(itemName: SettingsMenu + "Show CompilingView", priority = 2)]
        public static void ToggleCompileNotificationMode()
        {
            CompileNotificationView.SetEnable(!CompileNotificationView.Prefs.enable);
        }

        [MenuItem(itemName: SettingsMenu + "Show CompilingView", validate = true)]
        public static bool ToggleCompileNotificationModeValidate()
        {
            UnityEditor.Menu.SetChecked(SettingsMenu + "Show CompilingView", CompileNotificationView.Prefs.enable);
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

        //------ TextMeshProのダイナミックフォントのテクスチャリフレッシュ無効化 ------

        [MenuItem(itemName: SettingsMenu + "Disable CleanDynamicFontAsset", priority = 7)]
        public static void ToggleCleanDynamicFontAsset()
        {
            CleanDynamicFontAsset.Prefs.disable = !CleanDynamicFontAsset.Prefs.disable;
        }

        [MenuItem(itemName: SettingsMenu + "Disable CleanDynamicFontAsset", isValidateFunction: true)]
        public static bool ToggleCleanDynamicFontAssetValidate()
        {
            Menu.SetChecked(SettingsMenu + "Disable CleanDynamicFontAsset", CleanDynamicFontAsset.Prefs.disable);
            return true;
        }

        #endregion

        //===============================================================
        //  Tools.
        //===============================================================

        #region Tools

        protected const string ToolsMenu = MenuRoot + "Tools/";

        [MenuItem(itemName: ToolsMenu + "Open ShaderVariantWindow", priority = 6)]
        public static void OpenShaderVariantUpdateWindow()
        {
            ShaderVariantUpdateWindow.Open();
        }

        [MenuItem(itemName: ToolsMenu + "Open PatternTexturePacker", priority = 10)]
        public static void OpenPatternTexturePacker()
        {
            PatternTexturePacker.Open();
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

        [MenuItem(itemName: DirectoryMenu + "Open ConsoleLogPath", priority = 3)]
        private static void OpenConsoleLogPath()
        {
            OpenDirectory(Application.consoleLogPath);
        }

        [MenuItem(itemName: DirectoryMenu + "Open TemplateFilePath", priority = 4)]
        private static void OpenTemplateFilePath()
        {
            const string TemplateFolder = "Resources/ScriptTemplates/";

            var templateFilePath = PathUtility.Combine(EditorApplication.applicationContentsPath, TemplateFolder);

            var filePath = Directory.GetFiles(templateFilePath).FirstOrDefault();

            OpenDirectory(string.IsNullOrEmpty(filePath) ? templateFilePath : filePath);
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

        [MenuItem(itemName: PrereleaseMenu + "Open ApiMonitorWindow", priority = 0)]
        public static void OpenApiMonitorWindow()
        {
            ApiMonitorWindow.Open();
        }

        #endregion

        //===============================================================
        //  HierarchyMenu.
        //===============================================================

        #region HierarchyMenu

        protected const string HierarchyMenu = "GameObject/";

        [MenuItem(itemName: HierarchyMenu + "Copy Hierarchy Path", priority = int.MaxValue)]
        public static void CopyHierarchyPath()
        {
            var hierarchyPath = UnityUtility.GetHierarchyPath(Selection.activeGameObject);

            GUIUtility.systemCopyBuffer = hierarchyPath;
        }

        [MenuItem(itemName: HierarchyMenu + "Copy Hierarchy Path", validate = true)]
        public static bool CopyHierarchyPathValidate()
        {
            if (1 < Selection.gameObjects.Length){ return false; }

            if (Selection.activeGameObject == null){ return false; }

            if (Selection.activeGameObject.scene == null){ return false; }

            return true;
        }

        #endregion
    }
}
