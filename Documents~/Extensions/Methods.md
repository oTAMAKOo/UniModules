# Extensions/Methods（汎用拡張メソッド群）

> **namespace**: `Extensions`（例外: `Vector.cs` のみ `UnityEngine`）
> **場所**: `Client/Assets/UniModules/Scripts/Extensions/Methods/`
> **Client側使用**: 約865ファイル（Client側 .cs 1501中 ≈57%、2026-07時点）— 最頻出の基盤
> **依存**: UniTask / R3（+R3.Triggers） / DOTween（ScrollRect系） / Newtonsoft.Json（ToJson） / Unity.Linq / Modules.UI（ButtonEventTrigger・PreferredSizeCopy）

## 概要

string・コレクション・Dictionary・Transform/RectTransform・Color・Enum・日時・暗号/圧縮・UniTask/Observable・UI 等の汎用拡張メソッド集。
**汎用処理（null/空判定、ランダム抽選、変換、UI操作等）を書く前に必ずここを確認すること**。車輪の再発明が最も起きやすい領域。
`using Extensions;` を書くだけで全メソッドが使用可能（コンポーネント取得・GameObject 生成/破棄などは `UnityUtility`（[Core.md](Core.md)）側にある点に注意）。
Client 側の使用頻度目安（2026-07、ファイル数）: `GetValueOrDefault` 284 / `IsEmpty`・`IsNullOrEmpty` 各77 / `ForEach` 63 / `ToJson` 32 / `SetAnchor`・`SetSize` 系 24 / `ElementAtOrDefault` 17 / `HexToColor` 16 / `ToUnixTime` 系 14 / `SampleOne` 10。

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

| メソッド | 説明 |
|---|---|
| `IsNullOrEmpty()` | null レシーバ可の空チェック |
| `Combine(targets)` | 複数文字列を連結（null/空要素はスキップ） |
| `FixLineEnd(newLineStr = "\n")` | 改行コード統一 |
| `Escape()` / `Unescape()` | エスケープシーケンス ⇔ 制御コード変換 |
| `SubstringEquals(startIndex, length, target)` | 指定範囲の部分文字列が target と一致するか |
| `SafeSubstring(startIndex, length?)` | 範囲外でも例外を出さない Substring |
| `GetHash([Encoding])` / `GetCRC([Encoding])` | SHA256 / CRC32 ハッシュ（16進小文字文字列） |
| `IsMatch(keywords)` | 全キーワードを含むか（大文字小文字無視） |
| `RemoveTag()` | `<...>` タグを除去（リッチテキスト除去） |
| `To<T>(defaultValue = default)` | 任意型へ変換（整数/浮動小数/bool("0"/"1"可)/DateTime/enum/Nullable 対応） |

string を対象とする他カテゴリのメソッド: `HexToColor` / `ToHexCode`（→ Color）、`Encrypt` / `Decrypt`（→ 暗号）、`Compress(Encoding)`（→ 圧縮）。

### IEnumerable&lt;T&gt;・コレクション（EnumerableExtensions.cs）

| メソッド | 説明 |
|---|---|
| `IsEmpty()` | null または要素0なら true（**null 安全な空チェックの標準**） |
| `ToStrings()`（this object[]） | 各要素を `ToString()` した string[] |
| `DistinctBy(keySelector)` | キーの重複を除外（遅延評価） |
| `GetWrapIndex(requestIndex)` | 循環インデックス取得（負数もラップ。空だと例外） |
| `WeightSample()`（this IEnumerable&lt;int&gt;） / `WeightSample(selector, default)` | 重み付き抽選（int列→インデックス。合計0以下は -1 / 要素返却版） |
| `Sample(sampleCount, [random])` | ランダムN件抽出（**重複あり**） |
| `SampleOne([random], [defaultValue])` | ランダムに1件取得（空なら defaultValue） |
| `Shuffle([random])` | ランダム順で列挙（Fisher–Yates、遅延評価） |
| `ForEach(action)` | 各要素へ処理実行（index 付きオーバーロードあり） |
| `IndexOf(predicate, defaultValue = -1)` | 条件一致する最初のインデックス |
| `Swap(firstIndex, secondIndex)` | 2要素の位置を入れ替えた列を返す（範囲外は例外） |
| `ElementAtOrDefault(index, defaultValue)` | 範囲外なら既定値 |
| `FindMin(selector)` / `FindMax(selector)` | 最小/最大値を**持つ要素**を返す（空なら default） |
| `RemoveAt(removeTargets)` | 複数インデックスの要素を除外した列を返す |
| `Chunk(chunkSize)` | 指定個数ずつに分割 |
| `Order(ascending, keySelector, [comparer])` | bool で OrderBy / OrderByDescending を切替 |
| `ToHashSet([comparer])` | `#if !UNITY_2021_2_OR_NEWER` 限定。本プロジェクト（Unity 6）では BCL 版が使われる |

