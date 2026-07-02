# Devkit

> **namespace**: `Modules.Devkit.*`（サブ機能ごとに分割。一部フォルダ名と不一致 → 「注意点・罠」参照）
> **場所**: `Client/Assets/UniModules/Scripts/Modules/Devkit/`（約200ファイル・42サブフォルダ）
> **Client側使用**: 約100ファイル（2026-07時点。うち82ファイルが `Modules.Devkit.Console`）
> **依存**: UniTask / R3 / Newtonsoft.Json / Extensions（`Singleton<T>`, `LifetimeDisposable`, `SingletonEditorWindow<T>` 等）/ SRDebugger（ThirdParty, `ENABLE_SRDEBUGGER` 時）

## 概要

エディタ開発支援ツール群 + 実機デバッグ機能の集合モジュール。大部分は `Editor/` 配下のエディタ専用だが、**Console（開発ログ）・Diagnosis（実機デバッグUI/レポート送信）・LogHandler（ログ捕捉）・ApiMonitor（API履歴）はランタイムコードから使用する**。
本ドキュメントは巨大モジュールのため「サブ機能カタログ」形式。ランタイムから使う Console / Diagnosis のみ API を詳細に記載する。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| 開発時のみ出る情報ログを出したい | `UnityConsole.Info(message)`（リリースビルドでは自動無効） |
| 機能別に色分け・ON/OFF可能なログを出したい | `UnityConsole.Event(eventName, color, message, logType)` |
| ログのイベント別表示切替（エディタ） | メニュー `Extension/Settings/Open UnityConsoleConfigWindow` |
| Unityの全ログ（例外含む）を購読したい | `ApplicationLogHandler.Instance.OnReceivedAllAsObservable()` 等 |
| 実機でFPS/メモリ表示・デバッグパネルを出したい | `Diagnosis`（prefabを `Initialize()`。SRDebugger 連携） |
| 実機から不具合レポート（SS+ログ）を送信したい | `SendReportManager.Instance.Send(title)`（BugLogServer 連携） |
| SRDebugger のデバッグメニューに項目を足したい | `SROptions` partial（`Client/Assets/Scripts/Client/Devkit/SRDebugger/SROptions.cs`） |
| API通信の履歴を見たい | `ApiTracker`（自動記録）+ メニュー `Extension/Prerelease/Open ApiMonitorWindow` |
| エディタ設定をプロジェクト単位で保存したい | `ProjectPrefs.GetBool/SetBool/...`（エディタ専用） |
| プロジェクト設定アセット（単一SO）を作りたい | `SingletonScriptableObject<T>` 継承（エディタ専用） |
| アセットインポート時の自動設定を追加したい | `AssetTuner` 継承 + `AssetTuneManager.Instance.Register<T>()` |
| Hierarchy 行に独自アイコン/トグルを描きたい | `ItemContentDrawer` 継承 + `HierarchyItemDrawer<T>.AddDrawer<T>()` |
| シーン保存/Prefab適用をフックしたい | `CurrentSceneSaveHook` / `PrefabApplyHook` / `PrefabModeEventHook`（EventHook） |
| アセットの参照元を探したい | Projectビュー右クリック `Assets/Search Asset References`（FindReferences） |
| アセットの依存先を探したい | 右クリック `Assets/Find Dependencies In Project`（AssetDependencies） |
| アプリをビルドしたい | メニュー `Dominion/Build/...`（`BuildManager.Build` + `ApplicationBuilder` 派生） |
| Scenes/Tags/Layers の定数クラスを再生成したい | メニュー `Extension/Generators/Scripts/All Scripts` |
| ScriptableObject アセットをコードから生成したい | `ScriptableObjectGenerator.Generate<T>()` |
| DefineSymbol をコード/GUIで操作したい | `DefineSymbol.Add/Delete/Contains` / `Extension/Settings/Open DefineSymbolWindow` |
| 起動シーンを経由してPlayしたい | メニュー `Extension/Utility/Open SceneLaunchWindow` |
| マスターの中身をエディタで見たい/編集したい | メニュー `Extension/Master/Open MasterViewer`（→ [Master.md](Master.md)） |
| テクスチャの圧縮状態を一覧したい | メニュー `Extension/Utility/Open TextureViewerWindow` |
| sln/csproj 生成をカスタムしたい | `VisualStudioFileCallback` / `ProjectFilesGenerator`（`ENABLE_VSTU`） |
| シーン内にメモを残したい（ビルド非混入） | `Memo` コンポーネント（Productionビルド時に自動除去） |

## サブ機能カタログ

### ランタイム系（実行時コードから使用可）

