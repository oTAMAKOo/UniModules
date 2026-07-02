# ObjectPool

> **namespace**: `Modules.ObjectPool`
> **場所**: `Client/Assets/UniModules/Scripts/Modules/ObjectPool/`
> **Client側使用**: 約39ファイル（2026-07時点）
> **依存**: R3 / Extensions（`UnityUtility`）

## 概要

同一プレハブの Instantiate / Destroy 繰り返しを避けるための汎用 GameObject プール。
リストアイテム・戦闘エフェクト・ポップテキストなど「同じ見た目の要素を大量に出し入れする」場面で使う。
プールごとに非アクティブな親 `[Pooled]: {poolName}` を生成し、返却されたオブジェクトをその下に退避して使い回す方式。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| プールを作りたい | `new ObjectPool<T>(poolParent, prefab, poolName)` |
| プールから取得したい（空なら自動生成） | `pool.Get(parent)` |
| プールへ返却したい | `pool.Release(target)` |
| 事前生成（prewarm）して実行時の Instantiate を避けたい | `pool.Resize(count)` |
| プール内の待機オブジェクトを全破棄したい | `pool.Clear()` |
| 新規生成されたインスタンスに一度だけ初期化を挟みたい | `pool.OnCreateInstanceAsObservable()` |
| 取得 / 返却タイミングをフックしたい | `OnGetInstanceAsObservable()` / `OnReleaseInstanceAsObservable()` |
| プール内待機数を知りたい | `pool.Count`（貸出中は含まない） |

## 主要クラス

| クラス | 種別 | 役割 |
|---|---|---|
| `ObjectPool<T>`（where T : Component） | sealed class（通常クラス・非Singleton） | プール本体。本モジュールはこの1クラスのみ |

## 使い方(実例)

### ライフサイクル（生成 → 取得 → 返却 → 破棄）

1. **生成**: `new ObjectPool<T>(poolParent, prefab, poolName)` — `poolParent` の下に非アクティブな空 GameObject `[Pooled]: {poolName}` が作られ、これが返却オブジェクトの退避先になる
2. **取得**: `Get(parent)` — キューにあれば取り出し、無ければ `prefab` から新規 Instantiate（このとき `OnCreateInstanceAsObservable` 発火）。`parent` の下に付け替えて返す
3. **返却**: `Release(target)` — `[Pooled]` 親の下へ戻し（親が非アクティブなので画面から消える）、`transform.Reset()` で localPosition / localRotation / localScale を初期化してキューへ
4. **破棄**: `Clear()` でプール内待機オブジェクトを全 `DeleteGameObject`。プール親（`Instance`）自体が Destroy された場合はキューが自動 Clear される

### リストアイテムのプール（スクロールリストの行）

```csharp
// 引用元: Client/Assets/Scripts/Client/Scene/Citadel/Window/FacilityLevelWindow/FacilityLevelListView.cs
private ObjectPool<FacilityLevelListItemView> levelListItemPool = null;

public void Initialize()
{
    items = new List<FacilityLevelListItemView>();

    // Prefab クラス(Extensions)の場合は .Source で GameObject を渡す.
    levelListItemPool = new ObjectPool<FacilityLevelListItemView>(gameObject, levelListItemPrefab.Source, "ListItem");

    levelListItemPool.Resize(10);   // prewarm.
}

public async UniTask Setup(uint facilityId, uint? terrainId, int level)
{
    // 表示中アイテムを全返却してから再構築.
    items.ForEach(x => levelListItemPool.Release(x));
    items.Clear();

    foreach (var facilityLevelRecord in facilityLevelRecords)
    {
        var listItem = levelListItemPool.Get(levelListItemPrefab.Parent);
        // ... listItem.Set(...) して items.Add(listItem) ...
    }
}
```

### エフェクトのプール（戦闘ポップテキスト・スタイル別複数プール + 生成時初期化）

