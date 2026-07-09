# Extensions Core（基盤クラス群）

> **namespace**: `Extensions`（Serialize 配下のみ `Extensions.Serialize`、一部 Editor 用 Drawer は `Extensions.Devkit`）
> **場所**: `Client/Assets/UniModules/Scripts/Extensions/`（`Methods/`・`Devkit/` を除く: `Behaviour/` `Attribute/` `Component/` `Serialize/` `SafeValue/` `Scope/` `Types/` `Utility/`）
> **依存**: R3（LifetimeDisposable / FixedQueue / UnityUtility）、UniTask（AndroidUtility / MessagePackFileUtility）、Unity.Mathematics（MathematicsRandomUtility）、MessagePack（MessagePackFileUtility）

## 概要

基盤の土台となるクラス群。**Singleton パターン（`Singleton<T>` / `SingletonMonoBehaviour<T>`）はマネージャークラスの基底として使用され**、
R3 購読の寿命管理（`LifetimeDisposable`）、View 実装で頻出のプレハブ参照（`Prefab`）、インスペクタ属性、Unity でシリアライズ可能な Nullable 型、
GameObject 操作の安全ラッパー（`UnityUtility`）を提供する。新規マネージャー・View 実装前に必ず本ドキュメントを確認すること。

| フォルダ | 主要クラス |
|---|---|
| `Behaviour/` | `Singleton<T>`（非MonoBehaviourシングルトン基底） / `SingletonMonoBehaviour<T>`（シーン上コンポーネントのシングルトン基底。`[ExecuteAlways]` 付き） / `SingletonManager`（全インスタンスの登録簿。一括 Refresh / ReleaseAll） / `ISingleton`（`Refresh()` / `Release()` の共通IF） / `LifetimeDisposable`（R3 `CompositeDisposable` を保持し Dispose で購読を一括解除） |
| `Component/` | `Prefab`（プレハブ参照+生成先親をセットで持つフィールド用 [Serializable] class。View実装で頻出） / `PrefabPropertyDrawer`（エディタ専用・`Extensions.Devkit`） |
| `Attribute/` | `ReadOnlyAttribute`（編集不可表示） / `EnumFlagsAttribute`（MaskField 表示） / `IntSelectableAttribute`・`FloatSelectableAttribute`・`StringSelectableAttribute`（入力値を候補 Popup に制限） / `LabelAttribute`（enum 値に表示名付与）+ `LabelAttributeUtility`（`ToLabelName` の実体・型別キャッシュ） / 各 PropertyDrawer（エディタ専用） |
| `Serialize/` | `SerializableNullable<T>` 基底 + `IntNullable` / `UIntNullable` / `LongNullable` / `FloatNullable` / `DoubleNullable` / `DecimalNullable`（元の `int?` 等と暗黙相互変換可） / `SerializableDictionary<TKey, TValue>`（keys/values リストに分解してシリアライズ） / 型別 Drawer（エディタ専用） |
| `SafeValue/` | `SafeValue`（XOR による Pack/UnPack） / `XInt`・`XUInt`・`XLong`・`XULong`・`XShort`・`XUShort`・`XSByte`・`XByte`・`XChar`・`XBool`・`XFloat`・`XDouble`・`XString`（メモリ上で XOR 難読化された値型。演算子オーバーロード完備、元型へ implicit 変換） |
| `Scope/` | `Scope`（IDisposable 基底。`CloseScope()` を Dispose 時に1度だけ実行） / `DisableStackTraceScope`（ログのスタックトレース一時無効化） / `StopwatchScope`（終了時に経過ミリ秒をコールバック） / `GizmosColorScope`（`Gizmos.color` 退避・復元） / `LockReloadAssembliesScope`・`DiisplayProgressScope`（エディタ専用） |
| `Types/` | `FixedQueue<T>`（固定長キュー・あふれ通知 `OnExtrudedAsObservable()`） / `NaturalComparer`（文字列自然順ソート） / `ProcessExecute`（外部プロセス実行。`Start()` / `StartAsync()` → `Result`） / `BezierCurve` / `CRC16`・`CRC32` / `Encode`（バイト列の文字コード判別） / `RandomBoxMuller`（正規分布乱数） / `StreamReverseReader`（ファイル末尾から逆順 `ReadLine()`） / `TypeGenerator`（動的型生成） |
| `Utility/` | `UnityUtility`（**最重要**: GameObject 生成/削除/アクティブ/親子/レイヤー/コンポーネント/検索の安全ラッパー） / `UnityPathUtility`（Assetsパス⇔フルパス・各種ルートパス） / `PathUtility`（パス区切り `/` 統一・Combine） / `FileUtility`（SHA256 / CRC32 / ロック判定） / `DirectoryUtility` / `RandomUtility`（System.Random ベース・シード設定可・重み付き抽選） / `MathematicsRandomUtility`（Unity.Mathematics ベース。**戦闘系はこちら**） / `BiasSelectUtility`（スコア→重み関数付き抽選。`BiasLowPower` 等のプリセットあり） / `MathUtility` / `ByteDataUtility`（バイト数可読表記） / `TypeUtility` / `TextureUtility` / `ScreenUtility` / `PlatformUtility` / `CommandLineUtility`（起動引数 `Get<T>(label, default)`） / `AndroidUtility`（StreamingAssets→Temporary コピー） / `LogUtility`（長文ログの分割出力） / `Reflection`（非公開メンバーへのリフレクションアクセス） / `MessagePackFileUtility`（MessagePack ファイル Read/Write。AES 暗号オプション・UniTask版あり） / `GitUtility`・`SerializationFileUtility`（エディタ専用） |

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

