# Cache

> **namespace**: `Modules.Cache`
> **場所**: `Client/Assets/UniModules/Scripts/Modules/Cache/`
> **Client側使用**: 約32ファイル（2026-07時点）
> **依存**: R3（SpriteAtlasCache） / Extensions（`LifetimeDisposable`, `UnityUtility`, コレクション拡張）

## 概要

**文字列キー → オブジェクト** のメモリ上キャッシュ。ロード済みアセット（SpriteAtlas / PatternTexture 等）や生成済み Sprite を使い回し、二重ロード・二重生成を防ぐ。
キーは呼び出し側が決める（実例ではアセットのロードパスやスプライト名）。`referenceName` を指定すると**同名・同型の Cache インスタンス間でキャッシュ実体を共有**できる。
ディスクには何も書かない。永続化が必要なら本モジュールではなく下記を使う:

| 基盤 | 保存先 | 寿命 | 主な用途 |
|---|---|---|---|
| **Cache / SpriteAtlasCache（本モジュール）** | メモリ | Dispose / Clear まで（アプリ終了で消滅） | ロード済みアセット・生成済み Sprite の使い回し |
| [FileCache](FileCache.md)（`Modules.FileCache`） | ディスク（暗号化・有効期限付き） | 期限切れまで | ダウンロードしたファイルのキャッシュ |
| [LocalData](LocalData.md)（`Modules.LocalData`） | ディスク（暗号化セーブデータ） | 永続 | セーブデータ・ユーザー設定 |
| [Prefs](Prefs.md)（`Modules.Prefs`） | PlayerPrefs | 永続 | 軽量なキー値 |

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| ロードしたアセットをメモリにキャッシュしたい | `new Cache<T>()` + `Add(key, asset)` / `Get(key)` |
| 複数インスタンス・複数箇所で同じキャッシュを共有したい | `new Cache<T>("共有名")`（referenceName 指定） |
| SpriteAtlas から取り出した Sprite をキャッシュしたい | `new SpriteAtlasCache(atlas, referenceName)` + `GetSprite(name)` |
| キャッシュ済みか調べたい | `HasCache(key)` |
| 個別に消したい / 全部消したい | `Remove(key)` / `Clear()` |
| 使い終わったら破棄したい | `Dispose()`（共有モードでは必須） |

## 主要クラス

| クラス | 種別 | 役割 |
|---|---|---|
| `Cache<T>`（where T : class） | sealed class / `IDisposable` | 文字列キーの汎用メモリキャッシュ。referenceName による共有機能・スレッドセーフ |
| `SpriteAtlasCache` | sealed class / `LifetimeDisposable` 継承 | `SpriteAtlas.GetSprite()` の結果（Sprite）をスプライト名キーでキャッシュ。内部で `Cache<Sprite>` を使用 |

### referenceName（共有モード）の仕組み

- `new Cache<T>(referenceName)` で **同じ referenceName かつ同じ型引数 T** の Cache が static な実体（Dictionary）を共有する
- 参加者は `WeakReference` で管理され、`Dispose()` で離脱。**最後の生存参加者が Dispose した時に実体がクリア**される
- `Clear()` は自分が最終参照者の場合のみ実クリアし、共有相手が生存中は **no-op**（戻り値 bool で実クリアの有無を判定できる）
- referenceName 省略（null）時はインスタンスローカルなキャッシュになる

## 使い方(実例)

### 常駐アセットのキャッシュ（SpriteAtlasCache + 汎用 Cache）

```csharp
// 引用元: Client/Assets/Scripts/Client/Manager/CommonAssetManager.cs
private SpriteAtlasCache itemIconAtlasCache = null;

private Cache<PatternTexture> naviCharacterPatternTextureCache = null;

public async UniTask<Sprite> GetItemIconSprite(string spriteName)
{
    if (itemIconAtlasCache != null){ return itemIconAtlasCache.GetSprite(spriteName); }

    var itemIconAtlas = await ExternalAsset.LoadAsset<SpriteAtlas>("Contents/Item/Icon/ItemIconAtlas.spriteatlasv2");

    if (itemIconAtlasCache == null)
    {
        // referenceName は "クラス名 + 用途" で一意化するのが慣例.
        itemIconAtlasCache = new SpriteAtlasCache(itemIconAtlas, GetType().FullName + "-itemIconAtlasCache");
    }

    return itemIconAtlasCache.GetSprite(spriteName);
}

public async UniTask<PatternTexture> GetAssistantAsset(uint assistantId)
{
    var loadPath = $"Contents/Assistant/FullBody/{assistantId}/{assistantId}.asset";

    if (naviCharacterPatternTextureCache == null)
    {
        naviCharacterPatternTextureCache = new Cache<PatternTexture>();
    }

    // キー = ロードパス. キャッシュミス時のみロードして Add する定型パターン.
    var patternTexture = naviCharacterPatternTextureCache.Get(loadPath);

    if (patternTexture == null)
    {
        patternTexture = await ExternalAsset.LoadAsset<PatternTexture>(loadPath);

        naviCharacterPatternTextureCache.Add(loadPath, patternTexture);
    }

    return patternTexture;
}
```

### 複数インスタンス間で Sprite キャッシュを共有（サムネイル）

