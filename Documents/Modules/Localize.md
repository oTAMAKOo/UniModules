# Localize

> **namespace**: `Modules.Localize`（`Language/Editor/`・`Sprite/Editor/` 配下も同一 namespace）
> **場所**: `Client/Assets/UniModules/Scripts/Modules/Localize/`
> **Client側使用**: 4ファイル（2026-07時点、**すべてエディタコード**）
> **依存**: R3 / UniTask / Extensions（Singleton, AesCryptoKey, SerializableDictionary, UnityUtility）/ Modules.Cache（SpriteAtlasCache）/ Modules.Devkit.Prefs（Editor: ProjectPrefs）/ Modules.TextData.Editor（Editor: TextDataLoader）

## 概要

多言語対応基盤。**(A) エディタの言語選択**（`EditorLanguage` / `LanguageSelector`）と **(B) 言語別スプライト切替**（`Sprite/` 配下 + `LocalizeObject`）の2系統からなる。
Dominion で実際に使われているのは (A) のみで、TextData の言語別アセット生成・全言語ビルドの言語スイッチとして機能する。
**実行時のゲーム内言語切替は Client 側 `LangageManager`（`Dominion.Client`、`Client/Assets/Scripts/Client/Core/LangageManager.cs`）が担い、本モジュールは関与しない**（`EditorLanguage` は `#if UNITY_EDITOR` 限定）。

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

## 主要クラス

| クラス | 種別 | 役割 |
|---|---|---|
| `EditorLanguage` | static・**エディタ専用**（`#if UNITY_EDITOR`） | 言語選択状態の唯一の保持者。`selection`（int）を `ProjectPrefs` に永続化（既定 -1） |
| `LanguageSelector` | SingletonEditorWindow・**エディタ専用** | 言語選択 Popup ウィンドウ。言語 enum 型を受け取り、変更時 `EditorLanguage.selection` 更新 + `TextDataLoader.Reload()` |
| `LocalizeObject<T>` | abstract MonoBehaviour（T: Enum） | シリアライズされた言語と `CurrentLanguage` が不一致なら OnEnable で自身を非アクティブ化 |
| `LocalizeAtlasManager` | Singleton | 言語別 SpriteAtlas のロード・キャッシュ・解放。`LocalizeAtlasLoader` 注入式。`OnLoadAtlasAsObservable` で更新通知 |
| `LocalizeAtlasLoader` | abstract | Atlas の実ロード処理（`GetAtlasPath` / `Load`）をプロジェクト側で実装して注入 |
| `AtlasCache` | class | SpriteAtlas + `SpriteAtlasCache`（Modules.Cache）+ 参照カウント |
| `LocalizeSpriteAsset` | ScriptableObject | folderGuid→AES暗号化フォルダパス / spriteGuid→folderGuid の辞書（エディタツールが生成） |
| `LocalizeSpriteSetter` | MonoBehaviour（`[RequireComponent(typeof(Image))]`） | spriteGuid/spriteName から `Image.sprite` を設定。Atlas ロード通知で自動再設定 |
| `BuiltinLocalizeSpriteSetter` | abstract MonoBehaviour | 言語→Sprite 辞書（アプリ同梱）を OnEnable で適用。`LanguageType` / `CurrentLanguage` を実装して使う |
| `LocalizeAtlasRequest` | MonoBehaviour（`[DisallowMultipleComponent]`） | シーンルートに配置し、folderGuids の Atlas をシーンロード時ロード/アンロード時解放 |
| `LocalizeSpriteConfig` | SingletonScriptableObject・**エディタ専用** | Sprite 系のフォルダパス暗号キー（key/iv）設定アセット |
| `LocalizeSpriteAssetBuilder` / `LocalizeSpriteAssetUpdater` | static・**エディタ専用** | 全 `LocalizeSpriteAsset` の辞書再構築（フォルダパスを暗号化して格納） |
| `LocalizeSpriteAssetInspector` / `LocalizeSpriteSetterInspector` / `LocalizeAtlasRequestInspector` / `BuiltinLocalizeSpriteSetterInspector` | CustomEditor・**エディタ専用** | 各コンポーネントのインスペクタ（Sprite の D&D 設定等） |

## 使い方(実例)

### 1. 言語選択ウィンドウを開く（エディタメニュー）