## 使い方

定型パターン:

- `Singleton<T>` マネージャー定義（`OnCreate()` で購読登録 + `.AddTo(Disposable)`。利用側はどこからでも `Instance`）
- `CreateInstance()` / `ReleaseInstance()` による明示的な生成・破棄管理（特定フェーズ開始時に関連シングルトンを一括生成、終了時に一括解放）
- `SingletonMonoBehaviour<T>` + `Prefab` フィールドの組み合わせ
- シーン上に存在しない場合のフォールバック生成: `Instance ?? UnityUtility.Instantiate<T>(null, prefab)`
- `Prefab` によるリストアイテム複数生成: `prefab.Instantiate<T>(count)`
- `Prefab` + ObjectPool 併用（`Source` / `Parent` に分解して渡す）
- `[SerializeField, ReadOnly]` による表示専用フィールド
- `[Label("表示名")]` enum + `ToLabelName()`（基盤内実例: `Client/Assets/UniModules/Scripts/Modules/Network/WebRequest/WebRequestManager.cs`）
- SafeValue（`XInt`）で数値をメモリ改ざん対策付きで保持
- `FloatNullable` のインスペクタ利用（基盤内実例: `Client/Assets/UniModules/Scripts/Modules/UI/Layout/PreferredSizeCopy.cs`）
- `MathematicsRandomUtility` によるリプレイ整合性が必要な乱数生成

## 注意点・罠