### Dictionary（DictionaryExtensions.cs）

| メソッド | 説明 |
|---|---|
| `GetValueOrDefault(key, defaultValue = default)` | キーが無ければ既定値。`#if !UNITY_2021_2_OR_NEWER` 限定で、本プロジェクトでは BCL 版が解決される（挙動同等） |
| `GetOrAdd(key, valueFactory)` | キーが無ければ生成して追加し返す（キャッシュ実装の定番） |
| `IsDefault()`（this KeyValuePair） | KeyValuePair がデフォルト値かを判定 |

### GameObject（GameObjectExtensions.cs）

`ResetTransform(localPosition, localRotation, localScale)`（`Transform.Reset` へ委譲）のみ。生成/破棄/SetActive/コンポーネント取得/子列挙は `UnityUtility`（[Core.md](Core.md)）にある。

### Transform・RectTransform（TransformExtensions.cs / RectTransformExtensions.cs）

`TransformExtensions`: `GetHierarchyPath()`（ルートからの階層パス文字列。ログ出力用）/ `Copy(source)`（localPosition・localRotation・localScale をコピー）/ `Reset(...)`（zero / identity / one にリセット）

`RectTransformExtensions`（enum `AnchorType`（TopLeft〜StretchAll の16種）/ `PivotPreset`（9種）を同クラス内に定義）:

| メソッド | 説明 |
|---|---|
| `Copy(source)` | Transform 情報に加え anchorMin/Max・pivot・anchoredPosition をコピー |
| `SetDefaultScale()` / `SetPivotAndAnchors(v2)` | localScale を (1,1,1) に / pivot と anchorMin/Max を同値に設定 |
| `GetSize()` / `GetWidth()` / `GetHeight()` | `rect` のサイズ取得 |
| `FillRect()` | アンカーを StretchAll + offset 0 に（親いっぱいに広げる） |
| `SetAnchor(anchor, offsetX = 0, offsetY = 0)` / `SetPivot(pivot)` | アンカー / ピボットをプリセット設定 |
| `SetSize(v2)` / `SetWidth(f)` / `SetHeight(f)` | pivot を考慮して offsetMin/Max を調整しサイズ変更 |
| `CalcSize()` | 親 Canvas まで遡って実サイズを算出 |
| `GetWorldRect()` / `CalculateRelativeWorldRect()` | ワールド座標上の Rect / 子階層込みのワールド Bounds（`PreferredSizeCopy` を即時更新してから算出） |
| `GetPreferredSize()` | `LayoutUtility` の preferred 幅/高さを Vector2 で |
| `IsOverlap(rt)` / `Contains(rt / Bounds)` / `IsHit(rt / Bounds)` | 重なり / 完全内包 / 接触判定 |
| `ForceRebuildLayoutGroup()` | 子階層の LayoutGroup を優先度順に強制再構築（レイアウト崩れ対策の定番） |

### Color（ColorExtensions.cs）

`ColorToHex(hasAlpha = false)`（Color / Color32 → 16進文字列 `"RRGGBB(AA)"`、大文字）/ `HexToColor()`（string → Color。`#` / `0x` 前置・8桁アルファ対応。空なら `Color.clear`）/ `ToHexCode([Encoding])`（文字列から一意のカラーコードを生成。CRC ベース。ユーザー識別色等）