```csharp
// 引用元: Client/Assets/Scripts/Client/Module/Thumbnail/Components/ThumbnailContent.cs
protected const string ThumbnailAtlasCacheName = "ThumbnailAtlas-Cache";

// 引用元: Client/Assets/Scripts/Client/Module/Thumbnail/Contents/ItemThumbnail.cs
public override void Initialize(ThumbnailViewBase thumbnailView)
{
    // 全サムネイル(Item/Equipment/Character/Enemy...)が同じ referenceName を使い、Sprite 実体を共有.
    thumbnailSpriteCache = new SpriteAtlasCache(thumbnailAtlas, ThumbnailAtlasCacheName);
}

public void SetRarity(Rarity rarity)
{
    var frameSpriteName = FrameSpriteNameTable.GetValueOrDefault(rarity);

    var frameSprite = thumbnailSpriteCache.GetSprite(frameSpriteName);

    frameImage.sprite = frameSprite;
}
```

### アトラス自体のキャッシュ + Sprite キャッシュの併用

```csharp
// 引用元: Client/Assets/Scripts/Client/Module/ChipAnimation/ChipAnimation.cs
private Cache<SpriteAtlas> atlasCache = null;

private SpriteAtlasCache spriteAtlasCache = null;

public void Initialize()
{
    if (initialized) { return; }

    atlasCache = new Cache<SpriteAtlas>(GetType().FullName + "-atlasCache");

    initialized = true;
}

public async UniTask Load(string atlasLoadPath)
{
    var atlas = atlasCache.Get(atlasLoadPath);

    if (atlas == null)
    {
        atlas = await ExternalAsset.LoadAsset<SpriteAtlas>(atlasLoadPath);

        atlasCache.Add(atlasLoadPath, atlas);
    }

    if (spriteAtlasCache == null)
    {
        spriteAtlasCache = new SpriteAtlasCache(atlas, $"{GetType().FullName}-{atlasLoadPath}");
    }
}
```

## API(主要公開メンバー)

### Cache&lt;T&gt;（where T : class, IDisposable）

| メンバー | 説明 |
|---|---|
| `Cache(string referenceName = null)` | null: インスタンスローカル / 指定: 同名・同型 Cache と実体共有 |
| `void Add(string key, T asset)` | 登録。同キーは上書き。asset が null・key が空なら無視（例外にしない） |
| `T Get(string key)` | 取得。未登録なら null |
| `bool HasCache(string key)` | キーの存在確認 |
| `void Remove(string key)` | 個別削除 |
| `bool Clear()` | 全削除。共有モードでは最終参照者の時のみ実クリアし、実クリアの有無を返す |
| `void Dispose()` | 破棄。共有モードでは参加者から離脱し、最終参照者なら実体をクリア。多重 Dispose 安全 |
| `IReadOnlyList<string> Keys` / `IReadOnlyList<T> Values` | 現在のキー / 値のスナップショット |

公開メソッドはすべてスレッドセーフ。ファイナライザは持たないため**明示的 Dispose を推奨**（ソース内 remarks より）。

### SpriteAtlasCache（LifetimeDisposable 継承）

| メンバー | 説明 |
|---|---|
| `SpriteAtlasCache(SpriteAtlas spriteAtlas, string referenceName = null)` | 対象アトラスと共有名を指定して生成 |
| `Sprite GetSprite(string spriteName)` | キャッシュヒットならそれを返し、ミス時のみ `spriteAtlas.GetSprite()` して登録 |
| `void Clear()` | キャッシュクリア。**実クリアが成立した時のみ** Sprite を PostLateUpdate タイミングで `SafeDelete`（フレーム中の使用を壊さない遅延破棄） |
| `bool HasCache(string spriteName)` | キャッシュ済みか |
| `SpriteAtlas SpriteAtlas` | 元アトラス |
| `void Dispose()`（基底 `LifetimeDisposable`） | `Clear()` + 内部 `Cache<Sprite>` の Dispose |

## 注意点・罠

- **SpriteAtlas.GetSprite() は呼ぶたびに Sprite インスタンスを複製生成する（Unity仕様）**。アトラスから Sprite を取るコードを書くときは直接 GetSprite せず `SpriteAtlasCache` を使う（さもないと同名 Sprite が増殖しメモリを圧迫する）
- 共有の単位は「referenceName + 型引数 T」。`Cache<Sprite>` と `Cache<SpriteAtlas>` は同じ referenceName でも**別キャッシュ**
- referenceName は文字列でグローバル共有されるため衝突に注意。慣例は `GetType().FullName + "-用途名"`（CommonAssetManager）、意図的に共有する場合は共有定数（`ThumbnailContent.ThumbnailAtlasCacheName`）
- 共有モードで `Clear()` しても共有相手が生存中は消えない（no-op / false）。「消したはずなのに残っている」はこれ。確実に手放すには `Dispose()`
- `Cache<T>` は **Unity オブジェクトの破棄・アンロードは行わない**（参照を保持するだけ）。アセットの解放は呼び出し側の責務。例外的に `SpriteAtlasCache` のみ Clear 成立時に Sprite を `SafeDelete` する
- 破棄済みの UnityEngine.Object が値に残ることがある（Destroy してもキャッシュから消えない）。取得後の null / 破棄チェックは呼び出し側で行う
- `Add` の null asset は黙って無視されるため、「Add したのに Get で null」の原因になりうる

## 関連

- [FileCache](FileCache.md) — ディスク保存・有効期限付きのファイルキャッシュ（`SetCryptoKey` が必要）
- [LocalData](LocalData.md) — 永続セーブデータ
- [ExternalAsset](ExternalAsset.md) — アセットのロード元。ロード結果を本モジュールでキャッシュする組み合わせが定番
- [Extensions/Core.md](../Extensions/Core.md) — `LifetimeDisposable` / `UnityUtility.SafeDelete`
