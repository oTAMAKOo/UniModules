# Extensions/Methods（汎用拡張メソッド群）

> **namespace**: `Extensions`（例外: `Vector.cs` のみ `UnityEngine`）
> **場所**: `Client/Assets/UniModules/Scripts/Extensions/Methods/`
> **Client側使用**: 約865ファイル（Client側 .cs 1501中 ≈57%、2026-07時点）— 最頻出の基盤
> **依存**: UniTask / R3（+R3.Triggers） / DOTween（ScrollRect系） / Newtonsoft.Json（ToJson） / Unity.Linq / Modules.UI（ButtonEventTrigger・PreferredSizeCopy）

## 概要

string・コレクション・Dictionary・Transform/RectTransform・Color・Enum・日時・暗号/圧縮・UniTask/Observable・UI 等の汎用拡張メソッド集。
**汎用処理（null/空判定、ランダム抽選、変換、UI操作等）を書く前に必ずここを確認すること**。車輪の再発明が最も起きやすい領域。
`using Extensions;` を書くだけで全メソッドが使用可能（コンポーネント取得・GameObject 生成/破棄などは `UnityUtility`（[Core.md](Core.md)）側にある点に注意）。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| コレクションの null/空チェック | `source.IsEmpty()`（null 安全） |
| 文字列の null/空チェック | `str.IsNullOrEmpty()` |
| ランダムに1件選ぶ | `source.SampleOne()` |
| ランダムにN件抽出（重複あり） | `source.Sample(n)` |
| シャッフル（重複なし） | `source.Shuffle()` |
| 重み付き抽選（ドロップ・ガチャ等） | `source.WeightSample(x => x.Weight)` |
| Dictionary から安全に値取得 | `dict.GetValueOrDefault(key)`（プロジェクト規約で必須） |
| Dictionary に無ければ生成して追加 | `dict.GetOrAdd(key, k => new ...)` |
| 最大/最小値を持つ「要素」を取得 | `source.FindMax(x => x.Level)` / `FindMin` |
| N個ずつに分割（API リクエスト分割等） | `source.Chunk(50)` |
| インデックス付き foreach | `source.ForEach((x, i) => ...)` |
| 条件一致する要素のインデックス | `source.IndexOf(x => ..., defaultValue: -1)` |
| キーで重複除外 | `source.DistinctBy(x => x.Id)` |
| bool で昇順/降順を切替ソート | `source.Order(ascending, x => x.Key)` |
| 循環インデックス（末尾→先頭ループ） | `source.GetWrapIndex(index)` |
| 16進カラーコード ⇔ Color | `"FF0000".HexToColor()` / `color.ColorToHex()` |
| UnixTime ⇔ DateTime | `dateTime.ToUnixTime()` / `unixTime.UnixTimeToDateTime()` |
| 文字列→任意型（int/enum/bool等）変換 | `"123".To<int>()` |
| 範囲外例外を出さない Substring | `str.SafeSubstring(start, len)` |
| リッチテキストタグ除去 | `str.RemoveTag()` |
| SHA256 / CRC32 ハッシュ | `str.GetHash()` / `str.GetCRC()` |
| AES 暗号化/復号 | `bytes.Encrypt(key)` / `str.Decrypt(key)`（`AesCryptoKey`） |
| GZip/Deflate 圧縮/解凍 | `bytes.Compress()` / `bytes.Decompress()` |
| Transform を初期状態に戻す | `gameObject.ResetTransform()` / `transform.Reset()` |
| 階層パス取得（デバッグログ用） | `transform.GetHierarchyPath()` |
| RectTransform のサイズ変更（pivot考慮） | `rt.SetSize(size)` / `SetWidth` / `SetHeight` |
| アンカー/ピボットをプリセット設定 | `rt.SetAnchor(AnchorType.TopLeft)` / `rt.SetPivot(PivotPreset.MiddleCenter)` |
| 矩形同士の重なり/内包/接触判定 | `rt.IsOverlap(other)` / `rt.Contains(other)` / `rt.IsHit(other)` |
| レイアウトグループ強制再構築 | `rt.ForceRebuildLayoutGroup()` |
| ScrollRect を指定アイテムへスクロール | `scrollRect.ScrollToItem(item)` / `ScrollToItemAsync(...)` |
| ボタン長押し・プレス検知 | `button.OnLongPressAsObservable()` / `OnPressAsObservable()` |
| UniTask を Destroy 連動で Forget | `task.Forget(component)` / `task.Forget(gameObject)` |
| 値の変化を Observable 監視 | `target.ObserveEveryValueChanged(x => x.Value)` |
| オブジェクトの複製（DeepCopy） | `obj.DeepCopy()` |
| JSON 文字列化（デバッグログ等） | `obj.ToJson(indented: true)` |
| Enum のラベル名取得 | `enumValue.ToLabelName()`（`LabelAttribute`） |
| Enum 名から値を検索 | `EnumExtensions.FindByName<T>(name)`（非拡張 static） |
| パーティクル再生中判定 | `particleSystem.IsPlayback()` |
| コンポーネント取得/追加・SetActive・生成/破棄 | `UnityUtility.GetComponent` / `GetOrAddComponent` / `SetActive` 等 → [Core.md](Core.md) |
| 子オブジェクト列挙 | `UnityUtility.GetChildren` / Unity.Linq `Descendants()` → [Core.md](Core.md) |

