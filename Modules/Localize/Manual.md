# Localize

言語ごとの表示を切り替える機能。
エディタで作業する言語を選べる。言語ごとに違う画像を Image に表示できる。
エディタの言語選択はエディタ専用で、実行時の言語切替は扱わない。

## 基本情報

> **namespace**: `Modules.Localize`（`Language/Editor/`・`Sprite/Editor/` 配下も同一 namespace）
> **場所**: `Modules/Localize/`（`LocalizeObject.cs` / `Language/`（エディタの言語選択）/ `Sprite/`（言語別スプライト））
> **依存**: R3 / UniTask / Extensions（Singleton, AesCryptoKey, SerializableDictionary, UnityUtility, PathUtility）/ Modules.Cache（SpriteAtlasCache）/ Modules.Scene（`LocalizeAtlasRequest` が namespace を using）/ Modules.Devkit.Prefs（Editor: ProjectPrefs）/ Modules.Devkit.ScriptableObjects（Editor: `LocalizeSpriteConfig` の基底）/ Modules.TextData.Editor（Editor: TextDataLoader）

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
    └─ TextData の Generate Window と自動更新は Current の言語でアセット生成（TextDataGenerator.Generate は渡された LanguageInfo の言語で生成する）
```

## 言語別スプライトの仕組み

```
[エディタ] LocalizeSpriteAsset（対象フォルダの一覧）→ LocalizeSpriteAssetBuilder.Build()
    └─ フォルダ guid → 暗号化したフォルダパス、スプライト guid → 所属フォルダ guid の表を書き込む（キーは LocalizeSpriteConfig）
[実行時] LocalizeAtlasManager.Initialize(cryptoKey, atlasLoader, localizeSpriteAsset)
    ├─ LoadAtlas(フォルダパス) → LocalizeAtlasLoader.Load で SpriteAtlas を取得してキャッシュ → OnLoadAtlasAsObservable 発火（読み込み済みなら参照数を加算するだけ）
    └─ LocalizeSpriteSetter が spriteGuid から所属フォルダのアトラスを引き、spriteName のスプライトを Image に設定
