# Extensions Core（基盤クラス群）

> **namespace**: `Extensions`（Serialize 配下のみ `Extensions.Serialize`、一部 Editor 用 Drawer は `Extensions.Devkit`）
> **場所**: `Client/Assets/UniModules/Scripts/Extensions/`（`Methods/`・`Devkit/` を除く: `Behaviour/` `Attribute/` `Component/` `Serialize/` `SafeValue/` `Scope/` `Types/` `Utility/`）
> **Client側使用**: `using Extensions;` 865ファイル（Methods.md と namespace 共通のため合算）。内訳目安: Singleton系継承 64 / `Prefab` フィールド 48 / `.AddTo(Disposable)` 61 / `UnityUtility` 参照 230（2026-07時点）
> **依存**: R3（LifetimeDisposable / FixedQueue / UnityUtility）、UniTask（AndroidUtility / MessagePackFileUtility）、Unity.Mathematics（MathematicsRandomUtility）、MessagePack（MessagePackFileUtility）

## 概要

プロジェクト全体の土台となる基盤クラス群。特に **Singleton パターン（`Singleton<T>` / `SingletonMonoBehaviour<T>`）は全マネージャークラスの基底**であり、
R3 購読の寿命管理（`LifetimeDisposable`）、View 実装で頻出のプレハブ参照（`Prefab`）、インスペクタ属性、Unity でシリアライズ可能な Nullable 型、
GameObject 操作の安全ラッパー（`UnityUtility`）を提供する。新規マネージャー・View 実装前に必ず本ドキュメントを確認すること。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| 非MonoBehaviourのマネージャーを作りたい | `Singleton<T>` 継承（`Instance` / `CreateInstance()` / `ReleaseInstance()`） |
| シーン上のコンポーネントをシングルトン化したい | `SingletonMonoBehaviour<T>` 継承 |
| 全シングルトンを一括解放したい | `SingletonManager.ReleaseAll()` |
| R3購読をオブジェクト寿命で自動解除したい | `LifetimeDisposable` 継承 + `.AddTo(Disposable)` |
| インスペクタでプレハブ+生成先親を設定して生成したい | `Prefab` フィールド + `prefab.Instantiate<T>()` |
| GameObject を安全に生成/削除/アクティブ切替したい | `UnityUtility.Instantiate` / `SafeDelete` / `SetActive` |
| コンポーネントが無ければ追加して取得したい | `UnityUtility.GetOrAddComponent<T>(gameObject)` |
| UnityObject の破棄済み判定をしたい | `UnityUtility.IsNull(obj)` |
| インスペクタで編集不可（表示のみ）にしたい | `[ReadOnly]`（`[SerializeField, ReadOnly]`） |
| enum をビットフラグとしてインスペクタ表示したい | `[EnumFlags]` |
| インスペクタ入力値を選択肢に制限したい | `[IntSelectable(1,2,4)]` / `[FloatSelectable]` / `[StringSelectable]` |
| enum 値に表示名を付けて取得したい | `[Label("表示名")]` + `.ToLabelName()`（拡張は Methods 側） |
| null許容の int/float 等を Unity シリアライズしたい | `IntNullable` / `FloatNullable` 等（`Extensions.Serialize`） |
| Dictionary を Unity シリアライズしたい | `SerializableDictionary<TKey, TValue>` を継承した具象型 |
| メモリ改ざん（チート）対策の数値を持ちたい | `XInt` / `XLong` / `XFloat` / `XBool` 等（SafeValue） |
| using で後処理を保証したい（スコープ処理を自作したい） | `Scope` 継承（`CloseScope()` 実装） |
| ログのスタックトレースを一時無効化したい | `using (new DisableStackTraceScope())` |
| 処理時間を計測したい | `using (new StopwatchScope(ms => ...))` |
| 乱数・重み付き抽選・シャッフルしたい | `RandomUtility` / `MathematicsRandomUtility`（戦闘系は後者） |
| スコアに応じたバイアス付き抽選をしたい | `BiasSelectUtility.SelectOne` |
| 固定長キュー（あふれ通知付き）が欲しい | `FixedQueue<T>` |
| 文字列を自然順（数値考慮）でソートしたい | `NaturalComparer` |
| 外部プロセスを実行したい | `ProcessExecute` |
| パス区切りを統一して結合したい | `PathUtility.Combine` / `ConvertPathSeparator` |
| Assets パス⇔フルパスを変換したい | `UnityPathUtility.ConvertAssetPathToFullPath` 等 |
| ファイルの SHA256 / CRC32 を取りたい | `FileUtility.GetHash` / `GetCRC` |
| private フィールド/メソッドにリフレクションでアクセスしたい | `Reflection.GetPrivateField` / `InvokePrivateMethod` |
| MessagePack でファイル読み書きしたい（AES対応） | `MessagePackFileUtility.Read/Write(Async)` |
| 起動引数を取得したい | `CommandLineUtility.Get<T>("label")` |

