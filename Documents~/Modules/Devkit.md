# Devkit

> **namespace**: `Modules.Devkit.*`（サブ機能ごとに分割。一部フォルダ名と不一致 → 「注意点・罠」参照）
> **場所**: `Client/Assets/UniModules/Scripts/Modules/Devkit/`（約200ファイル・42サブフォルダ）
> **依存**: UniTask / R3 / Newtonsoft.Json / Extensions（`Singleton<T>`, `LifetimeDisposable`, `SingletonEditorWindow<T>` 等）/ SRDebugger（ThirdParty, `ENABLE_SRDEBUGGER` 時）

## 概要

エディタ開発支援ツール群 + 実機デバッグ機能の集合モジュール。大部分は `Editor/` 配下のエディタ専用だが、**Console（開発ログ）・Diagnosis（実機デバッグUI/レポート送信）・LogHandler（ログ捕捉）・ApiMonitor（API履歴）はランタイムコードから使用する**。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| 開発時のみ出る情報ログを出したい | `UnityConsole.Info(message)`（リリースビルドでは自動無効） |
| 機能別に色分け・ON/OFF可能なログを出したい | `UnityConsole.Event(eventName, color, message, logType)` |
| ログのイベント別表示切替（エディタ） | メニュー `Extension/Settings/Open UnityConsoleConfigWindow` |
| Unityの全ログ（例外含む）を購読したい | `ApplicationLogHandler.Instance.OnReceivedAllAsObservable()` 等 |
| 実機でFPS/メモリ表示・デバッグパネルを出したい | `Diagnosis`（prefabを `Initialize()`。SRDebugger 連携） |
| 実機から不具合レポート（SS+ログ）を送信したい | `SendReportManager.Instance.Send(title)`（利用側でアップローダを実装） |
| SRDebugger のデバッグメニューに項目を足したい | `SROptions` の partial を利用側で追加 |
| API通信の履歴を見たい | `ApiTracker`（自動記録）+ メニュー `Extension/Prerelease/Open ApiMonitorWindow` |
| エディタ設定をプロジェクト単位で保存したい | `ProjectPrefs.GetBool/SetBool/...`（エディタ専用） |
| プロジェクト設定アセット（単一SO）を作りたい | `SingletonScriptableObject<T>` 継承（エディタ専用） |
| アセットインポート時の自動設定を追加したい | `AssetTuner` 継承 + `AssetTuneManager.Instance.Register<T>()` |
| Hierarchy 行に独自アイコン/トグルを描きたい | `ItemContentDrawer` 継承 + `HierarchyItemDrawer<T>.AddDrawer<T>()` |
| シーン保存/Prefab適用をフックしたい | `CurrentSceneSaveHook` / `PrefabApplyHook` / `PrefabModeEventHook`（EventHook） |
| アセットの参照元を探したい | Projectビュー右クリック `Assets/Search Asset References`（FindReferences） |
| アセットの依存先を探したい | 右クリック `Assets/Find Dependencies In Project`（AssetDependencies） |
| アプリをビルドしたい | `BuildManager.Build` + `ApplicationBuilder<TParam>` 派生を利用側で用意 |
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
| Log | `Modules.Devkit.Log`（実体は `Extensions/Devkit/Log/DebugLog.cs`） | `DebugLog`(static) | ログ文字列の中継ハブ（Receive→Observable）。外部通知等の橋渡し |
| ApiMonitor | **`Modules.Net.WebRequest`**（フォルダ名と不一致） | `ApiTracker`(Singleton), `ApiInfo` | API通信履歴の記録（直近100件）。`UnityWebRequestManager` が自動で `Start/OnComplete/OnRetry/OnError` を呼ぶ。閲覧は `ApiMonitorWindow`（エディタ） |
| Memo | `Modules.Devkit.Memo` | `Memo`(MonoBehaviour) | GameObjectにメモ文字列を残すだけのコンポーネント。`MemoComponentRemover`（エディタ）でビルド時全削除 |
| ChatWork | `Modules.Devkit.ChatWork` | `ChatWorkMessage` | ChatWork API へメッセージ/ファイル送信（`SendMessage`/`SendFile`）。CI通知用 |
| ExternalAsset | `Modules.Devkit.ExternalAssets` | `SimulationModeAssetFileTrackerBridge`（ランタイム）+ `SimulationModeAssetFileTracker`（エディタWindow） | シミュレートモードで読まれた配信アセットの追跡 |

### エディタ専用（`Editor/` 配下）

