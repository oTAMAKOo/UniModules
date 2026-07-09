# Utage

> **namespace**: `Modules.UtageExtension`（フォルダ名 `Utage/` と不一致に注意。Editor専用クラスも同namespace）
> **場所**: `Client/Assets/UniModules/Scripts/Modules/Utage/`
> **依存**: ADVエンジン「宴（Utage）」アセット（`Utage` namespace） / UniTask / Modules.ExternalAssets / Modules.Sound + CRI ADX2（Sound系のみ） / Modules.Animation / Modules.Particle / Modules.PatternTexture / Modules.Devkit.ScriptableObjects（Editor） / Extensions / Unity.Linq

## 概要

市販ADVエンジン「宴（Utage）」をプロジェクト基盤に統合するための拡張群。アセットロードの `ExternalAsset`（配信アセット）差し替え、サウンド再生のADX2差し替え、カスタムコマンド（エモーション表示等）・カスタム描画オブジェクトの追加、キャラ表示制御（グレーアウト/前面表示）、Excelシナリオの一括ビルド（Editor）を提供する。

全ファイルが `#if ENABLE_UTAGE` でガードされており、利用側で `ENABLE_UTAGE` シンボルが未定義の場合はコンパイル対象外になる。`SoundManagerSystem` / `ExternalAssetSoundAssetFile` はさらに `ENABLE_CRIWARE_ADX(_LE)` も必要（→ [CriWare](CriWare.md)）。

主要クラス:
- アセットロード差替: `ExtendAssetFileManager`（abstract。ロード解決を ローカルアセット→`ExternalAsset` の順に差し替え）+ `ExternalAssetAssetFile<T>` 派生（Text / Texture / UnityObject）+ `ExternalAssetSoundAssetFile`（**CRI必須**）
- サウンド差替: `ExtendSoundManager` / `SoundManagerSystem`（宴のBGM/Voice/SE再生を `Modules.Sound.SoundManagement`（ADX2）へ委譲。**CRI必須**）
- カスタムコマンド: `ExtendCustomCommandManager`（abstract）+ `AdvExtendCommandEmotion` / `AdvExtendCommandEmotionOff` / `AdvExtendCommandFinish`
- カスタム描画: `ExtendGraphicManager`（abstract。fileType→独自GraphicObject型解決）+ `EmotionGraphicObject` / `PatternGraphicObject`（`Modules.PatternTexture` 使用）
- キャラ表示制御: `ExtendCharacteGrayoutController`（クラス名typo）/ `ExtendCharacterOrderController`（会話中キャラを最前面化）
- Editor: `UtageBuildConfig` / `UtageBuildWindow`（対象フォルダ内の `.xls` から宴の `.project.asset` / `.scenario.asset` を一括生成・更新）

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| このモジュールを有効化したい | 宴アセット（有償）導入 + `ENABLE_UTAGE` 定義 + 各abstractクラスの派生実装（罠参照） |
| （宴有効時）宴のアセットロードを配信アセット化 | `ExtendAssetFileManager` 派生 + `ExternalAsset*AssetFile` |
| （宴有効時）宴のサウンドをADX2で鳴らす | `ExtendSoundManager.OnCreateSoundSystem` → `SoundManagerSystem`（CRI必須） |
| （宴有効時）独自シナリオコマンドを追加 | `ExtendCustomCommandManager` 派生の `CreateCustomCommand` でID→ `AdvCommand` 派生を生成 |
| （宴有効時）独自の描画オブジェクト型を追加 | `ExtendGraphicManager` 派生の `CustomTypeTable`（fileType→型） |
| （宴有効時）エモーション（感情演出）表示 | `AdvExtendCommandEmotion` / `AdvExtendCommandEmotionOff` + `EmotionGraphicObject` |
| （宴有効時）表情パターン差し替え表示 | `PatternGraphicObject`（`Modules.PatternTexture` 使用） |
| （宴有効時）会話中キャラを最前面表示 | `ExtendCharacterOrderController` |
| 【Editor・宴有効時】Excelシナリオを一括ビルド | `UtageBuildWindow.Open()`（設定は `UtageBuildConfig`） |

## 注意点・罠

- 有効化には (1) 宴アセット本体（有償、`Utage` namespace）の導入、(2) `ENABLE_UTAGE` 定義、(3) `ExtendAssetFileManager` / `ExtendCustomCommandManager` / `ExtendGraphicManager` 等のabstract派生実装とシーン構築が必要で、シンボル定義だけでは動かない。
- サウンド統合（`SoundManagerSystem` / `ExternalAssetSoundAssetFile`）はCRI ADX2前提。UnityAudio版サウンド構成（→ [Sound](Sound.md)）とは非互換で、この2クラスは `ENABLE_CRIWARE_ADX(_LE)` が無い限りコンパイルされない。`GetAudioSource` は常に非サポート。
- [Scenario](Scenario.md)（xLuaベース）とは**別系統**のシナリオ基盤。採用時はどちらか（または独自実装）の設計判断が必要。
- 初期化が `Awake` / `OnTransformChildrenChanged` 等のUnityライフサイクル前提。有効化・改修時は Setup 方式への整理を要相談。
- 宴のExcelコマンド列に独自の意味を割り当てている: `AdvExtendCommandEmotion` は Arg3=レイヤー / Arg6=フェード時間、`EmotionGraphicObject` は `SubFileName`=アニメーション名・Arg4/Arg5=表示座標。シナリオシート定義とC#実装がセットでないと動かない。
- `ExtendCharacteGrayoutController` はクラス名がtypo（`Characte`）のまま。grep検索時に注意。
- `PatternGraphicObject` は `Modules.PatternTexture`（→ [PatternTexture](PatternTexture.md)）の `PatternImage` / `PatternTexture` に依存。
- `Editor/` 配下（`UtageBuildConfig` / `UtageBuildWindow`）はエディタ専用。ランタイムコードから参照しないこと。

## 関連

- [Scenario](Scenario.md) — 別系統のシナリオ基盤（xLua）
- [CriWare](CriWare.md) — サウンド統合の前提
- [Sound](Sound.md) — サウンド基盤
- [ExternalAsset](ExternalAsset.md) — `ExternalAsset*AssetFile` が使用する配信アセット基盤
- [Animation](Animation.md) — `EmotionGraphicObject` が使用する `AnimationPlayer`
- [PatternTexture](PatternTexture.md) / [Particle](Particle.md)