| サブ機能 | namespace | 主要クラス | 役割 |
|---|---|---|---|
| Console | `Modules.Devkit.Console` | `UnityConsole`(static) | 開発ログ出力。**詳細後述** |
| Diagnosis | `Modules.Devkit.Diagnosis`(+`.SRDebugger`/`.SendReport`/`.LogTracker`) | `Diagnosis`, `SRDiagnosis`, `SendReportManager`, `UnityLogTracker` | 実機デバッグUI・レポート送信。**詳細後述** |
| LogHandler | `Modules.Devkit.LogHandler` | `ApplicationLogHandler`(Singleton) | `Application.logMessageReceived(Threaded)` をR3 Observable化 |
| Log | `Modules.Devkit.Log`（実体は `Extensions/Devkit/Log/DebugLog.cs`） | `DebugLog`(static) | ログ文字列の中継ハブ（Receive→Observable）。Bugsnag通知等の橋渡し |
| ApiMonitor | **`Modules.Net.WebRequest`**（フォルダ名と不一致） | `ApiTracker`(Singleton), `ApiInfo` | API通信履歴の記録（直近100件）。`UnityWebRequestManager` が自動で `Start/OnComplete/OnRetry/OnError` を呼ぶ。閲覧は `ApiMonitorWindow`（エディタ） |
| Memo | `Modules.Devkit.Memo` | `Memo`(MonoBehaviour) | GameObjectにメモ文字列を残すだけのコンポーネント。`MemoComponentRemover`（エディタ）でビルド時全削除 |
| ChatWork | `Modules.Devkit.ChatWork` | `ChatWorkMessage` | ChatWork API へメッセージ/ファイル送信（`SendMessage`/`SendFile`）。CI通知用 |
| ExternalAsset | `Modules.Devkit.ExternalAssets` | `SimulationModeAssetFileTrackerBridge`（ランタイム）+ `SimulationModeAssetFileTracker`（エディタWindow） | シミュレートモードで読まれた配信アセットの追跡 |

### エディタ専用（`Editor/` 配下）