| サブ機能 | namespace | 主要クラス | 目的 / 入口 |
|---|---|---|---|
| AssetTuning | `Modules.Devkit.AssetTuning`(+`.TextureAsset`) | `AssetTuner`(abstract), `AssetTuneManager`(Singleton), `AssetTuningPostprocessor` | アセットインポート時の自動設定（Texture/Audio/Animator/SpriteAtlas/AssetBundle名等）。登録制: 利用側で `[InitializeOnLoadMethod]` により Tuner を登録 |
| Build | `Modules.Devkit.Build` | `BuildManager`(static), `ApplicationBuilder<TParam>`(abstract), `BuildParameter`, `BuiltInAssets`, `DeleteAssetSetting` | アプリビルドのフレームワーク。`BuildManager.Build(IApplicationBuilder)` が本体。利用側で `ApplicationBuilder<TParam>` を派生して実装 |
| Generators | `Modules.Devkit.Generators` | `ScenesScriptGenerator`, `TagsScriptGenerator`, `LayersScriptGenerator`, `SortingLayersScriptGenerator`, `ScenesInBuildGenerator`, `ScriptableObjectGenerator`, `ScriptGenerateUtility` | 定数スクリプト自動生成（`Constants/Scenes.cs` 等。テンプレの `#NAMESPACE#` を置換）と SO アセット生成。入口 `Extension/Generators/...` |
| Inspector | `Modules.Devkit.Inspector` | `DefaultAssetInspector`, `ExtendInspector`(abstract), `FolderInspector`, `RegisterScrollView<T>`(abstract), `TransformInspector`, `RectTransformSanitizer` | インスペクタ拡張基盤。`DefaultAssetInspector.AddExtendInspector<T>()` で登録。`RegisterScrollView<T>` は設定Window用の追加/削除可能リストUI部品 |
| EventHook | `Modules.Devkit.EventHook` | `HierarchyChangeNotification`, `CurrentSceneSaveHook`, `PrefabApplyHook`, `PrefabModeEventHook`, `AdditionalComponent` | エディタイベントのObservable化とコンポーネント自動付与。`AdditionalComponent.RegisterRequireComponents(...)` |
| Hierarchy | `Modules.Devkit.Hierarchy` | `HierarchyItemDrawer<T>`(Singleton), `ItemContentDrawer`(abstract), `ActiveToggleDrawer`, `ComponentIconDrawer`, `MissingComponentDrawer` | Hierarchy行の装飾（トグル/アイコン/Missing警告）。表示切替 `Extension/Settings/Hierarchy/...` |
| MasterViewer | `Modules.Devkit.MasterViewer` | `MasterViewerWindow<T>`, `MasterController`, `RecordWindow` | マスターデータ閲覧・実行中(`Application.isPlaying`)のみ編集可。入口 `Extension/Master/Open MasterViewer`（→ [Master.md](Master.md)） |
| MasterGenerator | **`Modules.Master`**（フォルダ名と不一致） | `MasterGenerator`, `MasterConfig`, `RecordDataLoader`, `MasterS3Uploader` | .record(YAML)→.master 生成とS3アップロード（→ [Master.md](Master.md)） |
| MasterFileNameViewer | **`Modules.Master`** | `MasterFileNameWindow<T>`(abstract) | マスター名⇔暗号化ファイル名の対応表示。入口 `Extension/Master/Open MasterFileNameViewer` |
| TextureViewer | `Modules.Devkit.TextureViewer` | `TextureViewerWindow`, `TextureInfo`, `TextureViewerConfig` | 全テクスチャのサイズ・圧縮設定を一覧表示。入口 `Extension/Utility/Open TextureViewerWindow` |
| ValidateAsset | `Modules.Devkit.ValidateAsset.TextureSize` / `.UnityWarning` | `ValidateTextureSize`, `TextureSizeValidateConfig`, `TextureSizeChatWorkNotify`, `UnityWarningChatWorkNotify` | テクスチャサイズ規約違反の検出とChatWork通知（CI連携） |
| VisualStudio | `Modules.Devkit.VisualStudio` | `VisualStudioFileCallback`(static), `ProjectFilesGenerator`, `SolutionFile`, `ProjectFile` | sln/csproj 生成フック（`ENABLE_VSTU`） |
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
| ClassAnalyzer | `Modules.Devkit.ClassAnalyzer` | `SealedClassAnalyzer`(static) | sealed 付与可能クラスの検索（`SearchTypes`） |
| JsonFile | `Modules.Devkit.JsonFile` | `JsonFileLoader`, `JsonFileLoaderPropertyDrawer` | JSONファイルの型付きロード（`Load<T>()`）+ PropertyDrawer |
| Spreadsheet | `Modules.Devkit.Spreadsheet` | `SpreadsheetConnector`, `SpreadsheetConfig`, `SpreadsheetConnectionWindow` | Google Spreadsheet OAuth接続・取得 |
| WebView | `Modules.Devkit.WebView` | `EditorWebViewWindow`(abstract) | エディタ内WebView（`OpenUrl`/`Back`/`Forward`/`Reload`） |
| Mac | `Modules.Devkit`（直下） | `LaunchUnityForMac` | Mac用Unity再起動メニュー（`Help/Launch Unity`） |
| CleanComponent(参考) | - | `Build/Editor/DeleteAsset/DeleteAssetSetting` | タグ指定でビルド時に削除するアセット定義 |