### Enum（EnumExtensions.cs）

`ToLabelName(no = 0)`（`LabelAttribute` で指定された表示名を取得。[Core.md](Core.md) の Attribute 参照）/ `FindByName<T>(name, default)`（Enum 名から値を検索。**非拡張 static**: `EnumExtensions.FindByName<T>(...)`）/ `SetFlag<T>(flag)` / `RemoveFlag<T>(flag)`（フラグを立てた/解除した値を返す）。`obj.ToEnum<T>()`（object → Enum 変換）は ObjectExtensions（後述）。

### 数値・Vector・Rect（FloatExtensions.cs / Vector.cs / Vector2Extensions.cs / Vector3Extensions.cs / RectExtensions.cs）

- `FloatExtensions`: `ValueInRange(min, max)`（min 以上 max 以下か。両端含む）
- `Vector`（**namespace `UnityEngine` の static ヘルパー。拡張メソッドではない**: `Vector.SetX(v, 1f)` と呼ぶ）: `SetX/SetY`（Vector2）・`SetX/SetY/SetZ`（Vector3）・`SetX/SetY/SetZ/SetW`（Vector4）— 指定成分だけ変更した値を返す
- `Vector2Extensions`: `ToVector2()`（float 列→Vector2）/ `Cross`（外積スカラー）/ `Perp()`（垂直（法線）ベクトル）/ `Distance`（2点間距離）/ `Degrees`（対象座標との角度・度）/ `RotateAroundOrigin(angle)`（原点周り回転。**ラジアン**指定）/ `Length()`・`LengthSq()` / `Sign`（時計回りなら 1、反時計回りなら -1）/ `GetReverse()`（逆向き）/ `ToVector3([z])`（z 省略時 0）
- `Vector3Extensions`: `ToVector3()`（float 列→Vector3）/ `ToVector2()`（Z を捨てて Vector2 へ）
- `RectExtensions`: `Contains(Rect)`（対象 Rect を内包しているか）/ `DrawGizmos()`（Gizmos で矩形の枠線描画。デバッグ用）

### 日時（TimeExtensions.cs）

enum `UnixTimeConvert { Milliseconds, Seconds, Minutes }` を同ファイルに定義。
`UNIX_EPOCH`（static readonly。1970-01-01 UTC）/ `To(to)`（DateTimeOffset 2時点間の TimeSpan）/ `ToUnixTime(type = Seconds)`（DateTime → UnixTime。ulong・UTC 換算）/ `UnixTimeToDateTime(type = Seconds)`（long / ulong → DateTime。UTC）
現在時刻の取得自体はプロジェクト規約により `systemModel.LocalTime` を使うこと（`DateTime.Now` 禁止）。

### byte[]・暗号・圧縮・ハッシュ（AESExtensions.cs / CompressionExtensions.cs / HashExtensions.cs）

- `AESExtension`（クラス名は単数形）: `Encrypt(key)` / `Decrypt(key)`（byte[] 版）、`Encrypt(key, escape = false)` / `Decrypt(key, escape = false)`（string 版。Base64 文字列。escape=true で URL セーフ化 `+/`→`-_`。復号は終端の null 文字除去済み）
- `AesCryptoKey`（同ファイル内の通常クラス）: コンストラクタは `(password)` / `(key, iv)` / `(byte[] key, byte[] iv)`（各 `AesManaged` 指定版あり）。プロパティ `Key` / `Iv` / `Encryptor` / `Decryptor` / `BlockSize`。既定は AES-256 / CBC / PKCS7
- `CompressionExtensions`（enum `CompressionAlgorithm { GZip, Deflate }`、既定 GZip）: `Compress([algorithm])`（byte[]）/ `Compress(Encoding, ...)`（string をエンコードして圧縮）/ `Decompress([algorithm])`（解凍。null は null、空は空配列）/ `Decompress(Encoding, ...)`（解凍して文字列化）。オブジェクトを圧縮したい場合は MessagePack でシリアライズしてから byte[] 版を使う
- `HashExtension`（クラス名は単数形。**非拡張の static メソッド**）: `CalcSHA256(FileStream)` / `CalcSHA256(string, Encoding)`（`str.GetHash()` の実体。16進小文字）

