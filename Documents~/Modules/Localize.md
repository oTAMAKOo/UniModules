# Localize

> **namespace**: `Modules.Localize`（`Language/Editor/`・`Sprite/Editor/` 配下も同一 namespace）
> **場所**: `Client/Assets/UniModules/Scripts/Modules/Localize/`
> **Client側使用**: 4ファイル（2026-07時点、**すべてエディタコード**）
> **依存**: R3 / UniTask / Extensions（Singleton, AesCryptoKey, SerializableDictionary, UnityUtility）/ Modules.Cache（SpriteAtlasCache）/ Modules.Devkit.Prefs（Editor: ProjectPrefs）/ Modules.TextData.Editor（Editor: TextDataLoader）

## 概要

多言語対応基盤。**(A) エディタの言語選択**（`EditorLanguage` / `LanguageSelector`）と **(B) 言語別スプライト切替**（`Sprite/` 配下 + `LocalizeObject`）の2系統からなる。
Dominion で実際に使われているのは (A) のみで、TextData の言語別アセット生成・全言語ビルドの言語スイッチとして機能する。
**実行時のゲーム内言語切替は Client 側 `LangageManager`（`Dominion.Client`、`Client/Assets/Scripts/Client/Core/LangageManager.cs`）が担い、本モジュールは関与しない**（`EditorLanguage` は `#if UNITY_EDITOR` 限定）。
主要クラス: (A) `EditorLanguage`（static・エディタ専用。言語選択状態の唯一の保持者、`selection` を ProjectPrefs 永続化）/ `LanguageSelector`（言語選択 Popup ウィンドウ）。(B) `LocalizeAtlasManager`・`LocalizeAtlasLoader`・`LocalizeSpriteAsset`・`LocalizeSpriteSetter`・`BuiltinLocalizeSpriteSetter`・`LocalizeAtlasRequest`・`LocalizeObject<T>` ほかエディタツール群（言語別スプライト系、**Dominion 未使用**）。

## 言語切替の仕組み（TextData との関係）

```
[エディタ]
EditorLanguage.selection (ProjectPrefs 保存の int。Constants.Language の値)
    ├─ LanguageSelector ウィンドウで変更 → TextDataLoader.Reload()（TextDataアセット再読込 + シーン上 TextSetter 再適用）
    ├─ Modules.TextData.Editor.LanguageManager.Current が selection から LanguageInfo を解決
    │    （Client の TextDataInitializer が [InitializeOnLoadMethod] で jp/en/ko/zh-TW/zh-CN を登録。ScriptGenerate は Japanese のみ true）
    └─ TextDataGenerator は Current の言語でアセット生成 → 全言語生成は selection をループで書換え（GenerateAllLanguage / JenkinsResource）

[実行時]
LangageManager (Dominion.Client) が PlayData(LocalData) に言語を保存し、TextData-{identifier}.asset をロードし直す
    → 本モジュールのクラスは一切使われない
```

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| エディタの表示言語を切り替えたい | メニュー `Extension/Localize/Open LanguageSelector Window`（実体: `LanguageSelector.Open(typeof(Constants.Language))`） |
| エディタの現在言語を取得/設定したい | `EditorLanguage.selection`（int。`(int)Constants.Language` の値、未選択は -1） |
| 全言語分の TextData アセットを生成したい | メニュー `Dominion/TextData/Generate All Language`（内部で `EditorLanguage.selection` を全言語ループ） |
| コードから一時的に言語を切り替えたい | `EditorLanguage.selection` を退避 → 書換え → `finally` で復元（`GenerateAllLanguage` / `JenkinsResource` パターン） |
| 実行時にゲーム言語を切り替えたい | Client側 `LangageManager.SetLanguage()` / `LoadTextData()`（**本モジュールではない**） |
| 言語別スプライトを Image に表示したい | `LocalizeSpriteSetter`（Atlas 配信）/ `BuiltinLocalizeSpriteSetter`（アプリ同梱）— **Dominion 未使用** |
| 言語別 SpriteAtlas をロード/解放したい | `LocalizeAtlasManager.LoadAtlas` / `ReleaseAtlas` — **Dominion 未使用** |
| 特定言語のときだけ表示する GameObject を作りたい | `LocalizeObject<T>` 継承 — **Dominion 未使用** |