### カタログ表に無いメニュー入口

- `Extension/Utility/Open BuiltInAssetsWindow`（`BuiltInAssetsWindow`: ビルド内蔵アセット解析） / `Extension/Utility/SpriteAtlas/Fix SpriteAtlas Source`（`FixSpriteAtlasSource.Modify`） / `Extension/ExternalAsset/Open SimulationModeAssetFileTracker`
- `Extension/Utility/Clean/Open CleanDirectoryWindow`・`Clean DummyText (All Prefabs)`（`PrefabDummyTextCleaner.CleanAllPrefabContents()`）・`Clean ParticleSystem (Selection GameObject)`（`ParticleSystemCleaner.CleanSelectionTarget()`）
- `Extension/Settings/Auto Add Component/Disable・Log`（`AdditionalComponent.Prefs` トグル） / `Extension/Directory/Open PersistentDataPath` 等（各種ディレクトリをエクスプローラで開く） / `GameObject/Copy Hierarchy Path`（選択オブジェクトの階層パスをコピー）
- （右クリック）`Assets/Find Sprite References In Project`（`FindSpriteReferencesInProject`） / （コンポーネント右クリック）`CONTEXT/RectTransform/Sanitize Values`（`RectTransformSanitizer`: 浮動小数の丸め） / `Extension/Utility/Open BlockInputMonitorWindow`（InputControl モジュール → [InputControl.md](InputControl.md)）

## UnityConsole のランタイム挙動

`UnityConsole` は**リリースビルドで自動的に無効化される開発用ログ**。出力可否は `UnityConsole.Enable` が一元判定する: `UNITY_EDITOR` または `ENABLE_DEVKIT` 定義時は常に有効、それ以外（実機）は `Debug.isDebugBuild`（Developmentビルドのみ有効）。
つまり本番ビルドでは呼び出しが空振りになるため、`#if` で囲まずにログを書ける。加えてエディタでは `UnityConsoleConfigWindow` でイベント名単位の表示ON/OFFができ、`[Battle]` `[Initialize]` のような色付きプレフィックスでフィルタしやすい。デフォルトでスタックトレースを抑制する（`DisableStackTraceScope`）ためコンソールが汚れない。
非メインスレッドからも呼べる（`SynchronizationContext.Post` でメインへ）。機能専用ログクラスの定型は、イベント名+色を定義して `UnityConsole.Event` を呼ぶ形。

## 使い方

- **Diagnosis（実機デバッグ表示）**: `Diagnosis` prefab（FpsStats / MemoryStats / SRDiagnosis を内包）を利用側の起動処理で生成・`Initialize()` する。有効化条件は `ENABLE_DEVKIT || ((UNITY_EDITOR || DEVELOPMENT) && !PRODUCTION)` を利用側で定義。`SRDiagnosis.IgnoreWarnings / IgnoreErrors`（プレフィックス一致で無視）を設定できる
- **SendReport（不具合レポート送信）**: `SendReportManager.Instance.Initialize(uploader)`（必須・1回）→ `Send(reportTitle)` で SS+直近ログ（`UnityLogTracker`）+任意項目を送信。追加項目は `OnRequestReportAsObservable()` 購読内で `AddReportContent(key, value)`、`SetCryptKey` で値をAES暗号化。アップロード先は利用側で `IReportUploader` 実装
- **ログ購読**: `ApplicationLogHandler.Instance.OnReceivedAllAsObservable()` 等（種別別 / Threaded 版あり。Threaded 版は `ObserveOn(UnityFrameProvider.Update)` と併用が定石）。`DebugLog` は `Receive*` → Observable 配信の static ハブで、外部のクラッシュ通知SDK等へのエラー転送に使う
- **エディタ拡張の登録パターン**: 基盤が枠を提供し、利用側が `[InitializeOnLoadMethod]` で登録する構造が共通（Tuner / Hierarchy Drawer / Inspector 拡張 / VisualStudio フック等）。ビルド前処理で Devkit 各機能を組み合わせる形（例: `OnBeforeBuild` で `FixSpriteAtlasSource.Modify` / `PrefabDummyTextCleaner.CleanAllPrefabContents` / `MemoComponentRemover.RemoveAll*` を実行）