| サブ機能 | namespace | 主要クラス | 目的 / 入口 |
|---|---|---|---|
| AssetTuning | `Modules.Devkit.AssetTuning`(+`.TextureAsset`) | `AssetTuner`(abstract), `AssetTuneManager`(Singleton), `AssetTuningPostprocessor` | アセットインポート時の自動設定（Texture/Audio/Animator/SpriteAtlas/AssetBundle名等）。登録制: Client側 `Editor/AssetTuner/AssetTunerRegister.cs` が `[InitializeOnLoadMethod]` で登録 |
| Build | `Modules.Devkit.Build` | `BuildManager`(static), `ApplicationBuilder<TParam>`(abstract), `BuildParameter`, `BuiltInAssets`, `DeleteAssetSetting` | アプリビルドのフレームワーク。`BuildManager.Build(IApplicationBuilder)` が本体。Client実装: `Editor/Build/ApplicationBuilder.cs`、入口 `Dominion/Build/...` |
| Generators | `Modules.Devkit.Generators` | `ScenesScriptGenerator`, `TagsScriptGenerator`, `LayersScriptGenerator`, `SortingLayersScriptGenerator`, `ScenesInBuildGenerator`, `ScriptableObjectGenerator`, `ScriptGenerateUtility` | 定数スクリプト自動生成（`Constants/Scenes.cs` 等。テンプレの `#NAMESPACE#` を置換）と SO アセット生成。入口 `Extension/Generators/...` |
| Inspector | `Modules.Devkit.Inspector` | `DefaultAssetInspector`, `ExtendInspector`(abstract), `FolderInspector`, `RegisterScrollView<T>`(abstract), `TransformInspector`, `RectTransformSanitizer` | インスペクタ拡張基盤。`DefaultAssetInspector.AddExtendInspector<T>()` で登録（Client: `Editor/Inspector/InspectorRegister.cs`）。`RegisterScrollView<T>` は設定Window用の追加/削除可能リストUI部品 |
| EventHook | `Modules.Devkit.EventHook` | `HierarchyChangeNotification`, `CurrentSceneSaveHook`, `PrefabApplyHook`, `PrefabModeEventHook`, `AdditionalComponent` | エディタイベントのObservable化とコンポーネント自動付与。`AdditionalComponent.RegisterRequireComponents(...)` |
| Hierarchy | `Modules.Devkit.Hierarchy` | `HierarchyItemDrawer<T>`(Singleton), `ItemContentDrawer`(abstract), `ActiveToggleDrawer`, `ComponentIconDrawer`, `MissingComponentDrawer` | Hierarchy行の装飾（トグル/アイコン/Missing警告）。Client登録: `Editor/Hierarchy/HierarchyItemDrawer.cs`。表示切替 `Extension/Settings/Hierarchy/...` |
| MasterViewer | `Modules.Devkit.MasterViewer` | `MasterViewerWindow<T>`, `MasterController`, `RecordWindow` | マスターデータ閲覧・実行中(`Application.isPlaying`)のみ編集可。Client実装: `Editor/Master/MasterViewerWindow.cs`。入口 `Extension/Master/Open MasterViewer`（→ [Master.md](Master.md)） |
| MasterGenerator | **`Modules.Master`**（フォルダ名と不一致） | `MasterGenerator`, `MasterConfig`, `RecordDataLoader`, `MasterS3Uploader` | .record(YAML)→.master 生成とS3アップロード（→ [Master.md](Master.md)） |
| MasterFileNameViewer | **`Modules.Master`** | `MasterFileNameWindow<T>`(abstract) | マスター名⇔暗号化ファイル名の対応表示。入口 `Extension/Master/Open MasterFileNameViewer` |
| TextureViewer | `Modules.Devkit.TextureViewer` | `TextureViewerWindow`, `TextureInfo`, `TextureViewerConfig` | 全テクスチャのサイズ・圧縮設定を一覧表示。入口 `Extension/Utility/Open TextureViewerWindow` |
| ValidateAsset | `Modules.Devkit.ValidateAsset.TextureSize` / `.UnityWarning` | `ValidateTextureSize`, `TextureSizeValidateConfig`, `TextureSizeChatWorkNotify`, `UnityWarningChatWorkNotify` | テクスチャサイズ規約違反の検出とChatWork通知（CI連携） |
| VisualStudio | `Modules.Devkit.VisualStudio` | `VisualStudioFileCallback`(static), `ProjectFilesGenerator`, `SolutionFile`, `ProjectFile` | sln/csproj 生成フック（`ENABLE_VSTU`）。Client登録: `Editor/ProjectFileHook.cs` |
| Prefs | `Modules.Devkit.Prefs` | `ProjectPrefs`(static) | `EditorPrefs` にプロジェクト識別子（`Application.dataPath` のhash）を付けて保存。Get/Set: Bool/String/Int/Float/Enum/Color/Class(Json)/Asset |
| Project | `Modules.Devkit.Project` | `ProjectResourceFolders`, `ProjectScriptFolders`, `ProjectUnityFolders`, `ProjectCryptoKey`（全て `SingletonScriptableObject`） | プロジェクトのフォルダ構成定義（`ExternalAssetPath`/`ConstantsScriptPath` 等）と暗号鍵（`GetCryptoKey()`）。他ツールの位置解決に使われる |
| ScriptableObject | **`Modules.Devkit.ScriptableObjects`**（複数形） | `SingletonScriptableObject<T>`, `ReloadableScriptableObject` | エディタ専用シングルトンSOアセット基盤。`Instance` でアセット検索ロード。設定Config類は全てこれ |
| SerializeAssets | `Modules.Devkit.SerializeAssets` | `ForceReSerializeAssets`(static) | アセット強制再シリアライズ（`ExecuteSelectionAssets`/`ExecuteAllPrefabs`/`ExecuteAllAssets`）。入口 `Extension/Utility/ForceReSerialize/...` |
| CleanComponent | `Modules.Devkit.CleanComponent` | `DummyTextCleaner`, `PrefabDummyTextCleaner`, `SceneDummyTextCleaner`, `CleanDummyTextAssetModificationProcessor`, `ParticleSystemCleaner` | `DummyText`/`TextSetter`（Modules.UI/TextData）のエディタ用ダミー文言を保存・ビルド時に除去。ParticleSystem の不要モジュール掃除 |
| CleanDirectory | `Modules.Devkit.CleanDirectory` | `CleanDirectoryWindow`, `SaveCleanDirectory`(AssetModificationProcessor) | 空ディレクトリの検出・削除（保存時自動 + 手動Window） |
| AssemblyCompilation | `Modules.Devkit.AssemblyCompilation` | `AssemblyCompilation<T>`(abstract Singleton), `CompileResult`, `CompileNotificationView` | コンパイル要求・結果取得（CI用）とSceneViewへのコンパイル中表示（`Extension/Settings/Show CompilingView`） |
| DefineSymbol | `Modules.Devkit.DefineSymbol` | `DefineSymbol`(static), `DefineSymbolWindow`, `DefineSymbolConfig` | ScriptingDefineSymbols の操作（`Current`/`Set`/`Add`/`Insert`/`Delete`/`Contains`） |
| SceneImporter | `Modules.Devkit.SceneImporter` | `SceneImporterConfig`(SingletonSO), `SceneAssetPostprocessor` | 管理対象シーンフォルダと初期シーンの定義（`GetInitialScenePath()`/`GetManagedFolderPaths()`）。Scenes定数生成・SceneLaunch の元データ |
| SceneLaunch | `Modules.Devkit.SceneLaunch` | `SceneLaunchWindow`, `SceneSelector` | 任意シーンを初期シーン経由でPlay起動。入口 `Extension/Utility/Open SceneLaunchWindow` |
| Pinning | `Modules.Devkit.Pinning` | `PinningWindow<T>`(abstract), `ProjectPinningWindow`, `HierarchyPinningWindow` | アセット/Hierarchyオブジェクトのピン留めランチャー。入口 `Extension/Utility/Pining/...` |
| FindReferences | `Modules.Devkit.FindReferences` | `FindReferencesInProject`, `FindSpriteReferencesInProject`, `FindReferencesResultWindow` | 選択アセットの**参照元**検索。入口 右クリック `Assets/Search Asset References` |
| AssetDependencies | `Modules.Devkit.AssetDependencies` | `FindDependenciesInProject`, `AssetDependenciesWindow` | 選択アセットの**依存先**検索。入口 右クリック `Assets/Find Dependencies In Project` / `Extension/Utility/Open AssetDependenciesWindow` |
| AssetBundle | `Modules.Devkit.AssetBundleViewer` / `Modules.Devkit.AssetBundles` | `AssetBundleViewerWindow`, `FindDependencyAssetsWindow` | AssetBundle の内容・依存の可視化。入口 `Extension/ExternalAsset/Open AssetBundleViewer` / `Open AssetBundleDependencyChecker` |
| ShaderVariants | **`Modules.Devkit.ShaderVariant`**（単数形） | `ShaderVariantUpdateWindow` | ShaderVariantCollection の更新。入口 `Extension/Tools/Open ShaderVariantWindow` |
| U2D | `Modules.Devkit.U2D` | `RaycastViewerWindow` | uGUIレイキャスト対象の可視化。入口 `Extension/Utility/Open RayCastViewerWindow` |
| TextMeshPro | `Modules.Devkit.TextMeshPro` | `CleanDynamicFontAsset`(static), `DynamicFontAssetModificationProcessor` | 動的フォントアセットの肥大データを保存時にクリア。無効化: `Extension/Settings/Disable CleanDynamicFontAsset` |
| Console(Editor) | `Modules.Devkit.Console` | `UnityConsoleConfig`(SingletonSO), `UnityConsoleConfigWindow` | UnityConsole のイベント別表示ON/OFF設定 |
| Diagnosis(Editor相当) | - | - | なし（Diagnosis はランタイム構成のみ） |
| ClassAnalyzer | `Modules.Devkit.ClassAnalyzer` | `SealedClassAnalyzer`(static) | sealed 付与可能クラスの検索（`SearchTypes`） |
| JsonFile | `Modules.Devkit.JsonFile` | `JsonFileLoader`, `JsonFileLoaderPropertyDrawer` | JSONファイルの型付きロード（`Load<T>()`）+ PropertyDrawer |
| Spreadsheet | `Modules.Devkit.Spreadsheet` | `SpreadsheetConnector`, `SpreadsheetConfig`, `SpreadsheetConnectionWindow` | Google Spreadsheet OAuth接続・取得 |
| WebView | `Modules.Devkit.WebView` | `EditorWebViewWindow`(abstract) | エディタ内WebView（`OpenUrl`/`Back`/`Forward`/`Reload`） |
| Mac | `Modules.Devkit`（直下） | `LaunchUnityForMac` | Mac用Unity再起動メニュー（`Help/Launch Unity`） |
| CleanComponent(参考) | - | `Build/Editor/DeleteAsset/DeleteAssetSetting` | タグ指定でビルド時に削除するアセット定義（iOS本番で使用） |

