# Utage

> **namespace**: `Modules.UtageExtension`（フォルダ名 `Utage/` と不一致に注意。Editor専用クラスも同namespace）
> **場所**: `Client/Assets/UniModules/Scripts/Modules/Utage/`
> **Client側使用**: 0ファイル（using 0、2026-07時点）
> **依存**: ADVエンジン「宴（Utage）」アセット（`Utage` namespace、**未導入**） / UniTask / Modules.ExternalAssets / Modules.Sound + CRI ADX2（Sound系のみ） / Modules.Animation / Modules.Particle / Modules.PatternTexture / Modules.Devkit.ScriptableObjects（Editor） / Extensions / Unity.Linq

## 概要

市販ADVエンジン「宴（Utage）」をプロジェクト基盤に統合するための拡張群。アセットロードの `ExternalAsset`（配信アセット）差し替え、サウンド再生のADX2差し替え、カスタムコマンド（エモーション表示等）・カスタム描画オブジェクトの追加、キャラ表示制御（グレーアウト/前面表示）、Excelシナリオの一括ビルド（Editor）を提供する。

**本プロジェクトでは未使用（コンパイル対象外）**。全ファイルが `#if ENABLE_UTAGE` でガードされており、`ENABLE_UTAGE` は Scripting Define Symbols（`Client/ProjectSettings/ProjectSettings.asset`）にも `Client/Assets/csc.rsp` にも未定義。宴アセット本体（`Utage` namespace）も Assets に存在しない。`SoundManagerSystem` / `ExternalAssetSoundAssetFile` はさらに `ENABLE_CRIWARE_ADX(_LE)` も必要（→ [CriWare](CriWare.md)、こちらも未定義）で二重に無効。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| **ADVパート・会話劇を実装したい（本プロジェクト）** | このモジュールは**使えない**（宴未導入・コンパイル対象外）。既存のClient側実装を探すかユーザーに設計相談すること |
| このモジュールを有効化したい | 宴アセット（有償）導入 + `ENABLE_UTAGE` 定義 + 各abstractクラスの派生実装（罠参照） |
| （宴有効時）宴のアセットロードを配信アセット化 | `ExtendAssetFileManager` 派生 + `ExternalAsset*AssetFile` |
| （宴有効時）宴のサウンドをADX2で鳴らす | `ExtendSoundManager.OnCreateSoundSystem` → `SoundManagerSystem`（CRI必須） |
| （宴有効時）独自シナリオコマンドを追加 | `ExtendCustomCommandManager` 派生の `CreateCustomCommand` でID→ `AdvCommand` 派生を生成 |
| （宴有効時）独自の描画オブジェクト型を追加 | `ExtendGraphicManager` 派生の `CustomTypeTable`（fileType→型） |
| （宴有効時）エモーション（感情演出）表示 | `AdvExtendCommandEmotion` / `AdvExtendCommandEmotionOff` + `EmotionGraphicObject` |
| （宴有効時）表情パターン差し替え表示 | `PatternGraphicObject`（`Modules.PatternTexture` 使用） |
| （宴有効時）会話中キャラを最前面表示 | `ExtendCharacterOrderController` |
| 【Editor・宴有効時】Excelシナリオを一括ビルド | `UtageBuildWindow.Open()`（設定は `UtageBuildConfig`） |

## 主要クラス

全クラスが `#if ENABLE_UTAGE` 内のため**本プロジェクトでは一切コンパイルされない**。