## 使い方

定型パターンと参照先:

- **言語選択ウィンドウを開く**（エディタメニュー定義）: `Client/Assets/Scripts/Editor/EditorMenu.cs`（`LanguageSelector.Open(typeof(Constants.Language))`）
- **全言語ループで TextData を生成**（`selection` の退避 → 全言語ループで書換え → `finally` で復元）: `Client/Assets/Scripts/Editor/TextData/GenerateAllLanguage.cs`
- **Jenkins リソースビルドでの退避・復元**（同パターン）: `Client/Assets/Scripts/Editor/Jenkins/JenkinsResource.cs`
- **TextData 側からの `selection` 参照**（基盤内の使用例）: `Client/Assets/UniModules/Scripts/Modules/TextData/Editor/TextDataLanguage.cs`（`LanguageManager.Current` が selection から `LanguageInfo` を解決）
- **実行時の言語切替**（参考・本モジュール外）: `Client/Assets/Scripts/Client/Core/LangageManager.cs`（`SetLanguage` が `PlayData`（LocalData）に言語を保存して通知）

## 注意点・罠

- **Sprite 系（`Sprite/` 配下）と `LocalizeObject` は Dominion 未使用**: Client 側から参照ゼロで、`LocalizeAtlasManager.Initialize` の呼び出しも存在しない。使う場合は `LocalizeAtlasLoader` の実装・`LocalizeSpriteAsset` の作成・Initialize 呼び出しが前置きで必要
- **`EditorLanguage` はエディタ専用**（`#if UNITY_EDITOR`）: 実行時の言語は Client 側 `LangageManager`（クラス名のスペルは **Langage**）が別管理。混同しない
- **`selection` の直接書換えでは表示は更新されない**: `LanguageSelector` ウィンドウ経由なら `TextDataLoader.Reload()` が走るが、コードから代入した場合は走らない。生成処理では必ず「退避 → 切替 → finally で復元」パターンを使う（`GenerateAllLanguage.cs` / `JenkinsResource.cs`）
- **`selection` の既定は -1（未選択）**: このとき `LanguageManager.Current`（Modules.TextData.Editor）は null を返すため、TextData 生成前に `LanguageSelector` で言語を選択しておく必要がある
- **`LanguageSelector.Open` に渡す enum 型は ProjectPrefs 保存**: `Prefs.assembly` / `Prefs.enumType` に永続化され、OnGUI 毎に `Assembly.Load` で復元される。プロジェクトの言語 enum は `Constants.Language`（Japanese / English / Korean / TraditionalChinese / SimplifiedChinese）
- **`LocalizeAtlasManager` の参照カウントは Release 側のみ**: `LoadAtlas` を複数回呼んでも `AddReference` は自動で呼ばれない（キャッシュ済みなら何もしない）。複数オーナーで同一フォルダを確保・解放する設計にする場合はカウント整合に注意
- **Sprite 系コンポーネントは Awake / OnEnable ベースの基盤コード**: プロジェクトの「Unity ライフサイクルメソッド禁止」ルールは新規 Client コードに対するもので、この基盤の既存動作はそのまま（`LocalizeSpriteSetter` は Awake で購読、OnEnable で適用）

## 関連

- [TextData](TextData.md) — `EditorLanguage` の主要な利用者（言語別アセット生成・`TextDataLoader.Reload`）
- [Cache](Cache.md) — `AtlasCache` が `SpriteAtlasCache` を使用
- [Scene](Scene.md) — `LocalizeAtlasRequest` のシーンロード/アンロードフック
- [../Extensions/Methods.md](../Extensions/Methods.md) — `AesCryptoKey`（フォルダパス暗号化）
- [../Extensions/Core.md](../Extensions/Core.md) — `Singleton<T>` / `SerializableDictionary`