### 非同期（TaskExtensions.cs / UniTaskExtensions.cs）

`TaskExtensions`（`System.Threading.Tasks.Task` 用。PlayFab 等の Task 返却 API との連携用）: `Forget([exceptionHandler])`（Task / Task&lt;T&gt; の fire-and-forget。例外はハンドラへ）

`UniTaskExtensions`（**R3 の `Observable<T>` ベース**。UniRx ではない）:

| メソッド | 説明 |
|---|---|
| `ObserveEveryValueChanged(propertySelector, [equalityComparer], [ct])` | 毎フレーム値を監視し変化時に発火する Observable |
| `TakeUntilDestroy(...)` / `TakeUntilDisable(...)`（Component / GameObject） | 対象破棄 / 非アクティブ化まで購読を継続 |
| `DoOnError` / `DoOnCompleted` / `DoOnTerminate`・`Finally` / `DoOnCancel` | 失敗時 / 成功時 / 成否問わず完了時 / 購読破棄（キャンセル）時に処理を挟む |
| `OnErrorRetry<T, TException>(onError, retryMaxCount, retryDelay)` | 指定例外時に遅延付きリトライ（秒 float / TimeSpan 版） |
| `AsUnitObservable()` | `Observable<T>` → `Observable<Unit>` |
| `ToUniTask([ct])` | Observable の最初の値を UniTask で待機（`FirstAsync`） |
| `Forget(Component / GameObject)`（this UniTask） | **対象の Destroy で自動キャンセルされる Forget**（生存期間の紐付けに必須級） |

### UI（UI/ButtonExtensions.cs / UI/ScrollRectExtensions.cs）

`ButtonExtensions`（`ButtonEventTrigger`（Modules.UI）を GetOrAddComponent して購読。戻り値は R3 `Observable`。通常クリックは R3 標準の `button.OnClickAsObservable()` を使用）:
`SetLongPressDuration(duration)`（長押し判定時間設定）/ `OnPressAsObservable()`（押下）/ `OnReleaseAsObservable()`（離し。押下時間 float 付き）/ `OnLongPressAsObservable()`（長押し成立）/ `OnLongPressReleaseAsObservable()`（長押し後の離し。float 付き）/ `OnCancelAsObservable()`（キャンセル）

`ScrollRectExtensions`（enum `ScrollAlignment { Center, Top, Bottom, Left, Right }`。DOTween 依存。`GridListView` / `ListView` 等の仮想リスト（[UI.md](../Modules/UI.md)）は自前の `ScrollToItem(index, ...)` を持つ — これは素の `ScrollRect` 用）:
`ScrollToItem(item, alignment = Center, offset)`（即座にスクロール）/ `ScrollToItemAsync(item, duration = 0.3f, alignment, offset, ease = OutQuad, ct)`（アニメ付き）/ `CalculateFocusedScrollPosition(item / focusPoint)`（対象を中央に収める normalizedPosition を計算）

### その他（ObjectExtensions.cs / TypeExtensions.cs / AnimatorExtensions.cs / ParticleSystemExtensions.cs / SpriteAtlasExtensions.cs）

- `ObjectExtensions`: `DeepCopy<T>()`（BinaryFormatter による複製。`[Serializable]` 必須）/ `ToJson<T>(indented = false)`（Newtonsoft.Json で JSON 文字列化。循環参照は無視）/ `ToEnum<T>()`（object（string/数値）→ Enum。失敗時は default(T)・例外を出さない）
- `TypeExtensions`: `IsNullable()`（null 代入可能な型か）/ `GetAliasName()`（C# エイリアス名。`Int32`→`int` 等）/ `GetFormattedName()`（ジェネリック型を `Name<T1, T2>` 形式の文字列に）
- `AnimatorExtensions`: `IsAvailable()`（activeInHierarchy かつ RuntimeAnimatorController 設定済みか）
- `ParticleSystemExtensions`: `IsPlayback(subemitter = false)`（再生中か。`IsAlive()` が常に true を返すバグの回避実装。ループは常に true）/ `GetSubemitters()`（サブエミッター一覧）
- `SpriteAtlasExtensions`: `GetAllSprites()`（アトラス内の全 Sprite。"(Clone)" 名を除去）/ `GetListOfSprites(match = null)`（Sprite 名一覧。完全一致→キーワード部分一致の順で絞り込み）