## 主要クラス

### Behaviour/（Singleton 基盤）

| クラス | 種別 | 役割 |
|---|---|---|
| `Singleton<T>` | abstract class | 非MonoBehaviourシングルトン基底。`LifetimeDisposable` + `ISingleton` 実装。Client の各種 Manager の基底 |
| `SingletonMonoBehaviour<T>` | abstract MonoBehaviour | シーン上コンポーネントのシングルトン基底。`[ExecuteAlways]` 付き |
| `SingletonManager` | static | `Singleton<T>` 全インスタンスの登録簿。一括 Refresh / ReleaseAll |
| `ISingleton` | interface | `Refresh()` / `Release()` を持つシングルトン共通IF |
| `LifetimeDisposable` | class (IDisposable) | R3 `CompositeDisposable` を保持し、Dispose で購読を一括解除 |

### Component/

| クラス | 種別 | 役割 |
|---|---|---|
| `Prefab` | [Serializable] class | プレハブ参照+生成先親をセットで持つフィールド用クラス。View実装で頻出 |
| `PrefabPropertyDrawer` | PropertyDrawer（**エディタ専用**、`Extensions.Devkit`） | インスペクタで Prefab/Parent の2段表示 |

### Attribute/（インスペクタ属性）

| クラス | 種別 | 役割 |
|---|---|---|
| `ReadOnlyAttribute` | PropertyAttribute | `[ReadOnly]` でインスペクタ編集不可（DisabledGroup表示） |
| `EnumFlagsAttribute` | PropertyAttribute | `[EnumFlags]` で enum を MaskField（ビットフラグ）表示 |
| `IntSelectableAttribute` / `FloatSelectableAttribute` / `StringSelectableAttribute` | PropertyAttribute | 入力値を指定候補の Popup に制限 |
| `LabelAttribute` | System.Attribute（**PropertyAttributeではない**） | enum 値に表示名を付与。`.ToLabelName()`（`Methods/EnumExtensions.cs`）で取得 |
| `LabelAttributeUtility` | static | `ToLabelName` の実体（型別キャッシュ付き） |
| 各 `*PropertyDrawer` | **エディタ専用** | 上記属性の描画実装（`Attribute/*/Editor/`） |

### Serialize/（namespace `Extensions.Serialize`: Unityでシリアライズ可能にするための型）

| クラス | 種別 | 役割 |
|---|---|---|
| `SerializableNullable<T>` | [Serializable][DataContract] abstract class | `T?` 相当を `hasValue`+`value` でシリアライズ可能にする基底 |
| `IntNullable` / `UIntNullable` / `LongNullable` / `FloatNullable` / `DoubleNullable` / `DecimalNullable` | sealed class | 各プリミティブの Nullable。元の `int?` 等と暗黙相互変換可 |
| `SerializableDictionary<TKey, TValue>` | [Serializable] abstract class | `Dictionary` 継承 + `ISerializationCallbackReceiver` で keys/values リストに分解してシリアライズ |
| `SerializableNullablePropertyDrawer` ほか型別 Drawer | **エディタ専用** | hasValue チェック + 値フィールドの1行表示 |

### SafeValue/（メモリ改ざん対策型）

| クラス | 種別 | 役割 |
|---|---|---|
| `SafeValue` | static | XOR による Pack/UnPack（シードはプロセス起動毎にランダム） |
| `XInt` / `XUInt` / `XLong` / `XULong` / `XShort` / `XUShort` / `XSByte` / `XByte` / `XChar` / `XBool` / `XFloat` / `XDouble` / `XString` | [Serializable] struct | メモリ上でXOR難読化された値型。演算子オーバーロード完備、元型へ implicit 変換 |

### Scope/（using による後処理保証）