```csharp
// Client/Assets/Scripts/Editor/EditorMenu.cs
[MenuItem(itemName: LocalizeMenu  + "Open LanguageSelector Window", priority = 0)]
public static void OpenLanguageSelectorWindow()
{
    var enumType = typeof(Constants.Language);

    LanguageSelector.Open(enumType);
}
```

### 2. 全言語ループで TextData を生成（selection の一時切替）

```csharp
// Client/Assets/Scripts/Editor/TextData/GenerateAllLanguage.cs
var originLanguage = EditorLanguage.selection;

try
{
    TextDataInitializer.SetLanguageInfo();

    var allLanguage = Enum.GetValues(typeof(Language)).Cast<Language>();

    foreach (var language in allLanguage)
    {
        EditorLanguage.selection = (int)language;

        TextDataGenerator.Generate(textType, languageManager.Current, true);
    }
}
finally
{
    EditorLanguage.selection = originLanguage;
}
```

### 3. Jenkins リソースビルドでの退避・復元

```csharp
// Client/Assets/Scripts/Editor/Jenkins/JenkinsResource.cs
var originLanguage = EditorLanguage.selection;

try
{
    // ... GenerateAllLanguage.Generate() や外部アセットビルド ...
}
finally
{
    EditorLanguage.selection = originLanguage;
}
```

### 4. TextData 側からの参照（基盤内の使用例）

```csharp
// Client/Assets/UniModules/Scripts/Modules/TextData/Editor/TextDataLanguage.cs
public LanguageInfo Current
{
    get
    {
        if (languageInfos == null){ return null; }

        var selection = EditorLanguage.selection;

        return languageInfos.FirstOrDefault(x => Convert.ToInt32(x.Language) == selection);
    }
}
```

### 5. 実行時の言語切替（参考: 本モジュール外）

```csharp
// Client/Assets/Scripts/Client/Core/LangageManager.cs (Dominion.Client)
public void SetLanguage(Language language)
{
    var playData = LocalDataManager.Get<PlayData>();

    current = language;

    playData.Language = current;

    playData.Save();

    if (onChangeLanguage != null)
    {
        onChangeLanguage.OnNext(current);
    }
}
```

## API(主要公開メンバー)

### EditorLanguage（static・エディタ専用）

| メンバー | 説明 |
|---|---|
| `static int selection { get; set; }` | 言語選択インデックス。`ProjectPrefs` 永続化・既定 -1（未選択）。値は `(int)Constants.Language` |

### LanguageSelector（SingletonEditorWindow・エディタ専用）

| メンバー | 説明 |
|---|---|
| `static void Open(Type enumType)` | 言語 enum 型を指定してウィンドウを開く。型情報（Assembly/FullName）は ProjectPrefs に保存され次回以降復元される |
| `const string WindowTitle` | `"Language"` |
| （挙動） | Popup 変更時に `EditorLanguage.selection` 更新 → `TextDataLoader.Reload()` |

### LocalizeAtlasManager（Singleton: `LocalizeAtlasManager.Instance`）

| メンバー | 説明 |
|---|---|
| `void Initialize(AesCryptoKey cryptoKey, LocalizeAtlasLoader atlasLoader, LocalizeSpriteAsset localizeSpriteObject)` | 初期化。**Sprite 系を使う場合は必須**（Dominion では未実施） |
| `void SetAtlasLoader(LocalizeAtlasLoader)` / `void SetLocalizeSpriteObject(LocalizeSpriteAsset)` | ローダー / 辞書アセットの差し替え |
| `UniTask LoadAtlas(string atlasFolder, bool force = false)` | Atlas ロード + キャッシュ。既ロードなら何もしない（force で再ロード） |
| `void ReleaseAtlas(string atlasFolder)` | 参照カウントを減らし 0 以下でキャッシュ破棄 |
| `UniTask ReloadAllAtlas()` | ロード済み全 Atlas を force 再ロード（言語切替後の差し替え用） |
| `void ReleaseAll()` | 全キャッシュ破棄 |
| `SpriteAtlas GetSpriteAtlas(string spriteGuid)` / `Sprite GetSprite(string spriteGuid, string spriteName)` | spriteGuid から Atlas / Sprite を解決（未ロードなら null） |
| `string GetFolderPathFromGuid(string folderGuid)` / `string GetAtlasFolderPath(string spriteGuid)` | 辞書アセット経由のパス解決（復号あり） |
| `IReadOnlyList<string> GetLoadedFolders()` | ロード済みフォルダ一覧 |
| `Observable<Unit> OnLoadAtlasAsObservable()` | Atlas ロード完了通知（R3） |