## エディタメニューパス一覧

メニュー登録の実体は基盤 `Client/Assets/UniModules/Scripts/Editor/EditorMenu.cs`（`Modules.EditorMenu`）。Client側 `Client/Assets/Scripts/Editor/EditorMenu.cs`（`Dominion.Editor.EditorMenu`）がこれを**継承**して `Extension/Master/...` 等を追加、`ProductEditorMenu.cs` が `Dominion/...` を追加する。

| メニューパス | 開く/実行するもの |
|---|---|
| `Extension/Generators/Scripts/All Scripts`（+個別 Scenes.cs / Tags.cs / Layers.cs / SortingLayers.cs） | 定数スクリプト生成 |
| `Extension/Generators/Generate ScenesInBuild` | `ScenesInBuildGenerator.Generate()` |
| `Extension/Generators/Generate ScriptableObject` | `ScriptableObjectGenerator.Generate()`（選択スクリプトからSO生成） |
| `Extension/Master/Generate` ほか Master系 | → [Master.md](Master.md)（Client側 EditorMenu で追加） |
| `Extension/ExternalAsset/Open AssetBundleDependencyChecker` | `FindDependencyAssetsWindow` |
| `Extension/ExternalAsset/Open AssetBundleViewer` | `AssetBundleViewerWindow` |
| `Extension/ExternalAsset/Open SimulationModeAssetFileTracker` | `SimulationModeAssetFileTracker` |
| `Extension/Utility/Open SceneLaunchWindow` | `SceneLaunchWindow` |
| `Extension/Utility/Open RayCastViewerWindow` | `RaycastViewerWindow` |
| `Extension/Utility/Open TextureViewerWindow` | `TextureViewerWindow` |
| `Extension/Utility/Open BuiltInAssetsWindow` | `BuiltInAssetsWindow`（ビルド内蔵アセット解析） |
| `Extension/Utility/Open AssetDependenciesWindow` | `AssetDependenciesWindow` |
| `Extension/Utility/Open BlockInputMonitorWindow` | InputControlモジュールのWindow（→ [InputControl.md](InputControl.md)） |
| `Extension/Utility/SpriteAtlas/Fix SpriteAtlas Source` | `FixSpriteAtlasSource.Modify`（namespace `Modules.Devkit.UI`、実体は `Modules/UI/SpriteAtlas/Editor/`） |
| `Extension/Utility/Pining/Open ProjectPinWindow` / `Open HierarchyPinWindow` | Pinning |
| `Extension/Utility/ForceReSerialize/SelectionAssets・All Prefabs・All Assets` | `ForceReSerializeAssets` |
| `Extension/Utility/Clean/Open CleanDirectoryWindow` | `CleanDirectoryWindow` |
| `Extension/Utility/Clean/Clean DummyText (All Prefabs)` | `PrefabDummyTextCleaner.CleanAllPrefabContents()` |
| `Extension/Utility/Clean/Clean ParticleSystem (Selection GameObject)` | `ParticleSystemCleaner.CleanSelectionTarget()` |
| `Extension/Settings/Open DefineSymbolWindow` | `DefineSymbolWindow` |
| `Extension/Settings/Open UnityConsoleConfigWindow` | `UnityConsoleConfigWindow` |
| `Extension/Settings/Show CompilingView`（トグル） | `CompileNotificationView` |
| `Extension/Settings/Hierarchy/ComponentIcon・MissingComponent・ActiveToggle`（トグル） | Hierarchy装飾のON/OFF |
| `Extension/Settings/Auto Add Component/Disable・Log`（トグル） | `AdditionalComponent.Prefs` |
| `Extension/Settings/Disable CleanDynamicFontAsset`（トグル） | `CleanDynamicFontAsset.Prefs.disable` |
| `Extension/Tools/Open ShaderVariantWindow` | `ShaderVariantUpdateWindow` |
| `Extension/Prerelease/Open ApiMonitorWindow` | `ApiMonitorWindow`（ApiTracker履歴表示） |
| `Extension/Directory/Open PersistentDataPath` 等 | 各種ディレクトリをエクスプローラで開く |
| `GameObject/Copy Hierarchy Path` | 選択オブジェクトの階層パスをコピー |
| （右クリック）`Assets/Search Asset References` | `FindReferencesInProject` |
| （右クリック）`Assets/Find Dependencies In Project` | `FindDependenciesInProject` |
| （右クリック）`Assets/Find Sprite References In Project` | `FindSpriteReferencesInProject` |
| （コンポーネント右クリック）`CONTEXT/RectTransform/Sanitize Values` | `RectTransformSanitizer`（浮動小数の丸め） |
| `Dominion/Build/iOS・Android / Development・Production` | Client側ビルド（`ApplicationBuilder` 使用） |