| クラス | 種別 | 役割 |
|---|---|---|
| `Scope` | abstract class (IDisposable) | スコープ処理基底。`CloseScope()` を Dispose 時に1度だけ実行 |
| `DisableStackTraceScope` | sealed Scope | スコープ内だけログのスタックトレースを無効化 |
| `StopwatchScope` | sealed Scope | スコープ終了時に経過ミリ秒をコールバック |
| `GizmosColorScope` | sealed Scope | `Gizmos.color` を退避・復元 |
| `LockReloadAssembliesScope` | sealed Scope（**エディタ専用**） | アセンブリリロードを一時ロック |
| `DiisplayProgressScope` | sealed Scope（**エディタ専用**、typo名のまま） | プログレスバー表示（batchMode では非表示） |

### Types/（汎用型）

| クラス | 種別 | 役割 |
|---|---|---|
| `FixedQueue<T>` | sealed class : Queue\<T\> | 固定長キュー（デフォルト4096）。あふれた要素は `OnExtrudedAsObservable()` で通知 |
| `NaturalComparer` | sealed Comparer\<string\> | 文字列自然順ソート（`OrderBy(x => x, new NaturalComparer())`） |
| `ProcessExecute` | sealed class | 外部プロセス実行。`Start()` / `StartAsync()` → `Result { ExitCode, Output, Error }` |
| `BezierCurve` | sealed class | 制御点配列から `Evaluate(time)` でベジェ曲線上の点を取得 |
| `CRC16` / `CRC32` | sealed HashAlgorithm | CRC ハッシュ実装（`FileUtility.GetCRC` が使用） |
| `Encode` | static | `GetEncode(byte[])` でバイト列の文字コード判別 |
| `RandomBoxMuller` | sealed class | Box-Muller 法による正規分布乱数 |
| `StreamReverseReader` | sealed class (IDisposable) | ファイル末尾から逆順に `ReadLine()` |
| `TypeGenerator` | sealed class | 実行時にプロパティ定義から動的型を生成（`ITypeData`） |

### Utility/（static ユーティリティ）

| クラス | 種別 | 役割 |
|---|---|---|
| `UnityUtility` | static | **最重要**。GameObject 生成/削除/アクティブ/親子/レイヤー/コンポーネント/検索の安全ラッパー |
| `UnityPathUtility` | static | Assetsパス⇔フルパス変換、`GetStreamingAssetsPath()` 等 |
| `PathUtility` | static | パス区切り `/` 統一、`Combine`、相対⇔絶対変換 |
| `FileUtility` | static | `GetHash`(SHA256) / `GetCRC`(CRC32) / `IsFileLocked` |
| `DirectoryUtility` | static | 空ディレクトリ削除 / ディレクトリ複製 / `GetAllFilesAsync` |
| `RandomUtility` | static | System.Random ベース乱数（シード設定可、重み付き抽選） |
| `MathematicsRandomUtility` | static | Unity.Mathematics ベース乱数。**戦闘系はこちらを使用**（`Shuffle` / `SampleOne` / `IsPercentageHit`） |
| `BiasSelectUtility` | static | スコア→重み関数（`BiasLowPower` 等）でバイアス付き抽選 |
| `MathUtility` | static | 角度変換 / `Sigmoid` / `InRange` / `GetAngle` |
| `ByteDataUtility` | static | `GetBytesReadable(long)` バイト数の可読表記("1.25 MB") |
| `TypeUtility` | static | C#型名文字列→`Type` 変換、`GetDefaultValue` |
| `TextureUtility` | static | 空テクスチャ/チェッカー柄生成、PNG bytes→Texture2D |
| `ScreenUtility` | static | `GetSize()` 画面サイズ取得 |
| `PlatformUtility` | static | プラットフォーム名文字列取得 |
| `CommandLineUtility` | static | 起動引数 `Get<T>(label, default)` |
| `AndroidUtility` | static | StreamingAssets→Temporary コピー（UniTask、Androidのjar内対策） |
| `LogUtility` | static | `ChunkLog` 長文ログの分割出力 |
| `Reflection` | static | private/public フィールド・プロパティ・メソッドへのリフレクションアクセス |
| `MessagePackFileUtility` | static | MessagePack ファイル Read/Write（AES暗号オプション、UniTask版あり） |
| `GitUtility` | static（**エディタ専用**） | ブランチ名/コミットハッシュ取得、Checkout/Pull 等 |
| `SerializationFileUtility` | static（**エディタ専用**） | `Format` 指定のファイルシリアライズ Read/Write |