### LocalizeAtlasRequest（MonoBehaviour）

| メンバー | 説明 |
|---|---|
| `IReadOnlyList<string> FolderGuids` | 要求する Atlas フォルダの guid 一覧（インスペクタ設定） |
| `UniTask OnLoadScene()` / `UniTask OnUnloadScene()` | シーンロード/アンロードフック（LoadAtlas / ReleaseAtlas を実行） |
| `UniTask RequestAtlas()` / `void ReleaseAtlas()` | 手動ロード / 解放 |
| `static UniTask ForceRequest()` | 全ロード済みシーンのルートから LocalizeAtlasRequest を探して再要求 |

### LocalizeSpriteSetter（MonoBehaviour）

| メンバー | 説明 |
|---|---|
| `SpriteAtlas Atlas { get; }` | 設定中 spriteGuid の所属 Atlas |
| `Observable<Unit> OnChangeAtlasAsObservable()` | Atlas 差し替えで sprite が再設定された通知（R3） |
| （挙動） | Awake で `OnLoadAtlasAsObservable` 購読、OnEnable で sprite 適用。spriteGuid/spriteName はインスペクタ（専用 Inspector で Sprite を D&D）設定 |

### LocalizeSpriteAsset（ScriptableObject）

| メンバー | 説明 |
|---|---|
| `IReadOnlyList<FolderInfo> Infos` | 対象フォルダ（guid + description）一覧 |
| `void SetCryptoKey(AesCryptoKey)` | フォルダパス復号キー設定（`LocalizeAtlasManager.SetLocalizeSpriteObject` が呼ぶ） |
| `string GetFolderPath(string folderGuid)` / `string GetAtlasFolderPath(string spriteGuid)` | 復号済みフォルダパス解決（キャッシュあり） |
| `string GetGetAtlasFolderGuid(string spriteGuid)` | spriteGuid → folderGuid（メソッド名の Get 重複は実コード通り） |

### BuiltinLocalizeSpriteSetter（abstract MonoBehaviour）

| メンバー | 説明 |
|---|---|
| `abstract Type LanguageType { get; }` / `protected abstract Enum CurrentLanguage { get; }` | 継承先で言語 enum 型と現在言語を返す |
| （挙動） | OnEnable で `SpriteDictionary`（言語→Sprite、インスペクタ設定）から現在言語の Sprite を Image に適用 |

### エディタ専用ツール

| メンバー | 説明 |
|---|---|
| `LocalizeSpriteConfig.Instance.GetCryptoKey()` | 設定アセットの key/iv から `AesCryptoKey` 生成 |
| `LocalizeSpriteAssetBuilder.Build()` | 全 `LocalizeSpriteAsset` を検索し辞書再構築 + 保存 |
| `LocalizeSpriteAssetUpdater.SetFolderInfo(target, cryptoKey)` / `SetSpriteFolderInfo(target)` | フォルダパス暗号化辞書 / spriteGuid 辞書の設定（private フィールドへ Reflection 書込） |

## 注意点・罠

- **Sprite 系（`Sprite/` 配下）と `LocalizeObject` は Dominion 未使用**: Client 側から参照ゼロで、`LocalizeAtlasManager.Initialize` の呼び出しも存在しない。使う場合は `LocalizeAtlasLoader` の実装・`LocalizeSpriteAsset` の作成・Initialize 呼び出しが前置きで必要
- **`EditorLanguage` はエディタ専用**（`#if UNITY_EDITOR`）: 実行時の言語は Client 側 `LangageManager`（クラス名のスペルは **Langage**）が別管理。混同しない
- **`selection` の直接書換えでは表示は更新されない**: `LanguageSelector` ウィンドウ経由なら `TextDataLoader.Reload()` が走るが、コードから代入した場合は走らない。生成処理では必ず「退避 → 切替 → finally で復元」パターンを使う（実例 2・3）
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