---

# 詳細: Modules.Devkit.Console（ランタイム使用・Client側82ファイル）

## なぜランタイムコードで使われているか

`UnityConsole` は**リリースビルドで自動的に無効化される開発用ログ**だから。出力可否は `UnityConsole.Enable` が一元判定する:

- `UNITY_EDITOR` または `ENABLE_DEVKIT` 定義時 → 常に有効
- それ以外（実機） → `Debug.isDebugBuild`（Developmentビルドのみ有効）

つまり本番ビルドでは呼び出しが空振りになるため、Client側の各Managerがログを `#if` で囲まずに書ける。加えてエディタでは `UnityConsoleConfigWindow` でイベント名単位の表示ON/OFFができ、`[Battle]` `[Initialize]` のような色付きプレフィックスでフィルタしやすい。デフォルトでスタックトレースを抑制する（`DisableStackTraceScope`）ためコンソールが汚れない。

## 主要クラス

| クラス | 種別 | 役割 |
|---|---|---|
| `UnityConsole` | static | ログ出力本体（ランタイム可）。非メインスレッドからも呼べる（`SynchronizationContext.Post` でメインへ） |
| `UnityConsoleManager` | Singleton（**エディタ専用**、`#if UNITY_EDITOR`） | イベント別有効状態の保存・判定（`ProjectPrefs` にJSON保存） |
| `ConsoleInfo` | Serializable | eventName + enable のペア |
| `UnityConsoleConfig` | SingletonScriptableObject（エディタ専用） | 定義済みイベント名リストのアセット |
| `UnityConsoleConfigWindow` | SingletonEditorWindow（エディタ専用） | イベント別表示ON/OFFのGUI。`Extension/Settings/Open UnityConsoleConfigWindow` |

## API（UnityConsole）

| メンバー | 説明 |
|---|---|
| `static bool Enable { get; }` | 出力可否（上記判定） |
| `static HashSet<string> DisableEventNames { get; }` | ランタイムから特定イベントを抑制したい時に追加 |
| `static void Info(string message, LogType logType = LogType.Log)` | `[Info]`（シアン）イベントで出力 |
| `static void Info(string format, params object[] args)` | Format版 |
| `static void Event(string eventName, Color color, string message, LogType logType = LogType.Log)` | 任意イベント名・色で出力。`LogType.Log/Warning/Error` のみ対応（他は `NotSupportedException`） |
| `static void EnableNextLogStackTrace()` | 次の1回だけスタックトレース付きで出力 |
| `UnityConsole.InfoEvent.ConsoleEventName / ConsoleEventColor` | 既定イベント定義（"Info" / シアン） |

## 使い方(実例)

```csharp
// Client/Assets/Scripts/Client/Battle/Develop/BattleLog.cs（機能専用ログクラスの定型）
public static class BattleLog
{
    public static readonly string ConsoleEventName = "Battle";
    public static readonly Color ConsoleEventColor = new Color(1.0f, 0.2f, 0.2f);

    public static void Log(string message)
    {
        if (!Enable){ return; }

        #if DEVELOPMENT

        UnityConsole.Event(ConsoleEventName, ConsoleEventColor, message);

        #endif
    }
}
```