## カテゴリ別拡張メソッド一覧

### string（StringExtensions.cs）

`public static partial class StringExtensions`

| メソッド | 説明 |
|---|---|
| `IsNullOrEmpty()` | `string.IsNullOrEmpty` のインスタンス風呼び出し（null レシーバ可） |
| `Combine(string[] targets)` | 複数文字列を連結（null/空要素はスキップ） |
| `FixLineEnd(string newLineStr = "\n")` | 改行コードを統一（`\r\n`/`\r` → 指定文字） |
| `Escape()` / `Unescape()` | エスケープシーケンス ⇔ 制御コード変換（`Regex.Escape/Unescape`） |
| `SubstringEquals(int startIndex, int length, string target)` | 指定範囲の部分文字列が target と一致するか |
| `SafeSubstring(int startIndex, int? length = null)` | 範囲外でも例外を出さない Substring |
| `GetHash()` / `GetHash(Encoding)` | SHA256 ハッシュ（16進小文字文字列） |
| `GetCRC()` / `GetCRC(Encoding)` | CRC32 ハッシュ（16進小文字文字列） |
| `IsMatch(string[] keywords)` | 全キーワードを含むか（大文字小文字無視） |
| `RemoveTag()` | `<...>` タグを除去（リッチテキスト除去） |
| `To<T>(T defaultValue = default)` | 文字列を任意型へ変換（整数/浮動小数/bool("0"/"1"可)/DateTime/enum/Nullable 対応） |

string を対象とする他カテゴリのメソッド: `HexToColor` / `ToHexCode`（→ Color）、`Encrypt` / `Decrypt`（→ 暗号）、`Compress(Encoding)`（→ 圧縮）。

### IEnumerable&lt;T&gt;・コレクション（EnumerableExtensions.cs）

`public static partial class EnumerableExtensions`