```

SpriteAtlas の読み込み方（`LocalizeAtlasLoader` の `GetAtlasPath` / `Load`）は利用側で実装する。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| エディタの表示言語を切り替えたい | `LanguageSelector.Open(typeof(<言語enum>))`（メニュー登録は利用側） |
| エディタの現在言語を取得/設定したい | `EditorLanguage.selection`（int。言語enumの値、未選択は -1） |
| コードから一時的に言語を切り替えたい | `EditorLanguage.selection` を退避 → 書換え → `finally` で復元 |
| 言語別スプライトを Image に表示したい | `LocalizeSpriteSetter`（Atlas 配信）/ `BuiltinLocalizeSpriteSetter`（アプリ同梱） |
| 言語別 SpriteAtlas をロード/解放したい | `LocalizeAtlasManager.LoadAtlas(atlasFolder, force = false)` / `ReleaseAtlas(atlasFolder)` / `ReleaseAll()` |
| 言語を切り替えた後、読み込み済みのアトラスを読み直したい | `LocalizeAtlasManager.ReloadAllAtlas()`（読み込み済みフォルダは `GetLoadedFolders()`） |
| アトラスの読み込みに反応したい | `LocalizeAtlasManager.OnLoadAtlasAsObservable()` / `LocalizeSpriteSetter.OnChangeAtlasAsObservable()` |
| シーンで使う言語別アトラスをまとめて要求/解放したい | `LocalizeAtlasRequest`（フォルダ guid の一覧を持つ。`RequestAtlas()` / `ReleaseAtlas()`、開いている全シーン分は static `ForceRequest()`） |
| スプライトとフォルダの対応表を作り直したい | `LocalizeSpriteAssetBuilder.Build()`（エディタ専用。全 `LocalizeSpriteAsset` を更新） |
| 特定言語のときだけ表示する GameObject を作りたい | `LocalizeObject<T>` 継承（`CurrentLanguage` を実装。`OnEnable` で言語が違えば非アクティブにする） |

## 使い方

- **言語選択ウィンドウを開く**（エディタメニュー定義）: 利用側で `LanguageSelector.Open(typeof(<言語enum>))` を呼ぶメニュー項目を用意
- **選択中の言語に依存する処理を全言語で回す**（定型パターン）: `EditorLanguage.selection` を退避 → 全言語ループで書換え → `finally` で復元
- **`selection` の参照**（基盤内の使用例）: `Modules/TextData/Editor/TextDataLanguage.cs`（`LanguageManager.Current` が selection から `LanguageInfo` を解決）/ `Modules/Lua.text/Editor/LuaTextLanguage.cs`
- **言語別スプライトの初期化**: `LocalizeAtlasManager.Instance.Initialize(cryptoKey, atlasLoader, localizeSpriteAsset)` を利用側の初期化フローから呼ぶ（基盤内に呼び出しは無い）
- **アプリ同梱の言語別スプライト**: `BuiltinLocalizeSpriteSetter` を継承して `LanguageType`（言語 enum の型）と `CurrentLanguage` を実装し、インスペクタで言語ごとの Sprite を登録する

## 注意点・罠

- **`EditorLanguage` はエディタ専用**（`#if UNITY_EDITOR`）: 実行時の言語切替は本モジュールでは扱わない
- **`selection` の直接書換えでは表示は更新されない**: `LanguageSelector` ウィンドウ経由なら `TextDataLoader.Reload()` が走るが、コードから代入した場合は走らない。生成処理では必ず「退避 → 切替 → finally で復元」パターンを使う
- **`selection` の既定は -1（未選択）**: このとき `LanguageManager.Current`（Modules.TextData.Editor）は null を返すため、TextData 生成前に `LanguageSelector` で言語を選択しておく必要がある
- **`LanguageSelector.Open` に渡す enum 型は ProjectPrefs 保存**: `Prefs.assembly` / `Prefs.enumType` に永続化され、OnGUI 毎に `Assembly.Load` で復元される
- **`LoadAtlas` と `ReleaseAtlas` は同じ回数を呼ぶ**: `LoadAtlas` は呼ぶたびに参照数を 1 加算し（読み込み済みなら加算だけ行う）、`ReleaseAtlas` は 1 減算して 0 になった時点でキャッシュから外す。`LoadAtlas(folder, force: true)` と `ReloadAllAtlas` は読み直すだけで、参照数は読み直す前の値を引き継ぐ。`LocalizeAtlasRequest` は要求済みかどうかを自身で持ち、`RequestAtlas` を重ねて呼んでも参照数は 1 回分しか加算しない（`ForceRequest` も同じ）。読み込みに失敗したフォルダは参照数を持たないので、同じフォルダをほかの要求元が読み込んでいる場合に `ReleaseAtlas` を呼ぶと、その要求元の参照を 1 つ減らす
- **`LocalizeAtlasManager` は `Initialize` が前提**: 未初期化で `LoadAtlas` / `ReleaseAtlas` / `GetLoadedFolders` を呼ぶと、キャッシュ辞書が null で NullReference になる。`GetSprite` / `GetSpriteAtlas` は `LocalizeSpriteAsset` 未設定なら null を返す
- **`LocalizeAtlasRequest.OnLoadScene` / `OnUnloadScene` は基盤内から呼ばれない**: シーンのロード/アンロード処理から利用側で呼ぶ
- **アトラス未読み込みのあいだ `LocalizeSpriteSetter` は Image の sprite を null にする**: `LoadAtlas` 完了の通知（`OnLoadAtlasAsObservable`）で設定し直す
- **Sprite 系コンポーネントは Awake / OnEnable ベースの基盤コード**（`LocalizeSpriteSetter` は Awake で購読、OnEnable で適用。`BuiltinLocalizeSpriteSetter` と `LocalizeObject<T>` は OnEnable で適用）

## 関連

- [TextData](../TextData/Manual.md) — `EditorLanguage` の主要な利用者（言語別アセット生成・`TextDataLoader.Reload`）
- [Cache](../Cache/Manual.md) — `AtlasCache` が `SpriteAtlasCache` を使用
- [Scene](../Scene/Manual.md) — シーン遷移基盤。シーンのロード/アンロードに合わせて `LocalizeAtlasRequest.OnLoadScene` / `OnUnloadScene` を呼ぶ
- [Extensions/Methods.md](../../Documents~/Extensions/Methods.md) — `AesCryptoKey`（フォルダパス暗号化）
- [Extensions/Core.md](../../Documents~/Extensions/Core.md) — `Singleton<T>` / `SerializableDictionary`