```csharp
// Client/Assets/Scripts/Client/Core/Initialize/InitializeObject/InitializeObject.cs
UnityConsole.Event("Initialize", Color.cyan, $"{sw.Elapsed.TotalMilliseconds:F2}ms");

// Client/Assets/Scripts/Client/Core/SaveData/SaveDataManager.cs（単発情報ログ）
UnityConsole.Info($"SaveData save skip. IsLoaded = {Instance.IsLoaded}");

// Client/Assets/Scripts/Client/Core/PlayHub/PlayHubManager.android.cs（エラーログ）
UnityConsole.Event(ConsoleEventName, ConsoleEventColor, errorMessage, LogType.Error);
```

---

# 詳細: Modules.Devkit.Diagnosis（実機デバッグ・SRDebugger連携）

## 構成

実機上のデバッグ機能一式。`Diagnosis` prefab を `InitializeObject.devkit.cs` が生成・初期化する（`ENABLE_DEVKIT || ((UNITY_EDITOR || DEVELOPMENT) && !PRODUCTION)` 時のみコンパイル）。

```
Diagnosis (SingletonMonoBehaviour, prefab)
 ├─ FpsStats      : FPS表示（TextMeshProUGUI, 0.5秒間隔）
 ├─ MemoryStats   : メモリ使用量表示
 └─ SRDiagnosis   : SRDebugger 連携ボタン（ログ種別で色が変わる。ENABLE_SRDEBUGGER 時のみ有効）
      └─ SRDebugger パネル内タブ: SendReportSheetController（レポート送信UI基底）

SendReportManager (Singleton) : SS+直近ログ+任意項目を ISendReportUploader で送信
 ├─ UnityLogTracker (Singleton) : 直近100件のログを FixedQueue に自動記録（RuntimeInitializeOnLoadMethod で自動起動）
 ├─ DefaultSendReportBuilder    : OS/端末/メモリ/ログ/SS を reportContents に詰める既定実装
 └─ SendReportUploader          : UnityWebRequest で Form/Json POST する既定実装
```

## 主要クラス

| クラス | 種別 | 役割 |
|---|---|---|
| `Diagnosis` | SingletonMonoBehaviour | 表示統括。`Initialize(bool srDiagnosisEnable = true)` / `DisplayFpsStats` / `DisplayMemoryStats`（SecurePrefsに保存） / `SetTouchBlock(GameObject)` |
| `FpsStats` / `MemoryStats` | MonoBehaviour | 数値表示。`IsEnable`（既定 `Debug.isDebugBuild`）/ `Initialize()` |
| `SRDiagnosis` | MonoBehaviour | SRDebuggerパネル開閉・ログ色通知。`IgnoreWarnings` / `IgnoreErrors`（プレフィックス一致で無視） |
| `SendReportManager` | Singleton | レポート送信本体（下記API） |
| `ISendReportUploader` / `SendReportUploader` | interface / 実装 | `Upload(reportTitle, reportContents, progress, cancelToken)`。URL+`DataFormat.Form/Json` |
| `ISendReportBuilder` / `DefaultSendReportBuilder` | interface / 実装 | 送信内容の構築（差し替え可: `SetReportBuilder`） |
| `SendReportResult` | class | ResponseCode / Text / Bytes / Error / HasError |
| `UnityLogTracker` | Singleton | `ApplicationLogHandler` 経由で直近ログ蓄積。`Logs` / `ReportLogNum`（既定100） |
| `SendReportSheetController` | abstract MonoBehaviour | SRDebuggerタブのレポート送信UI基底（title入力+送信ボタン+進捗バー） |
| `SendReportTabController` | MonoBehaviour | SRDebuggerのタブ有効化（`IEnableTab`） |

## API（SendReportManager）

| メンバー | 説明 |
|---|---|
| `void Initialize(ISendReportUploader uploader)` | 初期化（必須・1回） |
| `void SetCryptKey(AesCryptoKey aesCryptoKey)` | 設定すると `AddReportContent` の値をAES暗号化 |
| `void SetReportBuilder(ISendReportBuilder builder)` | 送信内容構築の差し替え |
| `UniTask Send(string reportTitle, IProgress<float> progressNotifier = null, CancellationToken cancelToken = default)` | 送信実行（SS+ログ+追加項目） |
| `IEnumerator CaptureScreenShot()` | SS取得。**描画フレーム内制約のため `MonoBehaviour.StartCoroutine` で実行必須** |
| `void AddReportContent(string key, string value)` | 送信項目の追加（`OnRequestReportAsObservable` 購読内で呼ぶ） |
| `Observable<Unit> OnRequestReportAsObservable()` | 送信直前（追加項目を詰めるタイミング） |
| `Observable<SendReportResult> OnReportCompleteAsObservable()` | 送信完了（成否含む） |

## 使い方(実例)

```csharp
// Client/Assets/Scripts/Client/Core/Initialize/InitializeObject/InitializeObject.devkit.cs（起動時生成）
var diagnosis = UnityUtility.FindObjectOfType<Diagnosis>() ?? UnityUtility.Instantiate<Diagnosis>(null, diagnosisPrefab);

diagnosis.Initialize(srDiagnosisEnable);

diagnosis.SRDiagnosis.IgnoreWarnings = new string[]
{
    "SpriteAtlasManager.atlasRequested wasn't listened to while",
};
```

