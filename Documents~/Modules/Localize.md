# Localize

> **namespace**: `Modules.Localize`（`Language/Editor/`・`Sprite/Editor/` 配下も同一 namespace）
> **場所**: `Client/Assets/UniModules/Scripts/Modules/Localize/`
> **依存**: R3 / UniTask / Extensions（Singleton, AesCryptoKey, SerializableDictionary, UnityUtility）/ Modules.Cache（SpriteAtlasCache）/ Modules.Devkit.Prefs（Editor: ProjectPrefs）/ Modules.TextData.Editor（Editor: TextDataLoader）

## 概要

多言語対応基盤。**(A) エディタの言語選択**（`EditorLanguage` / `LanguageSelector`）と **(B) 言語別スプライト切替**（`Sprite/` 配下 + `LocalizeObject`）の2系統からなる。
(A) は TextData の言語別アセット生成・全言語ビルドの言語スイッチとして機能する（`#if UNITY_EDITOR` 限定）。実行時のゲーム内言語切替は利用側で別途仕組みを用意する。
主要クラス: (A) `EditorLanguage`（static・エディタ専用。言語選択状態の唯一の保持者、`selection` を ProjectPrefs 永続化）/ `LanguageSelector`（言語選択 Popup ウィンドウ）。(B) `LocalizeAtlasManager`・`LocalizeAtlasLoader`・`LocalizeSpriteAsset`・`LocalizeSpriteSetter`・`BuiltinLocalizeSpriteSetter`・`LocalizeAtlasRequest`・`LocalizeObject<T>` ほかエディタツール群（言語別スプライト系）。

## 言語切替の仕組み（TextData との関係）

```
[エディタ]
EditorLanguage.selection (ProjectPrefs 保存の int。利用側言語 enum の値)
    ├─ LanguageSelector ウィンドウで変更 → TextDataLoader.Reload()（TextDataアセット再読込 + シーン上 TextSetter 再適用）
    ├─ Modules.TextData.Editor.LanguageManager.Current が selection から LanguageInfo を解決
    └─ TextDataGenerator は Current の言語でアセット生成 → 全言語生成は selection をループで書換え
```

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| エディタの表示言語を切り替えたい | メニュー `Extension/Localize/Open LanguageSelector Window`（実体: `LanguageSelector.Open(typeof(<言語enum>))`） |
| エディタの現在言語を取得/設定したい | `EditorLanguage.selection`（int。言語enumの値、未選択は -1） |
| コードから一時的に言語を切り替えたい | `EditorLanguage.selection` を退避 → 書換え → `finally` で復元 |
| 言語別スプライトを Image に表示したい | `LocalizeSpriteSetter`（Atlas 配信）/ `BuiltinLocalizeSpriteSetter`（アプリ同梱） |
| 言語別 SpriteAtlas をロード/解放したい | `LocalizeAtlasManager.LoadAtlas` / `ReleaseAtlas` |
| 特定言語のときだけ表示する GameObject を作りたい | `LocalizeObject<T>` 継承 |

## 使い方

- **言語選択ウィンドウを開く**（エディタメニュー定義）: 利用側で `LanguageSelector.Open(typeof(<言語enum>))` を呼ぶメニュー項目を用意
- **全言語ループで TextData を生成**（定型パターン）: `EditorLanguage.selection` を退避 → 全言語ループで書換え → `finally` で復元
- **TextData 側からの `selection` 参照**（基盤内の使用例）: `Client/Assets/UniModules/Scripts/Modules/TextData/Editor/TextDataLanguage.cs`（`LanguageManager.Current` が selection から `LanguageInfo` を解決）

## 注意点・罠

- **`EditorLanguage` はエディタ専用**（`#if UNITY_EDITOR`）: 実行時の言語切替は本モジュールでは扱わない
- **`selection` の直接書換えでは表示は更新されない**: `LanguageSelector` ウィンドウ経由なら `TextDataLoader.Reload()` が走るが、コードから代入した場合は走らない。生成処理では必ず「退避 → 切替 → finally で復元」パターンを使う
- **`selection` の既定は -1（未選択）**: このとき `LanguageManager.Current`（Modules.TextData.Editor）は null を返すため、TextData 生成前に `LanguageSelector` で言語を選択しておく必要がある
- **`LanguageSelector.Open` に渡す enum 型は ProjectPrefs 保存**: `Prefs.assembly` / `Prefs.enumType` に永続化され、OnGUI 毎に `Assembly.Load` で復元される
- **`LocalizeAtlasManager` の参照カウントは Release 側のみ**: `LoadAtlas` を複数回呼んでも `AddReference` は自動で呼ばれない（キャッシュ済みなら何もしない）。複数オーナーで同一フォルダを確保・解放する設計にする場合はカウント整合に注意
- **Sprite 系コンポーネントは Awake / OnEnable ベースの基盤コード**（`LocalizeSpriteSetter` は Awake で購読、OnEnable で適用）

## 関連

- [TextData](TextData.md) — `EditorLanguage` の主要な利用者（言語別アセット生成・`TextDataLoader.Reload`）
- [Cache](Cache.md) — `AtlasCache` が `SpriteAtlasCache` を使用
- [Scene](Scene.md) — `LocalizeAtlasRequest` のシーンロード/アンロードフック
- [../Extensions/Methods.md](../Extensions/Methods.md) — `AesCryptoKey`（フォルダパス暗号化）
- [../Extensions/Core.md](../Extensions/Core.md) — `Singleton<T>` / `SerializableDictionary`