- **`Singleton<T>` の生成/破棄は明示管理を推奨**: `Instance` アクセスでも自動生成されるが、初期化フローで `CreateInstance()` を明示的に呼び、対応する終了処理で `ReleaseInstance()` する運用が想定されている。解放しない限り instance は残り続ける（シーン遷移では消えない）。
- **`Singleton<T>.Release()` は `Dispose()` を呼ばない**: `Release()` は `OnRelease()` → `instance = null` のみ。`Disposable` に登録した購読は参照が切れて GC のファイナライザが走るまで解除されない。即時解除が必要な購読は `OnRelease()` 内で明示的に解除（または `Dispose()` 呼び出し）すること。
- **`Singleton<T>` のコンストラクタは private/protected にする**: `Activator.CreateInstance(type, true)` が非公開コンストラクタを呼ぶため公開不要（例: `private ExampleManager(){ }`）。`new` での生成はシングルトン管理外になるため禁止。
- **`SingletonMonoBehaviour<T>.Instance` は null を返しうる**: シーン上に存在しなければ `FindObjectOfType`（高負荷）で探した上で null（自動生成しない）。プレハブから生成したい場合は `Instance ?? UnityUtility.Instantiate<T>(null, prefab)` パターンを使う（`CreateInstance()` は空 GameObject 生成でプレハブ不可）。
- **`SingletonMonoBehaviour<T>` は基底が `Awake`/`OnDestroy` を使用**（`[ExecuteAlways]` 付きでエディタ非再生時も動く）。派生でオーバーライドする場合は `base.Awake()` 呼び出し必須。同型を複数配置すると後から Awake した方の gameObject が自動削除される。
- **`Prefab` はフィールド未設定だと `Instantiate` が LogError("Prefabが登録されていません") を出して null を返す**。例外は投げないため、戻り値の null チェック漏れに注意。
- **`SerializableNullable<T>.Value` は HasValue == false のとき InvalidOperationException**。`GetValueOrDefault()` を優先。
- **`SerializableDictionary` / `SerializableNullable` は abstract**: Unity はジェネリック型を直接シリアライズできないため、具象派生型を定義して使う（int/float 等は `IntNullable` 等が定義済み）。基盤内 UI モジュール（`PreferredSizeCopy` 等）で使用。
- **SafeValue（X系）は実行時メモリ専用**: XOR シードがプロセス起動毎に変わるため、シリアライズ・永続化・通信には使えない（保存時は元の型に戻す）。また内部 byte[] 生成・変換コストがあるため大量の高頻度演算には不向き。
- **`FixedQueue<T>.Enqueue` は `new` によるメソッド隠蔽**: `Queue<T>` 型の変数に代入すると固定長制限が効かなくなる。必ず `FixedQueue<T>` 型のまま使う。
- **`UnityUtility.FindObjectsOfType` / `FindObjectOfInterface` 系は検索負荷が高い**（XMLコメントにも明記）。毎フレーム呼び出し禁止。キャッシュすること。
- **`UnityUtility.IsNull` と `== null` の違い**: UnityEngine.Object の「Destroyed だが参照は残っている」状態を正しく null 扱いする。Unity オブジェクトの生存判定は `IsNull` を使う。
- **`Editor/` サブフォルダはエディタ専用**（各属性の PropertyDrawer、`PrefabPropertyDrawer`、`GitUtility`、`SerializationFileUtility`、`LockReloadAssembliesScope`、`DiisplayProgressScope`）。ランタイムコードから参照しない。`PrefabPropertyDrawer` のみ namespace が `Extensions.Devkit`。
- **`LabelAttribute` は PropertyAttribute ではない**: インスペクタ表示用ではなく、`.ToLabelName()`（`Methods/EnumExtensions.cs`）で enum の表示名を取る仕組み。
- **乱数の使い分け**: リプレイ整合性が必要な箇所は `MathematicsRandomUtility`、それ以外の汎用用途は `RandomUtility`。どちらもシード設定可。
- `DiisplayProgressScope` はクラス名が typo（Diisplay）のまま。grep 時に注意。

## 関連

- [Methods.md](Methods.md) — 拡張メソッド群。`.AddTo` の R3 連携、`SampleOne` / `ForEach` / `IsEmpty`、`ToLabelName`（EnumExtensions）、`AesCryptoKey`（AESExtensions）はこちら
- [Devkit.md](Devkit.md) — エディタ拡張ユーティリティ（`EditorLayoutTools` 等。`PrefabPropertyDrawer` が依存）
- [../Modules/ObjectPool.md](../Modules/ObjectPool.md) — `Prefab.Source` / `Prefab.Parent` と併用するオブジェクトプール
- [../Modules/View.md](../Modules/View.md) — View 実装での `Prefab` / `UnityUtility` 使用文脈
- [../Modules/R3Extension.md](../Modules/R3Extension.md) — R3 関連拡張（`LifetimeDisposable` の購読管理と関係）