## 使い方(実例)

### 1. `Singleton<T>` マネージャー定義 — OnCreate + AddTo(Disposable)

```csharp
// 引用: Client/Assets/Scripts/Client/Core/Sound/BgmManager.cs
public sealed class BgmManager : Singleton<BgmManager>
{
    protected override void OnCreate()
    {
        // Singleton は LifetimeDisposable 継承なので Disposable プロパティに購読を紐付けられる.
        MasterManager.Instance.OnLoadFinishAsObservable()
            .Subscribe(_ => UpdateBgmInfo())
            .AddTo(Disposable);
    }
}

// 利用側はどこからでも Instance でアクセス（初回アクセス時に自動生成される）.
var bgmManager = BgmManager.Instance;
```

### 2. CreateInstance / ReleaseInstance による明示的な生成・破棄管理

```csharp
// 引用: Client/Assets/Scripts/Client/Battle/Core/Manager/BattleManager.cs
public sealed partial class BattleManager : Singleton<BattleManager>
{
    private BattleManager(){ }  // コンストラクタは非公開でよい (Activator経由で生成される).

    public static void CreateManager()
    {
        CreateInstance();

        BattleBonusManager.CreateInstance();
        BattleEventManager.CreateInstance();
        BattleFlowManager.CreateInstance();
        // ... 戦闘系シングルトンを戦闘開始時に一括生成.
    }

    public static void ReleaseManager()
    {
        ReleaseInstance();

        BattleBonusManager.ReleaseInstance();
        BattleEventManager.ReleaseInstance();
        // ... 戦闘終了時に一括解放 (instance = null に戻り、次戦闘で作り直し).
    }
}
```

### 3. `SingletonMonoBehaviour<T>` + `Prefab` フィールド

```csharp
// 引用: Client/Assets/Scripts/Client/Core/LoadingIconManager.cs
public sealed class LoadingIconManager : SingletonMonoBehaviour<LoadingIconManager>
{
    [SerializeField]
    private Prefab loadingIconPrefab = null;

    private LoadingIcon loadingIcon = null;

    public void Initialize()
    {
        // Prefab フィールドから生成 (生成先の親もインスペクタで設定済み).
        loadingIcon = loadingIconPrefab.Instantiate<LoadingIcon>();

        UnityUtility.SetActive(loadingIcon, false);
    }
}

// 利用側 — 引用: Client/Assets/Scripts/Client/Core/Scene/SceneManager.cs
await LoadingScreen.Instance.FadeIn();

// シーン上に存在しない場合のフォールバック生成パターン.
// 引用: Client/Assets/Scripts/Client/Core/Initialize/InitializeObject/InitializeObject.core.cs
var loadingScreen = LoadingScreen.Instance ?? UnityUtility.Instantiate<LoadingScreen>(null, loadingScreenPrefab);
```

### 4. `Prefab` によるリストアイテム生成（View実装の定型）

```csharp
// 複数生成 — 引用: Client/Assets/Scripts/Client/Feature/Window/Langage/LangageSelectWindow.cs
items = langageSelectItemPrefab.Instantiate<LangageSelectItem>(allLanguage.Length).ToArray();

// ObjectPool と併用する場合は Source / Parent に分解して渡す.
// 引用: Client/Assets/Scripts/Client/Scene/Battle/View/BattleLog/BattleLogView.cs
[SerializeField]
private Prefab logItemPrefab = null;

logItemPool = new ObjectPool<BattleLogItemView>(gameObject, logItemPrefab.Source, "LogItem");

var logItem = logItemPool.Get(logItemPrefab.Parent);
```

### 5. インスペクタ属性 / SafeValue / Serialize 型