| メソッド | 説明 |
|---|---|
| `IsEmpty()` | null または要素0なら true（**null 安全な空チェックの標準**） |
| `ToStrings()`（this object[]） | 各要素を `ToString()` した string[] を返す |
| `DistinctBy(Func<T, TKey> keySelector)` | キーの重複を除外（遅延評価） |
| `GetWrapIndex(int requestIndex)` | 循環インデックス取得（負数もラップ。空だと例外） |
| `WeightSample()`（this IEnumerable&lt;int&gt;） | 重み配列から抽選しインデックス返却（合計0以下は -1） |
| `WeightSample(Func<T, int> selector, T defaultValue = default)` | 重み付き抽選で要素を返却 |
| `Sample(int sampleCount, Random random = null)` | ランダムN件抽出（**重複あり**。重複を避けるなら Shuffle） |
| `SampleOne(Random random = null, T defaultValue = default)` | ランダムに1件取得（空なら defaultValue） |
| `Shuffle(Random random = null)` | ランダム順で列挙（Fisher–Yates、遅延評価） |
| `ForEach(Action<T>)` / `ForEach(Action<T, int>)` | 各要素へ処理実行（index 付きオーバーロードあり） |
| `IndexOf(Func<T, bool> action, int defaultValue = -1)` | 条件一致する最初のインデックス |
| `Swap(int firstIndex, int secondIndex)` | 2要素の位置を入れ替えた列を返す（範囲外は例外） |
| `ElementAtOrDefault(int index, T defaultValue = default)` | 範囲外なら既定値（BCL 版に defaultValue 指定を追加） |
| `FindMin(Func<TSource, TResult> selector)` | 最小値を**持つ要素**を返す（空なら default） |
| `FindMax(Func<TSource, TResult> selector)` | 最大値を**持つ要素**を返す（空なら default） |
| `RemoveAt(IEnumerable<int> removeTargets)` | 複数インデックスの要素を除外した列を返す |
| `Chunk(int chunkSize)` | 指定個数ずつに分割（`IEnumerable<IEnumerable<T>>`） |
| `Order(bool ascending, Func<T, TKey> keySelector, IComparer<TKey> comparer = null)` | bool で OrderBy / OrderByDescending を切替 |
| `ToHashSet()` / `ToHashSet(comparer)` | ※ `#if !UNITY_2021_2_OR_NEWER` 限定。本プロジェクト（Unity 6）では BCL 版が使われる |

### Dictionary（DictionaryExtensions.cs）

`public static partial class DictionaryExtensions`

| メソッド | 説明 |
|---|---|
| `GetValueOrDefault(TKey key, TValue defaultValue = default)` | キーが無ければ既定値。※ `#if !UNITY_2021_2_OR_NEWER` 限定で、本プロジェクトでは BCL 版が解決される（挙動同等） |
| `GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)` | キーが無ければ valueFactory で生成して追加し返す（キャッシュ実装の定番） |
| `IsDefault()`（this KeyValuePair） | KeyValuePair がデフォルト値かを判定 |

### GameObject（GameObjectExtensions.cs）

| メソッド | 説明 |
|---|---|
| `ResetTransform(bool localPosition = true, bool localRotation = true, bool localScale = true)` | transform をリセット（`Transform.Reset` へ委譲） |

生成/破棄/SetActive/コンポーネント取得/子列挙は `UnityUtility`（[Core.md](Core.md)）にある。

### Transform・RectTransform（TransformExtensions.cs / RectTransformExtensions.cs）

`TransformExtensions`:

| メソッド | 説明 |
|---|---|
| `GetHierarchyPath()` | ルートからの階層パス文字列（`"Root/Child/Target"`。ログ出力用） |
| `Copy(Transform source)` | localPosition / localRotation / localScale をコピー |
| `Reset(bool localPosition = true, bool localRotation = true, bool localScale = true)` | zero / identity / one にリセット |

`RectTransformExtensions`（enum `AnchorType`（TopLeft〜StretchAll の16種）/ `PivotPreset`（9種）を同クラス内に定義）:

| メソッド | 説明 |
|---|---|
| `Copy(RectTransform source)` | Transform 情報に加え anchorMin/Max・pivot・anchoredPosition をコピー |
| `SetDefaultScale()` | localScale を (1,1,1) に |
| `SetPivotAndAnchors(Vector2)` | pivot と anchorMin/Max を同値に設定 |
| `GetSize()` / `GetWidth()` / `GetHeight()` | `rect` のサイズ取得 |
| `FillRect()` | アンカーを StretchAll + offset 0 に（親いっぱいに広げる） |
| `SetAnchor(AnchorType anchor, int offsetX = 0, int offsetY = 0)` | アンカーをプリセット設定 |
| `SetPivot(PivotPreset pivot)` | ピボットをプリセット設定 |
| `SetSize(Vector2)` / `SetWidth(float)` / `SetHeight(float)` | pivot を考慮して offsetMin/Max を調整しサイズ変更 |
| `CalcSize()` | 親 Canvas まで遡って実サイズを算出 |
| `GetWorldRect()` | ワールド座標上の Rect を取得 |
| `CalculateRelativeWorldRect()` | 子階層込みのワールド Bounds（`PreferredSizeCopy` を即時更新してから算出） |
| `GetPreferredSize()` | `LayoutUtility` の preferred 幅/高さを Vector2 で |
| `IsOverlap(RectTransform)` | 矩形同士が重なっているか |
| `Contains(RectTransform)` / `Contains(Bounds)` | 対象を完全に内包しているか |
| `IsHit(RectTransform)` / `IsHit(Bounds)` | 対象と接触しているか |
| `ForceRebuildLayoutGroup()` | 子階層の LayoutGroup を優先度順に強制再構築（レイアウト崩れ対策の定番） |

### Color（ColorExtensions.cs）

| メソッド | 説明 |
|---|---|
| `ColorToHex(bool hasAlpha = false)`（this Color / Color32） | Color → 16進文字列（`"RRGGBB(AA)"`、大文字） |
| `HexToColor()`（this string） | 16進文字列 → Color（`#` / `0x` 前置・8桁アルファ対応。空なら `Color.clear`） |
| `ToHexCode()` / `ToHexCode(Encoding)`（this string） | 文字列から一意のカラーコードを生成（CRC ベース。ユーザー識別色等） |

### Enum（EnumExtensions.cs）

| メソッド | 説明 |
|---|---|
| `ToLabelName(int no = 0)`（this Enum） | `LabelAttribute` で指定された表示名を取得（[Core.md](Core.md) の Attribute 参照） |
| `FindByName<T>(string name, T defaultValue = default)` | Enum 名から値を検索（**非拡張 static**: `EnumExtensions.FindByName<T>(...)`） |
| `SetFlag<T>(T flag)`（this Enum） | フラグを立てた値を返す |
| `RemoveFlag<T>(T flag)`（this Enum） | フラグを解除した値を返す |

`obj.ToEnum<T>()`（object → Enum 変換）は ObjectExtensions（後述）。

### 数値・Vector・Rect（FloatExtensions.cs / Vector.cs / Vector2Extensions.cs / Vector3Extensions.cs / RectExtensions.cs）

`FloatExtensions`:

| メソッド | 説明 |
|---|---|
| `ValueInRange(float min, float max)` | min 以上 max 以下か（両端含む） |

`Vector`（**namespace `UnityEngine` の static ヘルパー。拡張メソッドではない**: `Vector.SetX(v, 1f)` と呼ぶ）:

| メソッド | 説明 |
|---|---|
| `SetX/SetY(Vector2, float)` | 指定成分だけ変更した Vector2 を返す |
| `SetX/SetY/SetZ(Vector3, float)` | 指定成分だけ変更した Vector3 を返す |
| `SetX/SetY/SetZ/SetW(Vector4, float)` | 指定成分だけ変更した Vector4 を返す |

`Vector2Extensions`:

| メソッド | 説明 |
|---|---|
| `ToVector2()`（this IEnumerable&lt;float&gt;) | float 列から Vector2 生成 |
| `Cross(Vector2)` | 外積（スカラー） |
| `Perp()` | 垂直（法線）ベクトル |
| `Distance(Vector2)` | 2点間距離 |
| `Degrees(Vector2)` | 対象座標との角度（度） |
| `RotateAroundOrigin(float angle)` | 原点周り回転（**ラジアン**指定） |
| `Length()` / `LengthSq()` | 長さ / 長さの2乗 |
| `Sign(Vector2)` | 時計回りなら 1、反時計回りなら -1 |
| `GetReverse()` | 逆向きベクトル |
| `ToVector3(float z)` / `ToVector3()` | Vector3 へ変換（z 省略時 0） |

`Vector3Extensions`:

| メソッド | 説明 |
|---|---|
| `ToVector3()`（this IEnumerable&lt;float&gt;) | float 列から Vector3 生成 |
| `ToVector2()`（this Vector3） | Z を捨てて Vector2 へ |

`RectExtensions`:

| メソッド | 説明 |
|---|---|
| `Contains(Rect target)` | 対象 Rect を内包しているか |
| `DrawGizmos()` | Gizmos で矩形の枠線を描画（デバッグ用） |

### 日時（TimeExtensions.cs）

enum `UnixTimeConvert { Milliseconds, Seconds, Minutes }` を同ファイルに定義。

| メンバー | 説明 |
|---|---|
| `UNIX_EPOCH`（static readonly DateTime） | 1970-01-01 UTC |
| `To(DateTimeOffset to)`（this DateTimeOffset） | 2時点間の TimeSpan（to - from） |
| `ToUnixTime(UnixTimeConvert type = Seconds)`（this DateTime） | DateTime → UnixTime（ulong。UTC 換算） |
| `UnixTimeToDateTime(UnixTimeConvert type = Seconds)`（this long / ulong） | UnixTime → DateTime（UTC） |

現在時刻の取得自体はプロジェクト規約により `systemModel.LocalTime` を使うこと（`DateTime.Now` 禁止）。

### byte[]・暗号・圧縮・ハッシュ（AESExtensions.cs / CompressionExtensions.cs / HashExtensions.cs）

`AESExtension`（クラス名は単数形）:

| メソッド | 説明 |
|---|---|
| `Encrypt(AesCryptoKey)`（this byte[]） | AES 暗号化 |
| `Decrypt(AesCryptoKey)`（this byte[]） | AES 復号 |
| `Encrypt(AesCryptoKey, bool escape = false)`（this string） | 暗号化して Base64 文字列に（escape=true で URL セーフ化 `+/`→`-_`） |
| `Decrypt(AesCryptoKey, bool escape = false)`（this string) | Base64 文字列を復号（終端の null 文字は除去済み） |

`AesCryptoKey`（同ファイル内の通常クラス）: コンストラクタは `(password)` / `(key, iv)` / `(byte[] key, byte[] iv)`（各 `AesManaged` 指定版あり）。プロパティ `Key` / `Iv` / `Encryptor` / `Decryptor` / `BlockSize`。既定は AES-256 / CBC / PKCS7。

`CompressionExtensions`（enum `CompressionAlgorithm { GZip, Deflate }`、既定 GZip）:

| メソッド | 説明 |
|---|---|
| `Compress(algorithm)`（this byte[]） | 圧縮 |
| `Compress(Encoding, algorithm)`（this string） | 文字列をエンコードして圧縮 |
| `Decompress(algorithm)`（this byte[]） | 解凍（null は null、空は空配列） |
| `Decompress(Encoding, algorithm)`（this byte[]） | 解凍して文字列化 |

> ※ BinaryFormatter ベースの `Compress<T>` / `Decompress<T>` は 2026-07 に削除済み（Decompress側が不動作・BinaryFormatter非推奨のため）。オブジェクトを圧縮したい場合は MessagePack でシリアライズして byte[] 版を使う。

`HashExtension`（クラス名は単数形。**非拡張の static メソッド**）:

| メソッド | 説明 |
|---|---|
| `CalcSHA256(FileStream)` | ファイルストリームの SHA256（16進小文字） |
| `CalcSHA256(string, Encoding)` | 文字列の SHA256（`str.GetHash()` の実体） |

### 非同期（TaskExtensions.cs / UniTaskExtensions.cs）

`TaskExtensions`（`System.Threading.Tasks.Task` 用。PlayFab 等の Task 返却 API との連携用）:

| メソッド | 説明 |
|---|---|
| `Forget(Action<Exception> exceptionHandler = null)`（this Task / Task&lt;T&gt;） | fire-and-forget（例外はハンドラへ） |

`UniTaskExtensions`（**R3 の `Observable<T>` ベース**。UniRx ではない）:

| メソッド | 説明 |
|---|---|
| `ObserveEveryValueChanged(propertySelector, [equalityComparer], [ct])` | 毎フレーム値を監視し変化時に発火する Observable |
| `TakeUntilDestroy(Component / GameObject)` | 対象破棄まで購読を継続 |
| `TakeUntilDisable(Component / GameObject)` | 対象非アクティブ化まで購読を継続 |
| `DoOnError(Action<Exception>)` | 失敗完了時に処理を挟む |
| `DoOnCompleted(Action)` | 成功完了時に処理を挟む |
| `DoOnTerminate(Action)` / `Finally(Action)` | 成否問わず完了時に処理を挟む |
| `DoOnCancel(Action)` | 購読破棄（キャンセル）時に処理を挟む |
| `OnErrorRetry<T, TException>(onError, retryMaxCount, retryDelay)` | 指定例外時に遅延付きリトライ（秒 float / TimeSpan 版） |
| `AsUnitObservable()` | `Observable<T>` → `Observable<Unit>` |
| `ToUniTask([ct])` | Observable の最初の値を UniTask で待機（`FirstAsync`） |
| `Forget(Component / GameObject)`（this UniTask） | **対象の Destroy で自動キャンセルされる Forget**（生存期間の紐付けに必須級） |

### UI（UI/ButtonExtensions.cs / UI/ScrollRectExtensions.cs）

`ButtonExtensions`（`ButtonEventTrigger`（Modules.UI）を GetOrAddComponent して購読。戻り値は R3 `Observable`）:

| メソッド | 説明 |
|---|---|
| `SetLongPressDuration(float duration)` | 長押し判定時間を設定 |
| `OnPressAsObservable()` | 押下イベント |
| `OnReleaseAsObservable()` | 離しイベント（押下時間 float 付き） |
| `OnLongPressAsObservable()` | 長押し成立イベント |
| `OnLongPressReleaseAsObservable()` | 長押し後の離しイベント（float 付き） |
| `OnCancelAsObservable()` | キャンセルイベント |

通常クリックは R3 標準の `button.OnClickAsObservable()` を使用。

`ScrollRectExtensions`（enum `ScrollAlignment { Center, Top, Bottom, Left, Right }`。DOTween 依存）:

| メソッド | 説明 |
|---|---|
| `ScrollToItem(RectTransform item, alignment = Center, offset = default)` | 指定アイテムへ即座にスクロール |
| `ScrollToItemAsync(item, duration = 0.3f, alignment, offset, ease = OutQuad, cancelToken)` | アニメ付きスクロール（`SetUpdate(true)` = unscaled time） |
| `CalculateFocusedScrollPosition(RectTransform item / Vector2 focusPoint)` | 対象を中央に収める normalizedPosition を計算 |

※ `GridListView` / `ListView` 等の仮想リスト（[UI.md](../Modules/UI.md)）は自前の `ScrollToItem(index, ...)` を持つ。これは素の `ScrollRect` 用。

### その他（ObjectExtensions.cs / TypeExtensions.cs / AnimatorExtensions.cs / ParticleSystemExtensions.cs / SpriteAtlasExtensions.cs）

`ObjectExtensions`:

| メソッド | 説明 |
|---|---|
| `DeepCopy<T>()` | BinaryFormatter による複製（対象に `[Serializable]` 必須） |
| `ToJson<T>(bool indented = false)` | Newtonsoft.Json で JSON 文字列化（循環参照は無視） |
| `ToEnum<T>()`（this object） | object（string/数値）→ Enum。失敗時は default(T)（例外を出さない） |

`TypeExtensions`:

| メソッド | 説明 |
|---|---|
| `IsNullable()` | null 代入可能な型か |
| `GetAliasName()` | C# エイリアス名（`Int32`→`int` 等） |
| `GetFormattedName()` | ジェネリック型を `Name<T1, T2>` 形式の文字列に |

`AnimatorExtensions`:

| メソッド | 説明 |
|---|---|
| `IsAvailable()` | activeInHierarchy かつ RuntimeAnimatorController 設定済みか |

`ParticleSystemExtensions`:

| メソッド | 説明 |
|---|---|
| `IsPlayback(bool subemitter = false)` | 再生中か（`IsAlive()` が常に true を返すバグの回避実装。ループは常に true） |
| `GetSubemitters()` | サブエミッター一覧を取得 |

`SpriteAtlasExtensions`:

| メソッド | 説明 |
|---|---|
| `GetAllSprites()` | アトラス内の全 Sprite を取得（"(Clone)" 名を除去） |
| `GetListOfSprites(string match = null)` | Sprite 名一覧（完全一致→キーワード部分一致の順で絞り込み） |

## 使い方(実例)

```csharp
// Client/Assets/Scripts/Client/Feature/Loot/LootDrawer.cs（空チェック + 重み付き抽選）
var candidates = lootRecords.Where(x => !uniqueIndices.Contains(x.Index)).ToArray();

if (candidates.IsEmpty()){ break; }

var record = candidates.WeightSample(x => (int)x.Weight);
```

```csharp
// Client/Assets/Scripts/PlayFab/Api/Economy_v2/GetItems.cs（APIリクエストを50件ずつに分割）
var chunkIds = itemIds.Chunk(50);

foreach (var ids in chunkIds)
{
    var request = new GetItemsRequest
    {
        AuthenticationContext = playFabManager.AuthContext,
        Ids = ids.ToList(),
    };
    // ...
}
```

```csharp
// Client/Assets/Scripts/Client/Master/Skill/SkillMaster.cs（最大レベルのレコード取得）
var skillRecords = ActiveSkillMaster.GetRecordsBySkillId(record.ContentId);

var maxLevelRecord = skillRecords.FindMax(x => x.Level);

maxLevel = maxLevelRecord.Level;
```

```csharp
// Client/Assets/Scripts/Client/Scene/WorldMap/Window/SortiePartySelectWindow/CitadelListItemView.cs
// （Dictionary安全取得 + 16進カラー変換）
var hexColor = CitadelTagColorTable.GetValueOrDefault(userCitadel.TagColor);

if (string.IsNullOrEmpty(hexColor)){ return; }

nameTagImage.color = hexColor.HexToColor();
```

```csharp
// Client/Assets/Scripts/Client/Feature/Footer/FooterView.cs（ScrollRectをアイテム位置へアニメスクロール）
await buttonScrollView.ScrollRect.ScrollToItemAsync(rt, 0.5f, ScrollRectExtensions.ScrollAlignment.Left, offset);
```

## API(主要公開メンバー)

public メソッドは上記「カテゴリ別拡張メソッド一覧」で全クラス網羅済み。補助型のみ以下に補足。

| 型 | 種別 | 定義場所 | 内容 |
|---|---|---|---|
| `AesCryptoKey` | class | AESExtensions.cs | AES 鍵（password から生成 or key+iv 指定）。`Encrypt`/`Decrypt` の引数 |
| `UnixTimeConvert` | enum | TimeExtensions.cs | Milliseconds / Seconds / Minutes（変換単位） |
| `CompressionExtensions.CompressionAlgorithm` | enum | CompressionExtensions.cs | GZip / Deflate |
| `RectTransformExtensions.AnchorType` | enum | RectTransformExtensions.cs | アンカープリセット16種（TopLeft〜StretchAll） |
| `RectTransformExtensions.PivotPreset` | enum | RectTransformExtensions.cs | ピボットプリセット9種 |
| `ScrollRectExtensions.ScrollAlignment` | enum | UI/ScrollRectExtensions.cs | Center / Top / Bottom / Left / Right |

Client 側の使用頻度目安（2026-07、ファイル数）: `GetValueOrDefault` 284 / `IsEmpty`・`IsNullOrEmpty` 各77 / `ForEach` 63 / `ToJson` 32 / `SetAnchor`・`SetSize` 系 24 / `ElementAtOrDefault` 17 / `HexToColor` 16 / `ToUnixTime` 系 14 / `SampleOne` 10。

## 注意点・罠

