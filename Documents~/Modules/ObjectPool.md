# ObjectPool

> **namespace**: `Modules.ObjectPool`
> **場所**: `Client/Assets/UniModules/Scripts/Modules/ObjectPool/`
> **依存**: R3 / Extensions（`UnityUtility`）

## 概要

同一プレハブの Instantiate / Destroy 繰り返しを避けるための汎用 GameObject プール。
リストアイテム・エフェクト・ポップテキストなど「同じ見た目の要素を大量に出し入れする」場面で使う。
プールごとに非アクティブな親 `[Pooled]: {poolName}` を生成し、返却されたオブジェクトをその下に退避して使い回す方式。
主要クラス: `ObjectPool<T>`（where T : Component。sealed・非Singleton の通常クラス）の1クラスのみ。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| プールを作りたい | `new ObjectPool<T>(poolParent, prefab, poolName)` |
| プールから取得したい（空なら自動生成） | `pool.Get(parent)` |
| プールへ返却したい | `pool.Release(target)` |
| 事前生成（prewarm）して実行時の Instantiate を避けたい | `pool.Resize(count)` |
| プール内の待機オブジェクトを全破棄したい | `pool.Clear()` |
| 新規生成されたインスタンスに一度だけ初期化を挟みたい | `pool.OnCreateInstanceAsObservable()`（Get / Resize 起因の新規 Instantiate 時のみ発火。取得のたびではない） |
| 取得 / 返却タイミングをフックしたい | `OnGetInstanceAsObservable()` / `OnReleaseInstanceAsObservable()` |
| プール内待機数を知りたい | `pool.Count`（貸出中は含まない） |

## 使い方

- **リストアイテムのプール**: Initialize で `new ObjectPool` + `Resize` で prewarm、Setup 冒頭で表示中アイテムを全 `Release` してから再構築する
- **スタイル別複数プール + 生成時初期化**: `Dictionary<Style, ObjectPool<T>>` で複数プールを構築し、`OnCreateInstanceAsObservable` で新規インスタンスにだけ初期設定、Get 直後に自前リセット・Release 前に子要素を先に返却する

## 注意点・罠

- **貸出中オブジェクトはプール管理外**。`Count` / `Objects` / `Clear()` の対象は待機中のみ。`Release` を忘れたオブジェクトはプールに戻らない（Get 側が新規 Instantiate し続ける）
- **`Release` で初期化されるのは transform のみ**（localPosition / localRotation / localScale）。表示状態・アニメーション・テキスト等は初期化されないため、Get 直後に自前リセットするか、`OnCreateInstanceAsObservable` + Get 後の Set で毎回上書きする
- `Get(null)` は非アクティブなプール親の下に置いたまま返す（画面に出ない）。表示するなら必ず親を渡す
- `Release` は null・破棄済み・返却済みを黙って無視する。二重返却チェックは `Queue.Contains`（線形走査）のため、数千件規模のプールで毎フレーム返却する場合はコストに注意
- プール親（`Instance`）が Destroy されるとキューは自動 Clear される（`OnDestroyAsObservable` 購読済み）。待機オブジェクトも親ごと破棄されるため、画面破棄時に個別の後始末は不要
- `IDisposable` ではない。明示的に破棄したい場合は `Clear()` を呼ぶ。再 Setup 時も既存プールを `Clear()` してから作り直す
- prefab 引数は `GameObject`。Extensions の `Prefab` クラスを使う場合は `.Source` を渡す

## 関連

- [Extensions/Core.md](../Extensions/Core.md) — `UnityUtility`（Instantiate / SetParent / DeleteGameObject）、`Prefab` クラス
- [UI](UI.md) — スクロールリスト等、プールと組み合わせる UI 部品