| クラス | 種別 | 役割 |
|---|---|---|
| `ExtendAssetFileManager` | abstract MonoBehaviour | 宴の `AssetFileManager.GetCustomLoadManager().OnFindAsset` にフックし、ロード解決を ローカルアセット→`ExternalAsset` の順に差し替え。派生で `GetInternalResourcesAssetFile` / `GetExternalAssetAssetFile` を実装 |
| `ExternalAssetAssetFile<T>` | abstract class（宴 `AssetFileBase` 派生） | `ExternalAsset.UpdateAsset` → `LoadAsset<T>` を宴のコルーチンロードにブリッジ。派生: `ExternalAssetTextAssetFile`（TextAsset）/ `ExternalAssetTextureAssetFile`（Texture2D）/ `ExternalAssetUnityObjectAssetFile`（Object） |
| `ExternalAssetSoundAssetFile` | sealed class（`AssetFileBase` 派生、**CRI必須**） | サウンドをファイル実体ではなく `CueInfo`（ADX2キュー情報）として解決 |
| `ExtendSoundManager` | sealed MonoBehaviour | 宴 `SoundManager.System` を `SoundManagerSystem` に差し替えるコールバック（`OnCreateSoundSystem`） |
| `SoundManagerSystem` | sealed class（宴 `SoundManagerSystemInterface` 実装、**CRI必須**） | 宴のBGM/Ambience/Voice/SE再生を `Modules.Sound.SoundManagement`（ADX2）へ委譲。`GetAudioSource` は非サポート（LogError） |
| `ExtendCustomCommandManager` | abstract class（宴 `AdvCustomCommandManager` 派生） | `AdvCommandParser.OnCreateCustomCommandFromID` にフック。派生の `CreateCustomCommand` でID→独自コマンド生成 |
| `AdvExtendCommandEmotion` | sealed class（宴 `AdvCommand` 派生） | エモーション（感情アイコン等のプレハブ演出）表示コマンド。Arg3=表示レイヤー / Arg6=フェード時間 |
| `AdvExtendCommandEmotionOff` | sealed class（`AdvCommand` 派生） | エモーション非表示。Arg1=対象名（空なら全消し） |
| `AdvExtendCommandFinish` | sealed class（`AdvCommand` 派生） | `IsWait = true` で進行を止める終端コマンド（外部から `IsWait = false` にして終了同期する用途） |
| `ExtendGraphicManager` | abstract MonoBehaviour | `AdvGraphicInfo.CallbackCreateCustom` にフックし fileType→独自GraphicObject型を解決（`CustomTypeTable` abstract）。Canvas を ScreenSpaceCamera 化・子Canvasの `overrideSorting` 強制 |
| `EmotionGraphicObject` | sealed class（宴 `AdvGraphicObjectPrefabBase` 派生） | プレハブ+`AnimationPlayer` によるエモーション描画。シート `SubFileName` 列=アニメ名、Arg4/Arg5=ローカル座標 |
| `PatternGraphicObject` | sealed class（宴 `AdvGraphicObjectUguiBase` 派生） | `Modules.PatternTexture` の `PatternImage` による表情パターン差し替え+クロスフェード。`ChangePattern(string)` |
| `ExtendCharacteGrayoutController` | MonoBehaviour | 宴 `AdvCharacterGrayOutController` の補助。表示キャラ無しページでは全キャラをグレーアウト対象外にする（クラス名は `Characte` のまま、typo） |
| `ExtendCharacterOrderController` | sealed MonoBehaviour | 会話中キャラ/ページ内新規キャラの `Canvas.sortingOrder` を最前面+1に変更し、ページ切替で復元。対象は `TargetMask` フラグ（Talking / NewCharacerInPage / NoChanageIfTextOnly）で制御 |

### Editor/（エディタ専用）

| クラス | 種別 | 役割 |
|---|---|---|
| `UtageBuildConfig` | SingletonScriptableObject | シナリオビルド設定（対象フォルダ / `AdvScenarioDataProject` テンプレ / `AdvImportScenarios` テンプレ） |
| `UtageBuildWindow` | SingletonEditorWindow | 対象フォルダ内の `.xls` を走査し、宴の `.project.asset` / `.scenario.asset` を一括生成・更新（Excel最終更新日時で差分判定）。`Open()` で表示 |

## 使い方(実例)