```csharp
// [ReadOnly] 表示専用フィールド — 引用: Client/Assets/Scripts/Client/Core/AppConfig.cs
[SerializeField, ReadOnly]
private string iosAppIdentifier = string.Empty;

// [Label] enum表示名 — 引用: Client/Assets/UniModules/Scripts/Modules/Network/WebRequest/WebRequestManager.cs
public enum Method
{
    None,

    [Label("POST")]
    Post,
}
// → method.ToLabelName() で "POST" を取得 (拡張メソッドは Methods/EnumExtensions.cs).

// SafeValue: 戦闘のバフ効果時間をメモリ改ざん対策付きで保持.
// 引用: Client/Assets/Scripts/Client/Battle/Core/Status/BattleStatus.cs
protected XInt? time = null;

this.time = time.HasValue ? new XInt(time.Value) : null;

// SerializableNullable: インスペクタで「値なし」を表現 (基盤内実例. Client側は未使用).
// 引用: Client/Assets/UniModules/Scripts/Modules/UI/Layout/PreferredSizeCopy.cs
[SerializeField]
private FloatNullable min = new FloatNullable(null);
```

## API(主要公開メンバー)

### `Singleton<T>`

| メンバー | 説明 |
|---|---|
| `static T Instance { get; }` | 取得。null なら内部で `CreateInstance()` を呼び自動生成 |
| `static bool Exists { get; }` | 生成済みか（生成せずに判定） |
| `static T CreateInstance()` | 生成（`Activator.CreateInstance(type, true)` — 非publicコンストラクタ可）。`SingletonManager.Register` → `OnCreate()` |
| `static void ReleaseInstance()` | `Release()` を呼び `instance = null` に戻す |
| `void Release()` | `SingletonManager.Remove` → `OnRelease()` → instance破棄（ISingleton実装） |
| `void Refresh()` | `OnRefresh()` を呼ぶ（ISingleton実装） |
| `protected virtual void OnCreate()` | 生成時フック。購読登録はここで `.AddTo(Disposable)` |
| `protected virtual void OnRelease()` | 解放時フック |
| `protected virtual void OnRefresh()` | `SingletonManager.Refresh()` 時フック |

### `SingletonMonoBehaviour<T>`

| メンバー | 説明 |
|---|---|
| `static T Instance { get; }` | 取得。キャッシュが null なら `UnityUtility.FindObjectOfType<T>()` で検索。**見つからなければ null**（自動生成しない） |
| `static bool HasInstance { get; }` | インスタンス保持済みか |
| `static T CreateInstance()` | **空の GameObject** を新規作成して `AddComponent<T>`（プレハブからは生成しない） |
| `static void DestroyInstance()` | gameObject ごと `SafeDelete` して instance を null に |
| `protected virtual void Awake()` | instance 登録 + 重複チェック（重複時は自分の gameObject を削除） |
| `protected virtual void OnDestroy()` | instance == this なら null 化 |

### `SingletonManager`

| メンバー | 説明 |
|---|---|
| `static void Register(ISingleton)` / `Remove(ISingleton)` | 登録簿の追加/削除（`Singleton<T>` が内部で呼ぶ） |
| `static void Refresh()` | 全シングルトンの `OnRefresh()` を実行 |
| `static void ReleaseAll()` | 全シングルトンを `Release()`（ゲームリセット系処理用） |
| `static IEnumerable<ISingleton> GetAllInstance()` | 登録済み一覧 |

### `LifetimeDisposable`

| メンバー | 説明 |
|---|---|
| `CompositeDisposable Disposable { get; }` | R3 の CompositeDisposable（遅延生成）。`Subscribe(...).AddTo(Disposable)` で寿命連動解除 |
| `bool IsDisposed { get; }` | Dispose 済みか |
| `void Dispose()` | `OnDispose()` → 保持購読を一括 Dispose（ファイナライザからも呼ばれる） |
| `protected virtual void OnDispose()` | 破棄時フック |

### `Prefab`

| メンバー | 説明 |
|---|---|
| `GameObject Source { get; }` | 参照プレハブ本体（ObjectPool 等へ渡す用） |
| `GameObject Parent { get; }` | 生成先の親（インスペクタで設定） |
| `GameObject Instantiate(bool active = true, bool instantiateInWorldSpace = false)` | parent の子として1つ生成 |
| `IEnumerable<GameObject> Instantiate(int count, ...)` | count 個生成 |
| `T Instantiate<T>(bool active = true, ...) where T : Component` | 生成 + コンポーネント取得（View実装の定型） |
| `IEnumerable<T> Instantiate<T>(int count, ...)` | 複数生成 + コンポーネント取得 |

### インスペクタ属性