```csharp
// 引用元: Client/Assets/Scripts/Client/Scene/Battle/Vfx/PopText/Manager/PopTextEntryManager.cs
private Dictionary<PopTextStyle, ObjectPool<PopTextEntry>> pools = null;
private ObjectPool<PopTextGlyph> glyphPool = null;

public void Setup(SetupArgument argument)
{
    pools = new Dictionary<PopTextStyle, ObjectPool<PopTextEntry>>();

    foreach (var item in argument.stylePrefabs)
    {
        var pool = new ObjectPool<PopTextEntry>(argument.poolRoot, item.prefab.gameObject, item.style.ToString());

        // 新規 Instantiate されたインスタンスにだけ初期設定を反映.
        pool.OnCreateInstanceAsObservable()
            .Subscribe(entry => InitializeEntry(entry))
            .AddTo(disposable);

        pools[item.style] = pool;
    }

    // Glyph 用 Pool を構築し、prewarm (初期数まで事前生成して実行時の Instantiate を回避).
    glyphPool = new ObjectPool<PopTextGlyph>(argument.poolRoot, argument.glyphPrefab.gameObject, "PopTextGlyph");

    glyphPool.Resize(argument.initialGlyphPoolSize);
}

public PopTextEntry Get(PopTextStyle style)
{
    var pool = pools.GetValueOrDefault(style);

    var entry = pool.Get(popupRoot.gameObject);

    entry.ResetVisual();    // transform 以外の見た目は自前でリセット.

    return entry;
}

public void Return(PopTextStyle style, PopTextEntry entry)
{
    entry.OnRelease();      // 内部で抱えている子要素を先に返却.

    pools.GetValueOrDefault(style).Release(entry);
}
```

## API(主要公開メンバー)

### ObjectPool&lt;T&gt;（where T : Component）

| メンバー | 説明 |
|---|---|
| `ObjectPool(GameObject poolParent, GameObject prefab, string poolName)` | コンストラクタ。poolParent 直下に非アクティブなプール親 `[Pooled]: {poolName}` を生成 |
| `T Get(GameObject parent)` | 取得。プールが空なら prefab から新規生成。parent 直下へ付け替えて返す（parent は null 可、ただし罠参照） |
| `void Release(T target)` | 返却。プール親の下へ退避し `transform.Reset()`。null・破棄済み・返却済みは無視 |
| `void Resize(int count)` | 待機数を count に調整。不足分は生成して即返却（prewarm）、超過分は破棄 |
| `void Clear()` | 待機中オブジェクトを全て `DeleteGameObject` してキューを空にする |
| `GameObject Instance` | プール親 `[Pooled]: {poolName}` の GameObject |
| `IEnumerable<T> Objects` | 待機中オブジェクトの列挙（貸出中は含まない） |
| `int Count` | 待機数（貸出中は含まない） |
| `Observable<T> OnCreateInstanceAsObservable()` | 新規 Instantiate 時に発火（Get / Resize 起因。取得のたびではない） |
| `Observable<T> OnGetInstanceAsObservable()` | Get のたびに発火 |
| `Observable<T> OnReleaseInstanceAsObservable()` | Release 成立時に発火 |

## 注意点・罠

- **貸出中オブジェクトはプール管理外**。`Count` / `Objects` / `Clear()` の対象は待機中のみ。`Release` を忘れたオブジェクトはプールに戻らない（Get 側が新規 Instantiate し続ける）
- **`Release` で初期化されるのは transform のみ**（localPosition / localRotation / localScale）。表示状態・アニメーション・テキスト等は初期化されないため、Get 直後に自前リセットする（実例: `PopTextEntry.ResetVisual()`）か、`OnCreateInstanceAsObservable` + Get 後の Set で毎回上書きする
- `Get(null)` は非アクティブなプール親の下に置いたまま返す（画面に出ない）。表示するなら必ず親を渡す
- `Release` の二重返却チェックは `Queue.Contains`（線形走査）。数千件規模のプールで毎フレーム返却する場合はコストに注意
- プール親（`Instance`）が Destroy されるとキューは自動 Clear される（`OnDestroyAsObservable` 購読済み）。待機オブジェクトも親ごと破棄されるため、画面破棄時に個別の後始末は不要
- `IDisposable` ではない。明示的に破棄したい場合は `Clear()` を呼ぶ。再 Setup 時も既存プールを `Clear()` してから作り直す（実例: `PopTextEntryManager.Setup`）
- prefab 引数は `GameObject`。Extensions の `Prefab` クラスを使う場合は `.Source` を渡す

## 関連

- [Extensions/Core.md](../Extensions/Core.md) — `UnityUtility`（Instantiate / SetParent / DeleteGameObject）、`Prefab` クラス
- [UI](UI.md) — スクロールリスト等、プールと組み合わせる UI 部品