### Editor/ 配下（エディタ専用）

`Editor/TextureImporterExtensions.cs` — `TextureImporter` の拡張（UnityEditor 依存）: `GetImportWarning()`（インポート警告文字列。internal メソッドをリフレクション呼び出し）/ `GetPreImportTextureSize()`（インポート前のテクスチャ実サイズ。非公開 `GetWidthAndHeight` を呼び出し）

## 注意点・罠

- **`Vector` クラスは拡張メソッドではない**: namespace `UnityEngine` の static ヘルパー。`Vector.SetX(pos, 1f)` と呼ぶ（`pos.SetX(1f)` は不可）。
- **`HashExtension.CalcSHA256` / `EnumExtensions.FindByName` も非拡張の static メソッド**（クラス名から直接呼ぶ）。
- フラグ判定は BCL の `Enum.HasFlag`（全ビット一致）を使う。「いずれかのビット一致」判定は `(value & flag) != 0` を直接書く。
- `StringExtensions.To<T>` は値の書式不正（FormatException）時に **ArgumentException を throw** する（defaultValue を返すのはそれ以外の失敗時のみ）。
- `Sample` は**重複あり**抽選。重複を避けたいなら `Shuffle().Take(n)` を使う（実装コメントにも明記）。
- `ObjectExtensions.DeepCopy` は BinaryFormatter 使用のため対象型に `[Serializable]` が必須。`ToEnum` は失敗を握りつぶして default(T) を返す。
- `DictionaryExtensions.GetValueOrDefault` / `EnumerableExtensions.ToHashSet` は `#if !UNITY_2021_2_OR_NEWER` 内。本プロジェクト（Unity 6000.4）では**コンパイル対象外**で、呼び出しは BCL 版に解決される（挙動は同等なのでコードはそのまま書いてよい）。
- `EnumerableExtensions` の多くは内部で `ToArray()` する（多重列挙・巨大シーケンスに注意）。`Shuffle` / `Sample` / `Chunk` / `DistinctBy` は遅延評価。
- `ButtonExtensions` の各 Observable は初回呼び出し時に `ButtonEventTrigger` コンポーネントを対象へ**自動追加**する（Modules.UI 依存）。
- `ScrollToItemAsync` は DOTween 依存で `SetUpdate(true)`（タイムスケール非依存）。呼び出し前に content のレイアウトが確定していない場合も内部で `ForceRebuildLayoutImmediate` される。
- `SpriteAtlasExtensions.GetAllSprites` は取得した Sprite の `name` を書き換える（"(Clone)" 除去）。
- `EnumerableExtensions.random` は `System.Random` の共有インスタンス（シード固定の再現性が必要な場合は `Random` を引数で渡す）。
- **Editor/ 配下はエディタ専用**（カテゴリ別一覧の末尾参照）。ランタイムコードから参照しないこと。

## 関連

- [Core.md](Core.md) — `UnityUtility`（GetComponent / GetOrAddComponent / SetActive / 生成・破棄 / 子オブジェクト列挙）、`Singleton`、`LabelAttribute`、`IntNullable` / `FloatNullable`（`To<T>` の変換先）、`CRC32`（`GetCRC` の実体）
- [UI.md](../Modules/UI.md) — `ButtonEventTrigger`（ButtonExtensions の実体）、`GridListView` 等の仮想リスト
- [R3Extension.md](../Modules/R3Extension.md) — R3 関連の追加拡張
- [TextData.md](../Modules/TextData.md) — テキスト取得（文字列直書き禁止規約）
- [INDEX.md](../INDEX.md) — 全モジュール一覧