```csharp
// Client/Assets/Scripts/Client/Devkit/SRDebugger/SendReportController.cs（BugLogServerへの送信設定）
public sealed class SendReportController : SendReportSheetController
{
    public override void Initialize()
    {
        var sendReportManager = SendReportManager.Instance;

        var postReportUrl = "https://buglog.kurukurugames.com/buglog/report";

        var reportUploader = new SendReportUploader(postReportUrl, SendReportUploader.DataFormat.Json);

        sendReportManager.Initialize(reportUploader);
        sendReportManager.SetCryptKey(cryptoKey);

        sendReportManager.OnRequestReportAsObservable()
            .Subscribe(_ => OnRequestReport())   // ここで AddReportContent("UserCode", ...) 等を追加.
            .AddTo(this);

        base.Initialize();
    }
}
```

```csharp
// Client/Assets/Scripts/Client/Devkit/SRDebugger/SROptions.cs（SRDebuggerメニュー項目の追加: partial）
[Category("System"), DisplayName("Fps Stats"), Sort(0)]
public bool DisplayFpsStats
{
    get { return Diagnosis.Instance.DisplayFpsStats; }
    set {  Diagnosis.Instance.DisplayFpsStats = value; }
}
```

---

# 準詳細: LogHandler / Log（小規模ランタイム機能）

## ApplicationLogHandler（`Modules.Devkit.LogHandler`）

`Application.logMessageReceived` / `logMessageReceivedThreaded` を購読して R3 Observable として配信する Singleton。SRDiagnosis・UnityLogTracker・Bugsnag 連携の土台。

| メンバー | 説明 |
|---|---|
| `LogInfo`（内部クラス） | `Type`(LogType) / `Condition`(本文) / `StackTrace` |
| `Observable<LogInfo> OnReceivedAllAsObservable()` | メインスレッド発行の全ログ |
| `OnReceivedLog/Warning/Error/ExceptionAsObservable()` | 種別別 |
| `OnReceivedThreadedAll/Log/Warning/Error/ExceptionAsObservable()` | 全スレッド発行（`ObserveOn(UnityFrameProvider.Update)` と併用が定石） |

```csharp
// Client/Assets/UniModules/Scripts/Modules/Devkit/Diagnosis/SRDebugger/SRDiagnosis.cs（購読例）
applicationLogHandler.OnReceivedThreadedAllAsObservable()
    .ObserveOn(UnityFrameProvider.Update)
    .Subscribe(x => OnLogReceive(x))
    .AddTo(this);
```

## DebugLog（`Modules.Devkit.Log`・実体は `Extensions/Devkit/Log/DebugLog.cs`）

ログ文字列を `Receive*` で受けて Observable 配信するだけの static ハブ。Client では Bugsnag へのエラー転送に使用。

| メンバー | 説明 |
|---|---|
| `static void ReceiveLog/Warning/Error/Assert(string message)` / `ReceiveException(Exception)` | 通知の発行 |
| `static Observable<string> OnLogReceived/OnWarningReceived/OnErrorReceived/OnAssertReceivedAsObservable()` / `Observable<Exception> OnExceptionReceivedAsObservable()` | 購読 |

```csharp
// Client/Assets/Scripts/Client/Core/Initialize/InitializeObject/InitializeObject.core.cs
DebugLog.OnErrorReceivedAsObservable().Subscribe(x => OnError(x));
DebugLog.OnExceptionReceivedAsObservable().Subscribe(x => OnException(x));
```

---

# エディタ拡張の登録パターン（Client側実例）

Devkit のエディタ機能は「基盤が枠を提供し、Client側が `[InitializeOnLoadMethod]` で登録する」構造が共通。

```csharp
// Client/Assets/Scripts/Editor/AssetTuner/AssetTunerRegister.cs（AssetTuning: インポート自動設定の登録）
[InitializeOnLoadMethod]
private static void InitializeOnLoadMethod()
{
    foreach (var type in AssetTuners)   // TextureAssetTuner, AudioClipAssetTuner, SpriteAtlasAssetTuner 等.
    {
        AssetTuneManager.Instance.Register(type);
    }
}
```

```csharp
// Client/Assets/Scripts/Editor/Hierarchy/HierarchyItemDrawer.cs（Hierarchy装飾の登録）
public sealed class HierarchyItemDrawer : HierarchyItemDrawer<HierarchyItemDrawer>
{
    [InitializeOnLoadMethod]
    private static void InitializeOnLoadMethod()
    {
        Instance.AddDrawer<ActiveToggleDrawer>();
        Instance.AddDrawer<ComponentIconDrawer>();

        var missingComponentDrawer = Instance.AddDrawer<MissingComponentDrawer>();
    }
}
```

```csharp
// Client/Assets/Scripts/Editor/Inspector/InspectorRegister.cs（インスペクタ拡張の登録）
[InitializeOnLoadMethod]
private static void InitializeOnLoadMethod()
{
    DefaultAssetInspector.AddExtendInspector<FolderInspector>();
}
```

```csharp
// Client/Assets/Scripts/Editor/ProjectFileHook.cs（csproj/sln生成フックの登録: ENABLE_VSTU）
ProjectFilesGenerator.SolutionFile.AddHook(SolutionFile.AbortIfExists);
ProjectFilesGenerator.ProjectFile.AddHook(ProjectFile.IsUnityOrEditorOrPluginsProject, ProjectFile.ExcludeAnotherLanguageReference);
```