Client側・基盤内ともに使用例が存在しないため、**実コードのシグネチャから構成した最小の想定例**（動作確認不可。宴導入 + `ENABLE_UTAGE` 有効化が前提）。

### 想定例1: アセットロードの ExternalAsset 差し替え

```csharp
// 想定例（実在コードではない）. 実シグネチャは
// Client/Assets/UniModules/Scripts/Modules/Utage/ExtendAssetFileManager.cs 参照.
public sealed class AdvAssetFileManager : ExtendAssetFileManager
{
    protected override AssetFileBase GetInternalResourcesAssetFile(AssetFileManager mangager, AssetFileInfo fileInfo, IAssetFileSettingData settingData)
    {
        // SetLocalAssets で渡したアプリ内蔵アセットから解決.
        return new ExternalAssetUnityObjectAssetFile(mangager, fileInfo, settingData);
    }

    protected override AssetFileBase GetExternalAssetAssetFile(AssetFileManager mangager, AssetFileInfo fileInfo, IAssetFileSettingData settingData)
    {
        switch (fileInfo.FileType)
        {
            case AssetFileType.Text:
                return new ExternalAssetTextAssetFile(mangager, fileInfo, settingData);

            case AssetFileType.Texture:
                return new ExternalAssetTextureAssetFile(mangager, fileInfo, settingData);
        }

        return new ExternalAssetUnityObjectAssetFile(mangager, fileInfo, settingData);
    }
}
```

### 想定例2: カスタムコマンドとカスタム描画オブジェクトの登録

```csharp
// 想定例（実在コードではない）. フック機構は ExtendCustomCommandManager.cs / ExtendGraphicManager.cs 参照.
public sealed class AdvCommandManager : ExtendCustomCommandManager
{
    public override void CreateCustomCommand(string id, StringGridRow row, AdvSettingDataManager dataManager, ref AdvCommand command)
    {
        switch (id)
        {
            case "Emotion":
                command = new AdvExtendCommandEmotion(row, dataManager);
                break;

            case "EmotionOff":
                command = new AdvExtendCommandEmotionOff(row);
                break;
        }
    }
}

public sealed class AdvGraphicManager : ExtendGraphicManager
{
    protected override Dictionary<string, Type> CustomTypeTable
    {
        get
        {
            return new Dictionary<string, Type>()
            {
                { "Emotion", typeof(EmotionGraphicObject) },
                { "Pattern", typeof(PatternGraphicObject) },
            };
        }
    }
}
```

## API(主要公開メンバー)

### ExtendAssetFileManager / ExternalAssetAssetFile<T>

| メンバー | 説明 |
|---|---|
| `ExtendAssetFileManager.SetLocalAssets(Dictionary<string, Object>)` | アプリ内蔵（非配信）アセットの辞書を設定。設定時は配信より優先解決 |
| `ExtendAssetFileManager.GetInternalResourcesAssetFile(...)` / `GetExternalAssetAssetFile(...)` | **abstract**。内蔵/配信それぞれの `AssetFileBase` 生成 |
| `ExternalAssetAssetFile<T>.Asset : T` | ロード結果 |
| `ExternalAssetAssetFile<T>.CheckCacheOrLocal()` / `LoadAsync(onComplete, onFailed)` / `Unload()` | 宴のロードインターフェース実装（内部で `ExternalAsset.UpdateAsset` → `LoadAsset<T>`） |
| `ExternalAssetSoundAssetFile.CueInfo : CueInfo` | 解決済みADX2キュー情報（CRI必須） |

### サウンド / コマンド / 表示制御