| 属性 | 使い方 |
|---|---|
| `[ReadOnly]` | `[SerializeField, ReadOnly] private string xxx;` — 編集不可表示 |
| `[EnumFlags]` | enum フィールドを MaskField（複数選択）表示。`prop.intValue` ベース |
| `[IntSelectable(1, 2, 4)]` / `[FloatSelectable(...)]` / `[StringSelectable("a","b")]` | 指定候補のみの Popup 入力 |
| `[Label("表示名", no = 0)]` | enum 値へ表示名付与（インスペクタ属性ではない）。取得は `enumValue.ToLabelName(no)` |

### Serialize 型（`Extensions.Serialize`）

| メンバー | 説明 |
|---|---|
| `T Value { get; set; }` | 値。**HasValue == false で get すると InvalidOperationException** |
| `bool HasValue { get; }` | 値が入っているか |
| `T GetValueOrDefault()` / `GetValueOrDefault(T)` | 安全な取得 |
| `int? ToNullable()` 等 | 標準 Nullable へ変換 |
| implicit 変換 | `IntNullable x = 5;` / `IntNullable x = (int?)null;` / `int? y = x;` すべて可 |
| `SerializableDictionary<TKey,TValue>` | abstract。**使うには具象型を派生定義**（例: `class MyDict : SerializableDictionary<string, int> { }`）してフィールド化 |

### SafeValue 型

| メンバー | 説明 |
|---|---|
| `new XInt(value)` / `Value { get; set; }` | 生成・読み書き（内部は XOR 難読化 byte[]） |
| 演算子 | `+ - * / %` `== != < >` `++ --` 等を元型同様に使用可。`int n = xint;`（implicit）/ `XInt x = (XInt)n;`（explicit） |
| `SafeValue.Pack / UnPack` | 独自型を難読化する場合の低レベルAPI |

### Scope 派生

| クラス | 使い方 |
|---|---|
| `Scope`（基底） | `CloseScope()` を実装した派生を `using` で使う。Dispose 二重呼び出し安全 |
| `DisableStackTraceScope` | `using (new DisableStackTraceScope()) { Debug.Log(...); }` — 全LogType または指定LogTypeのみ |
| `StopwatchScope` | `using (new StopwatchScope(ms => Debug.Log($"{ms}ms"))) { ... }` |
| `GizmosColorScope` | `OnDrawGizmos` 内で色を一時変更 |
| `LockReloadAssembliesScope` / `DiisplayProgressScope` | エディタ専用（ビルド・インポート処理用） |

### `UnityUtility`（カテゴリ別）

| カテゴリ | メンバー |
|---|---|
| 状態プロパティ | `IsPlaying` / `IsEditor` / `IsDebugBuild` / `IsBatchMode` / `RealtimeSinceStartup`（毎フレーム更新のキャッシュ。`Application.isPlaying` 等より低コスト） |
| 生成 | `CreateEmptyGameObject(parent, name)` / `CreateGameObject<T>(parent, name)` / `Instantiate(parent, original[, count])` / `Instantiate<T>(parent, original[, count])`（parent の子として生成、レイヤー引継ぎ） |
| 削除 | `SafeDelete(instance, immediate = false)`（再生中は Destroy、非再生は DestroyImmediate を自動選択）/ `DeleteComponent<T>` / `DeleteGameObject<T>` |
| null判定 | `IsNull(object)` — **UnityEngine.Object の Destroyed 判定込み**の null チェック |
| アクティブ | `SetActive(instance, state)`（同値ならスキップ）/ `IsActive` / `IsActiveInHierarchy` |
| 親子・レイヤー | `SetParent(instance, parent)` / `SetLayer(source, target, setChildLayer)` |
| コンポーネント | `GetComponent<T>` / `GetComponents<T>` / `AddComponent<T>` / `GetOrAddComponent<T>` / `GetInterface<T>` / `GetInterfaces<T>` |
| 検索（**高負荷注意**） | `FindObjectOfType<T>([root])` / `FindObjectsOfType<T>([root])`（DontDestroyOnLoad 内も対象）/ `FindObjectsOfTypeInChild<T>(root)`（直下のみ）/ `FindObjectOfInterface<T>` / `FindCameraForLayer(layerMask)` |
| 階層 | `GetChildrenAndSelf(root)` / `GetHierarchyPath(target)` / `GetChildHierarchyPath(root, target)` |
| その他 | `IsPrefabModeInstance(target)` |

### 乱数・抽選