ビルドフロー実例（`ApplicationBuilder` 派生の `OnBeforeBuild` で Devkit 各機能を組み合わせる）:

```csharp
// Client/Assets/Scripts/Editor/Build/ApplicationBuilder.cs（抜粋）
public override async UniTask<bool> OnBeforeBuild()
{
    // SpriteAtlasのソーステクスチャ修正.
    FixSpriteAtlasSource.Modify(FixSpriteAtlasSource.DefaultTargetPlatforms);

    // ダミーテキスト削除.
    if (parameter.PublishType == PublishType.Production)
    {
        PrefabDummyTextCleaner.CleanAllPrefabContents();
    }

    // メモ削除.
    if (parameter.PublishType == PublishType.Production)
    {
        MemoComponentRemover.RemoveAllSceneComponents();
        MemoComponentRemover.RemoveAllPrefabComponents();
    }
    // ...
}
```

## 注意点・罠

- **namespace とフォルダ名の不一致が多い**。grep 時は注意:
  - `ApiMonitor/` → `Modules.Net.WebRequest`（ApiTracker は Network モジュール扱い）
  - `MasterGenerator/`・`MasterFileNameViewer/` → `Modules.Master`
  - `ScriptableObject/` → `Modules.Devkit.ScriptableObjects`（複数形）
  - `ShaderVariants/` → `Modules.Devkit.ShaderVariant`（単数形）
  - `Modules.Devkit.Log`（DebugLog）は `Extensions/Devkit/Log/` に、`Modules.Devkit.UI`（FixSpriteAtlasSource）は `Modules/UI/SpriteAtlas/Editor/` にあり、**Devkit フォルダ外**
- **UnityConsole は本番ビルドで無出力**。本番でも必ず残すべきエラーは `Debug.LogError` を直接使う（規約準拠）
- `UnityConsole.Event` の `LogType` は Log/Warning/Error のみ。`Exception` 等を渡すと `NotSupportedException`
- UnityConsole はデフォルトでスタックトレースを消す。トレースが要る時は直前に `UnityConsole.EnableNextLogStackTrace()`
- `UnityConsoleManager` / `ProjectPrefs` / `SingletonScriptableObject` は `#if UNITY_EDITOR` 内。ランタイムコードから触るとコンパイルエラー
- `SendReportManager.CaptureScreenShot()` はコルーチン（`WaitForEndOfFrame` 必須）。`Send()` の前に StartCoroutine で実行しておく
- `SRDiagnosis` / SRDebugger 系は `ENABLE_SRDEBUGGER` シンボル時のみ実体化（`SRDEBUGGER_DISABLE` で初期化スキップ）。`Diagnosis` の FPS/メモリ表示設定は `SecurePrefs` に永続化される
- `UnityLogTracker` は `RuntimeInitializeOnLoadMethod(BeforeSceneLoad)` で自動起動する（手動初期化不要）。保持は直近100件のみ
- `Diagnosis` は `SingletonMonoBehaviour`。prefab 生成は `InitializeObject.devkit.cs` が担う（新規で生成コードを書かない）
- `MasterViewer` の編集は `Application.isPlaying` 中のみ（`MasterController.CanEdit`）
- `AssetTuner` は**登録制**。新しい Tuner を作っても `AssetTunerRegister` に追加しないと動かない。バッチモード時は `AssetTuningPostprocessor` がスキップされる（`Application.isBatchMode`）
- `AdditionalComponent`（コンポーネント自動付与）はメニューからOFFにできるため、必須依存は `[RequireComponent]` を正とする
- Devkit のエディタ設定保存は `ProjectPrefs`（`EditorPrefs` + プロジェクト識別子）。他プロジェクトと衝突しないが、プロジェクトパス移動でリセットされる
- メニュー拡張時は Client側 `Dominion.Editor.EditorMenu`（`Modules.EditorMenu` 継承）または `ProductEditorMenu` に追加する（基盤側 EditorMenu.cs は直接編集しない）

## 関連

- [Master.md](Master.md) — MasterViewer / MasterGenerator / MasterFileNameViewer の詳細（namespace `Modules.Master`）
- [InputControl.md](InputControl.md) — BlockInputMonitorWindow（`Extension/Utility/...` メニュー配下）
- [Network.md](Network.md) — `ApiTracker`（namespace `Modules.Net.WebRequest`）は `UnityWebRequestManager` から自動記録される
- [ExternalAsset.md](ExternalAsset.md) — AssetBundleViewer / FindDependencyAssets / SimulationModeAssetFileTracker が扱う配信アセット本体
- [TextData.md](TextData.md) / [UI.md](UI.md) — DummyTextCleaner が除去する `TextSetter` / `DummyText` の定義元
- [Bugsnag.md](Bugsnag.md) — DebugLog / ApplicationLogHandler 経由のエラー転送先
- [Extensions/Core.md](../Extensions/Core.md) — `Singleton<T>` / `SingletonMonoBehaviour<T>` / `LifetimeDisposable`（Devkit 各クラスの基底）