- **`Vector` クラスは拡張メソッドではない**: namespace `UnityEngine` の static ヘルパー。`Vector.SetX(pos, 1f)` と呼ぶ（`pos.SetX(1f)` は不可）。
- **`HashExtension.CalcSHA256` / `EnumExtensions.FindByName` も非拡張の static メソッド**（クラス名から直接呼ぶ）。
- TimeSpan の長短比較は比較演算子を直接書く（名前と実装が逆だった `IsShorterThan` / `IsLongerThan` は 2026-07 に削除済み）。
- 反射ベクトル計算は `Vector2.Reflect`（Unity 標準）を使う（不動作の拡張 `Vector2Extensions.Reflect` は 2026-07 に削除済み）。
- フラグ判定は BCL の `Enum.HasFlag`（全ビット一致）を使う。「いずれかのビット一致」判定は `(value & flag) != 0` を直接書く（BCLと同名・別意味だった拡張 `EnumExtensions.HasFlag<T>` は 2026-07 に削除済み）。
- `StringExtensions.To<T>` は値の書式不正（FormatException）時に **ArgumentException を throw** する（defaultValue を返すのはそれ以外の失敗時のみ）。
- `Sample` は**重複あり**抽選。重複を避けたいなら `Shuffle().Take(n)` を使う（実装コメントにも明記）。
- `ObjectExtensions.DeepCopy` は BinaryFormatter 使用のため対象型に `[Serializable]` が必須。`ToEnum` は失敗を握りつぶして default(T) を返す。
- `DictionaryExtensions.GetValueOrDefault` / `EnumerableExtensions.ToHashSet` は `#if !UNITY_2021_2_OR_NEWER` 内。本プロジェクト（Unity 6000.4）では**コンパイル対象外**で、呼び出しは BCL 版に解決される（挙動は同等なのでコードはそのまま書いてよい）。
- `EnumerableExtensions` の多くは内部で `ToArray()` する（多重列挙・巨大シーケンスに注意）。`Shuffle` / `Sample` / `Chunk` / `DistinctBy` は遅延評価。
- `ButtonExtensions` の各 Observable は初回呼び出し時に `ButtonEventTrigger` コンポーネントを対象へ**自動追加**する（Modules.UI 依存）。
- `ScrollToItemAsync` は DOTween 依存で `SetUpdate(true)`（タイムスケール非依存）。呼び出し前に content のレイアウトが確定していない場合も内部で `ForceRebuildLayoutImmediate` される。
- `SpriteAtlasExtensions.GetAllSprites` は取得した Sprite の `name` を書き換える（"(Clone)" 除去）。
- `EnumerableExtensions.random` は `System.Random` の共有インスタンス（シード固定の再現性が必要な場合は `Random` を引数で渡す）。
- **Editor/ 配下はエディタ専用**（下記参照）。ランタイムコードから参照しないこと。

### Editor/ 配下（エディタ専用）

`Editor/TextureImporterExtensions.cs` — `TextureImporter` の拡張（UnityEditor 依存）:

| メソッド | 説明 |
|---|---|
| `GetImportWarning()` | インポート警告文字列を取得（internal メソッドをリフレクション呼び出し） |
| `GetPreImportTextureSize()` | インポート前のテクスチャ実サイズを取得（非公開 `GetWidthAndHeight` を呼び出し） |

## 関連

- [Core.md](Core.md) — `UnityUtility`（GetComponent / GetOrAddComponent / SetActive / 生成・破棄 / 子オブジェクト列挙）、`Singleton`、`LabelAttribute`、`IntNullable` / `FloatNullable`（`To<T>` の変換先）、`CRC32`（`GetCRC` の実体）
- [UI.md](../Modules/UI.md) — `ButtonEventTrigger`（ButtonExtensions の実体）、`GridListView` 等の仮想リスト
- [R3Extension.md](../Modules/R3Extension.md) — R3 関連の追加拡張
- [TextData.md](../Modules/TextData.md) — テキスト取得（文字列直書き禁止規約）
- [INDEX.md](../INDEX.md) — 全モジュール一覧
