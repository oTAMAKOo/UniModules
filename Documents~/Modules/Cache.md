# Cache

> **namespace**: `Modules.Cache`
> **場所**: `Client/Assets/UniModules/Scripts/Modules/Cache/`
> **依存**: R3（SpriteAtlasCache） / Extensions（`LifetimeDisposable`, `UnityUtility`, コレクション拡張）

## 概要

**文字列キー → オブジェクト** のメモリ上キャッシュ。ロード済みアセット（SpriteAtlas / PatternTexture 等）や生成済み Sprite を使い回し、二重ロード・二重生成を防ぐ。
キーは呼び出し側が決める（アセットのロードパスやスプライト名を使うのが定石）。`referenceName` を指定すると**同名・同型の Cache インスタンス間でキャッシュ実体を共有**できる。
主要クラス: `Cache<T>`（文字列キーの汎用メモリキャッシュ。referenceName 共有・スレッドセーフ） / `SpriteAtlasCache`（`SpriteAtlas.GetSprite()` の結果をスプライト名キーでキャッシュ。内部で `Cache<Sprite>` を使用）。
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

## 使い方

- **キャッシュミス時のみロードして Add する定型**: `Get(loadPath)` → null なら `ExternalAsset.LoadAsset` → `Add(loadPath, asset)`（キー = ロードパス）
- **複数インスタンス間で Sprite キャッシュを共有**: 全サムネイル等が共通の referenceName を指定して Sprite 実体を共有する
- **アトラス自体（`Cache<SpriteAtlas>`）と Sprite（`SpriteAtlasCache`）の併用**: アトラスは `Cache<SpriteAtlas>` でロード結果を保持し、そこから取得した Sprite は `SpriteAtlasCache` でキャッシュする

## 注意点・罠

- **SpriteAtlas.GetSprite() は呼ぶたびに Sprite インスタンスを複製生成する（Unity仕様）**。アトラスから Sprite を取るコードを書くときは直接 GetSprite せず `SpriteAtlasCache` を使う（さもないと同名 Sprite が増殖しメモリを圧迫する）
- 共有の単位は「referenceName + 型引数 T」。`Cache<Sprite>` と `Cache<SpriteAtlas>` は同じ referenceName でも**別キャッシュ**
- referenceName は文字列でグローバル共有されるため衝突に注意。慣例は `GetType().FullName + "-用途名"`、意図的に共有する場合は共有定数を用意する
- 共有モードの参加者は `WeakReference` 管理で、**最後の生存参加者が Dispose した時に実体がクリア**される
- 共有モードで `Clear()` しても共有相手が生存中は消えない（no-op / false。実クリアの有無は戻り値 bool で判定できる）。「消したはずなのに残っている」はこれ。確実に手放すには `Dispose()`
- ファイナライザは持たないため**明示的 Dispose を推奨**（多重 Dispose は安全）
- `Cache<T>` は **Unity オブジェクトの破棄・アンロードは行わない**（参照を保持するだけ）。アセットの解放は呼び出し側の責務。例外的に `SpriteAtlasCache` のみ Clear 成立時に Sprite を PostLateUpdate タイミングで `SafeDelete` する（フレーム中の使用を壊さない遅延破棄）
- 破棄済みの UnityEngine.Object が値に残ることがある（Destroy してもキャッシュから消えない）。取得後の null / 破棄チェックは呼び出し側で行う
- `Add` の null asset・空 key は黙って無視される（例外にしない）ため、「Add したのに Get で null」の原因になりうる

## 関連

- [FileCache](FileCache.md) — ディスク保存・有効期限付きのファイルキャッシュ（`SetCryptoKey` が必要）
- [LocalData](LocalData.md) — 永続セーブデータ
- [ExternalAsset](ExternalAsset.md) — アセットのロード元。ロード結果を本モジュールでキャッシュする組み合わせが定番
- [Extensions/Core.md](../Extensions/Core.md) — `LifetimeDisposable` / `UnityUtility.SafeDelete`