| メンバー | 説明 |
|---|---|
| `RandomUtility.SetRandomSeed(int?)` / `Seed` | シード制御（リプレイ・デバッグ用） |
| `RandomUtility.RandomInRange(min, max)` | int/float/double 各オーバーロード |
| `RandomUtility.IsPercentageHit(percentage, max = 100)` | 確率判定 |
| `RandomUtility.GetRandomIndexByWeight(int[])` / `GetRandomByWeight<T>(int[], T[])` | 重み付き抽選 |
| `RandomUtility.RandomBool()` / `RandomString(length)` | 便利系 |
| `MathematicsRandomUtility.*` | 上記とほぼ同APIの Unity.Mathematics 版 + `Sample<T>(source, count)` / `SampleOne<T>(source)` / `Shuffle<T>(source)`。**戦闘ロジックはこちら**（例: `Client/Assets/Scripts/Client/Battle/Brain/BrainBase.cs`） |
| `BiasSelectUtility.SelectOne<T>(source, scoreSelector, weightFunc)` | スコア→重み変換関数付き抽選。`BiasLowPower` / `BiasLowInverse` / `BiasHighPower` 等のプリセット重み関数あり |

### パス・ファイル

| メンバー | 説明 |
|---|---|
| `PathUtility.Combine(params string[])` | `/` 区切りで結合（Windows `\` を正規化） |
| `PathUtility.ConvertPathSeparator(path)` / `GetPathWithoutExtension(path)` / `IsFolder` / `IsFile` | パス操作 |
| `UnityPathUtility.ConvertAssetPathToFullPath(assetPath)` / `ConvertFullPathToAssetPath(path)` | "Assets/..." ⇔ 絶対パス |
| `UnityPathUtility.GetStreamingAssetsPath()` / `GetPrivateDataPath()` / `GetProjectFolderPath()` | 各種ルートパス |
| `FileUtility.GetHash(path)` | SHA256（16進文字列） |
| `FileUtility.GetCRC(path)` / `IsFileLocked(path)` | CRC32 / ロック判定 |
| `DirectoryUtility.Clone(source, copy)` / `Clean(path)` / `DeleteEmpty(dir)` / `GetAllFilesAsync(path)` | ディレクトリ操作 |
| `MessagePackFileUtility.Write<T>/Read<T>(filePath, [cryptoKey])` + `WriteAsync/ReadAsync` | MessagePack シリアライズ + AES 暗号（UniTask） |

### その他

| メンバー | 説明 |
|---|---|
| `Reflection.GetPrivateField<T, TResult>(instance, name)` / `SetPrivateField` / `InvokePrivateMethod` 等 | 非公開メンバーアクセス（テスト・デバッグ・基盤ハック用） |
| `FixedQueue<T>.Enqueue(item)` / `OnExtrudedAsObservable()` | 固定長化された Enqueue（`new` 隠蔽）とあふれ通知 |
| `ProcessExecute.Start()` / `StartAsync()` | `WorkingDirectory` / `Hide` 等をプロパティ設定して実行 |
| `CommandLineUtility.Get<T>("label", default)` | `-label value` 形式の起動引数取得（バッチビルド用） |
| `Encode.GetEncode(bytes)` | BOM等から Encoding 判別 |
| `LogUtility.ChunkLog(logs, title, outputCallback, maxLine)` | Unityログ上限対策の分割出力 |
| `GitUtility.GetBranchName(dir)` / `GetCommitHash(dir)`（エディタ専用） | ビルド情報埋め込み用 |

## 注意点・罠

- **`Singleton<T>` の生成/破棄はプロジェクト慣例として明示管理**: `Instance` アクセスでも自動生成されるが、Client 側は初期化フロー（`InitializeObject`、`BattleManager.CreateManager` 等）で `CreateInstance()` を明示的に呼び、対応する終了処理で `ReleaseInstance()` する。解放しない限り instance は残り続ける（シーン遷移では消えない）。
- **`Singleton<T>.Release()` は `Dispose()` を呼ばない**: `Release()` は `OnRelease()` → `instance = null` のみ。`Disposable` に登録した購読は参照が切れて GC のファイナライザが走るまで解除されない。即時解除が必要な購読は `OnRelease()` 内で明示的に解除（または `Dispose()` 呼び出し）すること。
- **`Singleton<T>` のコンストラクタは private/protected にする**: `Activator.CreateInstance(type, true)` が非公開コンストラクタを呼ぶため公開不要（例: `private BattleManager(){ }`）。`new` での生成はシングルトン管理外になるため禁止。
- **`SingletonMonoBehaviour<T>.Instance` は null を返しうる**: シーン上に存在しなければ `FindObjectOfType`（高負荷）で探した上で null。プレハブから生成したい場合は `Instance ?? UnityUtility.Instantiate<T>(null, prefab)` パターンを使う（`CreateInstance()` は空 GameObject 生成でプレハブ不可）。
- **`SingletonMonoBehaviour<T>` は基底が `Awake`/`OnDestroy` を使用**（`[ExecuteAlways]` 付きでエディタ非再生時も動く）。派生でオーバーライドする場合は `base.Awake()` 呼び出し必須。同型を複数配置すると後から Awake した方の gameObject が自動削除される。なおプロジェクトルールで新規コードの Unity ライフサイクル使用は原則禁止（基底の既存実装は例外）。
- **`Prefab` はフィールド未設定だと `Instantiate` が LogError("Prefabが登録されていません") を出して null を返す**。例外は投げないため、戻り値の null チェック漏れに注意。
- **`SerializableNullable<T>.Value` は HasValue == false のとき InvalidOperationException**。`GetValueOrDefault()` を優先。
- **`SerializableDictionary` / `SerializableNullable` は abstract**: Unity はジェネリック型を直接シリアライズできないため、具象派生型を定義して使う（int/float 等は `IntNullable` 等が定義済み）。Client 側では現状未使用（`using Extensions.Serialize` は 0 ファイル）、基盤内 UI モジュールで使用。
- **SafeValue（X系）は実行時メモリ専用**: XOR シードがプロセス起動毎に変わるため、シリアライズ・永続化・通信には使えない（保存時は元の型に戻す）。また内部 byte[] 生成・変換コストがあるため大量の高頻度演算には不向き。
- **`FixedQueue<T>.Enqueue` は `new` によるメソッド隠蔽**: `Queue<T>` 型の変数に代入すると固定長制限が効かなくなる。必ず `FixedQueue<T>` 型のまま使う。
- **`UnityUtility.FindObjectsOfType` / `FindObjectOfInterface` 系は検索負荷が高い**（XMLコメントにも明記）。毎フレーム呼び出し禁止。キャッシュすること。
- **`UnityUtility.IsNull` と `== null` の違い**: UnityEngine.Object の「Destroyed だが参照は残っている」状態を正しく null 扱いする。Unity オブジェクトの生存判定は `IsNull` を使う。
- **`Editor/` サブフォルダはエディタ専用**（各属性の PropertyDrawer、`PrefabPropertyDrawer`、`GitUtility`、`SerializationFileUtility`、`LockReloadAssembliesScope`、`DiisplayProgressScope`）。ランタイムコードから参照しない。`PrefabPropertyDrawer` のみ namespace が `Extensions.Devkit`。
- **`LabelAttribute` は PropertyAttribute ではない**: インスペクタ表示用ではなく、`.ToLabelName()`（`Methods/EnumExtensions.cs`）で enum の表示名を取る仕組み。
- **乱数の使い分け**: 戦闘ロジック（リプレイ整合性が必要な箇所）は `MathematicsRandomUtility`、それ以外の汎用用途は `RandomUtility`。どちらもシード設定可。
- `DiisplayProgressScope` はクラス名が typo（Diisplay）のまま。grep 時に注意。

## 関連

- [Methods.md](Methods.md) — 拡張メソッド群。`.AddTo` の R3 連携、`SampleOne` / `ForEach` / `IsEmpty`、`ToLabelName`（EnumExtensions）、`AesCryptoKey`（AESExtensions）はこちら
- [Devkit.md](Devkit.md) — エディタ拡張ユーティリティ（`EditorLayoutTools` 等。`PrefabPropertyDrawer` が依存）
- [../Modules/ObjectPool.md](../Modules/ObjectPool.md) — `Prefab.Source` / `Prefab.Parent` と併用するオブジェクトプール
- [../Modules/View.md](../Modules/View.md) — View 実装での `Prefab` / `UnityUtility` 使用文脈
- [../Modules/R3Extension.md](../Modules/R3Extension.md) — R3 関連拡張（`LifetimeDisposable` の購読管理と関係）
