# InputControl

画面全体のタップを一時的に止める機能。
処理のあいだだけ入力を無効にし、複数の処理が同時に止めていても全部が終わるまで止め続けられる。

## 基本情報

> **namespace**: `Modules.InputControl`（エディタ専用の `BlockInputMonitorWindow` も同じ namespace）
> **場所**: `Modules/InputControl/`（BlockInput.cs / BlockInputManager.cs / InputBlockListener.cs + `Editor/BlockInputMonitorWindow.cs`）
> **依存**: R3 / Extensions（`Scope`, `Singleton<T>`, `UnityUtility`）/ Modules.Devkit.LogHandler（例外時の強制解除）/ Extensions.Devkit（エディタウィンドウのみ）

## 概要

画面全体のタップ入力（uGUI）を一時的に無効化する基盤。通信中・演出中・ウィンドウ開閉中などの多重タップ・操作割り込みを防ぐ。
実体は `InputBlockListener` を付けた常駐 GameObject（全画面の透明レイキャストターゲットを持つ Canvas を利用側で用意する）の表示切替で、ブロック要求は **ブロックID（ulong 連番）の HashSet** で多重管理される（`BlockInput` 1個 = ID 1個。全スコープが Dispose されるまでブロック継続。ネスト・並行 await でも安全）。
使う側は `using (new BlockInput())` スコープを書くだけでよい（Lock/Unlock の手動呼び出しは不要）。
主要クラス: `BlockInput`（生成でロック・Dispose で解除する Scope。基本の入口） / `BlockInputManager`（Singleton。ブロックID集合の管理・状態変化通知・例外時の強制解除） / `InputBlockListener`（常駐全画面キャンバスの表示切替 MonoBehaviour） / `BlockInputMonitorWindow`（エディタ専用デバッグウィンドウ）。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| **処理中だけタップを無効化したい（標準）** | `using (new BlockInput()) { await ...; }` |
| ウィンドウ開閉アニメ中の入力ブロック | `Window.Open()` / `Close()` が標準で実施（引数 `blockInput: false` で抑止） |
| 現在ブロック中か調べたい | `BlockInputManager.Instance.IsBlocking`（ブロック中の ID 一覧は `BlockingIds`、スコープ自身の ID は `blockInput.BlockingId`） |
| ブロック状態の変化を購読したい | `BlockInputManager.Instance.OnUpdateStatusAsObservable()` |
| 異常状態から全ブロックを強制解除したい | `BlockInputManager.Instance.ForceUnlock()`（全体リセット時のみ） |
| 誰がブロックし続けているか調べたい（デバッグ） | エディタメニュー `Extension/Utility/Open BlockInputMonitorWindow` |

## 使い方

### 標準パターン: 処理中の入力ブロック

```csharp
using Modules.InputControl;

using (new BlockInput())
{
    await UniTask.WaitUntil(() => ready, cancellationToken: cancelToken);
}   // using を抜けた時点で自動解除（例外時も解除される）.
```

### その他のパターン

- **using を跨げない場合**: フィールド保持 + 明示 `Dispose()`（実例: `Modules/Window/Window.cs` の Open）
- **全体リセット時の強制解除**（通常フローでは呼ばない）: `BlockInputManager.Instance.ForceUnlock()`

## 注意点・罠

- **Dispose 漏れ = 操作不能**。必ず `using` で囲む。`await` を含む処理も using ブロック内に収めれば例外時も確実に解除される
- 例外ログが1件出ると**全ブロックが強制解除される**仕様（`ApplicationLogHandler.OnReceivedExceptionAsObservable` 購読の安全弁）。「例外後にブロックが消えている」のは意図通り
- `ForceUnlock()` は自分以外のブロックも解除するため通常フローで呼ばない（全体リセット時のみ）
- 遮断方式は **uGUI レイキャスト遮断**（EventSystem 経由）のみ。`Input` API の直接参照・物理コライダー入力は止まらない
- `OnUpdateStatusAsObservable()` は「ブロック有無が切り替わった瞬間」だけ bool を発火する（ID の追加・削除のたびには発火しない）。購読開始時点の状態は `IsBlocking` を自分で見る（`InputBlockListener.Start` が実例）
- ブロックの実体は起動時に常駐プレハブ（全画面 Canvas）が生成される想定。利用側で本モジュールを組み込む際に一度だけ生成・DontDestroyOnLoad しておく（`InputBlockListener` を持つプレハブを常駐化）。基盤にこのプレハブは含まれない
- `InputBlockListener` は **自身の GameObject のアクティブを切り替える**。購読は `Start` で行うため、非アクティブな状態で生成すると `Start` が走らず、以後ブロックに反応しない
- `InputBlockListener` の表示切替は状態変化の通知を Update のタイミングで受けて行う（`Lock` を呼んだ瞬間ではなく、次の Update で遮断が有効になる）
- `BlockInputManager` は初回 `Instance` アクセスで自動生成される（`CreateInstance()` の手動呼び出し不要）
- ブロック中のタップを可視化するUIは無い（画面は見たままタップだけ無効）
- エディタで「ブロックしっぱなし」になったら `Extension/Utility/Open BlockInputMonitorWindow` で原因箇所（スタックトレース）を特定して手動 Unlock できる（デバッグビルドでは Lock ごとに呼び出し元スタックトレースを記録）

## 関連

- [Window](../Window/Manual.md) — `Window.Open/Close(blockInput: true)` が開閉アニメ中に本モジュールでブロック
- [PlayFab](../PlayFab/Manual.md) — 通信時のローディング表示 + 入力ブロックの複合スコープを利用側で用意することを推奨
- [Scene](../Scene/Manual.md) — シーン遷移基盤（Modules.Scene 自体は本モジュール未使用。遷移中の覆いは `SceneManagerBase` 派生の `TransitionStart` / `TransitionFinish` で実装する）
- [Extensions/Core.md](../../Documents~/Extensions/Core.md) — `Scope` / `Singleton<T>`