| メンバー | 説明 |
|---|---|
| `ExtendSoundManager.OnCreateSoundSystem(SoundManager)` | `soundManager.System = new SoundManagerSystem()` に差し替え（宴のSoundManager生成イベントに接続する想定） |
| `SoundManagerSystem.Play(groupName, label, soundData, fadeInTime, fadeOutTime)` | groupName（IdBgm/IdAmbience/IdVoice/IdSe）→ `SoundType` に変換して `SoundManagement.Play` |
| `ExtendCustomCommandManager.OnBootInit()` | `AdvCommandParser.OnCreateCustomCommandFromID` へのフック登録（宴が呼ぶ） |
| `ExtendCustomCommandManager.CreateCustomCommand(id, row, dataManager, ref command)` | **abstract**。コマンドID→独自 `AdvCommand` 生成 |
| `AdvExtendCommandFinish.IsWait : bool` | true の間シナリオ進行を停止（`Wait` override が参照） |
| `ExtendGraphicManager.CustomTypeTable : Dictionary<string, Type>` | **abstract**。fileType文字列→独自GraphicObject型 |
| `PatternGraphicObject.ChangePattern(string pattern)` | 表示中パターン（表情）を文字列指定で変更 |
| `ExtendCharacterOrderController.Mask : TargetMask` | 最前面化の対象条件（Flags） |

### Editor

| メンバー | 説明 |
|---|---|
| `UtageBuildConfig.TargetFolderAssetPath` / `ScenarioProjectTemplate` / `ImportScenarioTemplate` | ビルド対象フォルダのアセットパス / 各テンプレート |
| `UtageBuildWindow.Open()` | ビルドウィンドウ表示（static） |

## 注意点・罠

- **本プロジェクトでは未使用（コンパイル対象外）**。ADV/会話劇の実装をこのモジュール前提で書かないこと。有効化には (1) 宴アセット本体（有償、`Utage` namespace）の導入、(2) `ENABLE_UTAGE` 定義、(3) `ExtendAssetFileManager` / `ExtendCustomCommandManager` / `ExtendGraphicManager` 等のabstract派生実装とシーン構築が必要で、シンボル定義だけでは動かない。
- サウンド統合（`SoundManagerSystem` / `ExternalAssetSoundAssetFile`）はCRI ADX2前提。本プロジェクトのUnityAudio版サウンド構成（→ [Sound](Sound.md)）とは非互換で、有効化してもこの2クラスは `ENABLE_CRIWARE_ADX(_LE)` が無い限りコンパイルされない。`GetAudioSource` は常に非サポート。
- [Scenario](Scenario.md)（xLuaベース）とは**別系統**のシナリオ基盤。両方とも本プロジェクトでは無効なので、採用時はどちらか（または独自実装）の設計判断が必要。
- 初期化が `Awake` / `OnTransformChildrenChanged` 等のUnityライフサイクル前提（プロジェクトの「ライフサイクルメソッド原則禁止」規約より前の設計）。有効化・改修時は Setup 方式への整理を要相談。
- 宴のExcelコマンド列に独自の意味を割り当てている: `AdvExtendCommandEmotion` は Arg3=レイヤー / Arg6=フェード時間、`EmotionGraphicObject` は `SubFileName`=アニメーション名・Arg4/Arg5=表示座標。シナリオシート定義とC#実装がセットでないと動かない。
- `ExtendCharacteGrayoutController` はクラス名がtypo（`Characte`）のまま。grep検索時に注意。
- `PatternGraphicObject` は `Modules.PatternTexture`（ドキュメント未作成）の `PatternImage` / `PatternTexture` に依存。
- `Editor/` 配下（`UtageBuildConfig` / `UtageBuildWindow`）はエディタ専用。ランタイムコードから参照しないこと。

## 関連

- [Scenario](Scenario.md) — 別系統のシナリオ基盤（xLua。同じく未使用）
- [CriWare](CriWare.md) — サウンド統合の前提（未導入）
- [Sound](Sound.md) — 本プロジェクトの実サウンド基盤
- [ExternalAsset](ExternalAsset.md) — `ExternalAsset*AssetFile` が使用する配信アセット基盤
- [Animation](Animation.md) — `EmotionGraphicObject` が使用する `AnimationPlayer`
- PatternTexture / Particle（各ドキュメント未作成）