## 注意点・罠

- **namespace とフォルダ名の不一致が多い**。grep 時は注意:
  - `ApiMonitor/` → `Modules.Net.WebRequest`（ApiTracker は Network モジュール扱い）
  - `MasterGenerator/`・`MasterFileNameViewer/` → `Modules.Master`
  - `ScriptableObject/` → `Modules.Devkit.ScriptableObjects`（複数形）
  - `ShaderVariants/` → `Modules.Devkit.ShaderVariant`（単数形）
  - `Modules.Devkit.Log`（DebugLog）は `Extensions/Devkit/Log/` に、`Modules.Devkit.UI`（FixSpriteAtlasSource）は `Modules/UI/SpriteAtlas/Editor/` にあり、**Devkit フォルダ外**
- **UnityConsole は本番ビルドで無出力**。本番でも必ず残すべきエラーは `Debug.LogError` を直接使う
- `UnityConsole.Event` の `LogType` は Log/Warning/Error のみ。`Exception` 等を渡すと `NotSupportedException`
- UnityConsole はデフォルトでスタックトレースを消す。トレースが要る時は直前に `UnityConsole.EnableNextLogStackTrace()`
- `UnityConsoleManager` / `ProjectPrefs` / `SingletonScriptableObject` は `#if UNITY_EDITOR` 内。ランタイムコードから触るとコンパイルエラー
- `SendReportManager.CaptureScreenShot()` はコルーチン（`WaitForEndOfFrame` 必須）。`Send()` の前に StartCoroutine で実行しておく
- `SRDiagnosis` / SRDebugger 系は `ENABLE_SRDEBUGGER` シンボル時のみ実体化（`SRDEBUGGER_DISABLE` で初期化スキップ）。`Diagnosis` の FPS/メモリ表示設定は `SecurePrefs` に永続化される
- `UnityLogTracker` は `RuntimeInitializeOnLoadMethod(BeforeSceneLoad)` で自動起動する（手動初期化不要）。保持は直近100件のみ
- `Diagnosis` は `SingletonMonoBehaviour`。prefab 生成は利用側の初期化処理で1度だけ行う
- `MasterViewer` の編集は `Application.isPlaying` 中のみ（`MasterController.CanEdit`）
- `AssetTuner` は**登録制**。新しい Tuner を作っても登録処理を追加しないと動かない。バッチモード時は `AssetTuningPostprocessor` がスキップされる（`Application.isBatchMode`）
- `AdditionalComponent`（コンポーネント自動付与）はメニューからOFFにできるため、必須依存は `[RequireComponent]` を正とする
- Devkit のエディタ設定保存は `ProjectPrefs`（`EditorPrefs` + プロジェクト識別子）。他プロジェクトと衝突しないが、プロジェクトパス移動でリセットされる
- メニュー拡張は基盤の `Modules.EditorMenu` を継承した利用側クラスで追加する（基盤側 `EditorMenu.cs` は直接編集しない）。メニュー登録の実体: 基盤 `Client/Assets/UniModules/Scripts/Editor/EditorMenu.cs`

## 関連

- [Master.md](Master.md) — MasterViewer / MasterGenerator / MasterFileNameViewer の詳細（namespace `Modules.Master`）
- [InputControl.md](InputControl.md) — BlockInputMonitorWindow（`Extension/Utility/...` メニュー配下）
- [Network.md](Network.md) — `ApiTracker`（namespace `Modules.Net.WebRequest`）は `UnityWebRequestManager` から自動記録される
- [ExternalAsset.md](ExternalAsset.md) — AssetBundleViewer / FindDependencyAssets / SimulationModeAssetFileTracker が扱う配信アセット本体
- [TextData.md](TextData.md) / [UI.md](UI.md) — DummyTextCleaner が除去する `TextSetter` / `DummyText` の定義元
- [Bugsnag.md](Bugsnag.md) — DebugLog / ApplicationLogHandler 経由のエラー転送先の例
- [Extensions/Core.md](../Extensions/Core.md) — `Singleton<T>` / `SingletonMonoBehaviour<T>` / `LifetimeDisposable`（Devkit 各クラスの基底）
