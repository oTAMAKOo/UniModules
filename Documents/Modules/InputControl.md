# InputControl

> **namespace**: `Modules.InputControl`
> **場所**: `Client/Assets/UniModules/Scripts/Modules/InputControl/`
> **Client側使用**: 約17ファイル（2026-07時点）
> **依存**: R3 / Extensions（`Scope`, `Singleton<T>`, `UnityUtility`）/ Modules.Devkit.LogHandler（例外時の強制解除）

## 概要

画面全体のタップ入力（uGUI）を一時的に無効化する基盤。通信中・演出中・ウィンドウ開閉中などの多重タップ・操作割り込みを防ぐ。
実体は起動時に常駐生成される全画面ブロックキャンバス（sortingOrder 500 の透明レイキャストターゲット）の表示切替で、ブロック要求は ulong ID の集合で多重管理される。
使う側は `using (new BlockInput())` スコープを書くだけでよい（Lock/Unlock の手動呼び出しは不要）。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| **処理中だけタップを無効化したい（標準）** | `using (new BlockInput()) { await ...; }` |
| 通信中: ローディングアイコン + 入力ブロック | `using (new LoadingScope())`（Client側 `Dominion.Client`） |
| アイコンだけ表示して入力は止めない | `new LoadingScope(blockInput: false)` |
| ウィンドウ開閉アニメ中の入力ブロック | `Window.Open()` / `Close()` が標準で実施（引数 `blockInput: false` で抑止） |
| 現在ブロック中か調べたい | `BlockInputManager.Instance.IsBlocking` |
| ブロック状態の変化を購読したい | `BlockInputManager.Instance.OnUpdateStatusAsObservable()` |
| 異常状態から全ブロックを強制解除したい | `BlockInputManager.Instance.ForceUnlock()`（全体リセット時のみ） |
| 誰がブロックし続けているか調べたい（デバッグ） | エディタメニュー `Extension/Utility/Open BlockInputMonitorWindow` |

## 主要クラス

| クラス | 種別 | 役割 |
|---|---|---|
| `BlockInput` | sealed class / `Extensions.Scope`（IDisposable） | 生成でロック・Dispose で解除する使い捨てスコープ。基本の入口 |
| `BlockInputManager` | Singleton（`Extensions.Singleton<T>` 継承・純C#クラス） | ブロックID集合の管理・状態変化通知・例外時の強制解除 |
| `InputBlockListener` | MonoBehaviour | ブロック状態を購読し、自GameObject（全画面キャンバス）を表示/非表示 |
| `BlockInputMonitorWindow` | **エディタ専用**（SingletonEditorWindow） | ブロック中IDと取得元スタックトレースの一覧表示・手動 Unlock / ForceUnlock |
| `LoadingScope`（Client側） | sealed class / `Extensions.Scope` | `BlockInput` + `LoadingIconManager.Show/Hide` の複合スコープ。通信時の定番 |

### 多重ブロックの仕組み（ID集合方式）

- 参照カウントではなく **ブロックID（ulong 連番）の HashSet** で管理。`BlockInput` 1個 = ID 1個
- ID が1つでも残っていれば `IsBlocking == true`。**全スコープが Dispose されるまでブロック継続**（ネスト・並行 await でも安全）
- `OnUpdateStatusAsObservable()` は「ブロック有無が切り替わった瞬間」だけ bool を発火する（ID の追加・削除のたびには発火しない）
- 例外検知（`ApplicationLogHandler.OnReceivedExceptionAsObservable`）で **ForceUnlock による全解除**が走る（例外でブロックが残留して操作不能になるのを防ぐ安全弁）
- デバッグビルドでは Lock ごとに呼び出し元スタックトレースを記録（`GetTrackContents()` / MonitorWindow で閲覧）

### 表示の実体（ScreenInputBlock）

- プレハブ: `Client/Assets/Resource (Internal)/Core/Prefabs/Canvas (InputBlock).prefab`（Canvas sortingOrder 500 + 透明 `GraphicCast` の全画面 Hitbox + `InputBlockListener`）
- 起動時に `InitializeObject.CreateScreenInputBlock()`（`Client/Assets/Scripts/Client/Core/Initialize/InitializeObject.core.cs`）が "ScreenInputBlock" として生成し DontDestroyOnLoad。**Client実装側での生成・配置は不要**
- 遮断方式は **uGUI レイキャスト遮断**。`Input` API の直接参照や物理コライダー入力は止まらない

## 使い方(実例)

### 標準パターン: 処理中の入力ブロック（Claudeはまずこれを使う）

```csharp
// 引用元: Client/Assets/Scripts/Client/Tutorial/TutorialControllerBase.cs
using Modules.InputControl;

using (new BlockInput())
{
    sceneEnter = false;

    await UniTask.WaitUntil(() => sceneEnter, cancellationToken: cancelToken);
}   // using を抜けた時点で自動解除（例外時も解除される）.
```

### 通信中: LoadingScope（入力ブロック + ローディングアイコン）

```csharp
// 引用元: Client/Assets/Scripts/PlayFab/PlayFabManager.cs
public async UniTask<Login.Result> Login(string userCode)
{
    using (new LoadingScope())
    {
        return await new Login().Execute(userCode);
    }
}
```

PlayFabManager の全APIメソッドがこの形。通信を伴う処理は `BlockInput` 単体ではなく `LoadingScope` を使うのが慣例。

### 動画広告の表示中だけブロック

```csharp
// 引用元: Client/Assets/Scripts/Client/Core/AdsManager.cs
using (new BlockInput())
{
    var displaySuccess = await adsRewarded.Display();

    await ResumeAllSounds();
}
```

### using を跨げない場合: フィールド保持 + 明示 Dispose

```csharp
// 引用元: Client/Assets/UniModules/Scripts/Modules/Window/Window.cs (Open)
var inputBlock = blockInput ? new BlockInput() : null;

await Prepare();

UnityUtility.SetActive(gameObject, true);

await OnOpen();

if (inputBlock != null)
{
    inputBlock.Dispose();
    inputBlock = null;
}
```

### 全体リセット時の強制解除（通常フローでは呼ばない）

```csharp
// 引用元: Client/Assets/Scripts/Client/Model/System/SystemModel.cs (タイトルへ戻す処理)
// 入力制限を解除.
BlockInputManager.Instance.ForceUnlock();
```

## API(主要公開メンバー)

### BlockInput（`Extensions.Scope` 継承）

| メンバー | 説明 |
|---|---|
| `BlockInput()` | 生成と同時に ID を採番して `BlockInputManager.Lock()` |
| `ulong BlockingId` | このスコープのブロックID |
| `void Dispose()`（基底 Scope） | `Unlock()` 実行。using 推奨・多重 Dispose 安全（ファイナライザ保険あり） |

### BlockInputManager（Singleton）

| メンバー | 説明 |
|---|---|
| `bool IsBlocking` | 入力制限中か（ID が1つでも残っていれば true） |
| `ulong[] BlockingIds` | 現在のブロックID一覧 |
| `ulong GetNextBlockingId()` | ID採番（通常は `BlockInput` 経由で使い、直接呼ばない） |
| `void Lock(ulong blockingId)` / `void Unlock(ulong blockingId)` | ID指定のロック/解除（通常は `BlockInput` を使う） |
| `void ForceUnlock()` | 全ブロック強制解除。**他所のブロックも消える** |
| `Observable<bool> OnUpdateStatusAsObservable()` | ブロック有無が切り替わった時に発火 |
| `IReadOnlyDictionary<ulong, string> GetTrackContents()` | ID→取得元スタックトレース（デバッグビルドのみ記録） |

### InputBlockListener（MonoBehaviour）

| メンバー | 説明 |
|---|---|
| `bool IsBlocking` | 現在ブロック表示中か（常駐プレハブに設定済み。個別にアタッチしない） |

### LoadingScope（Client側 `Dominion.Client` / `Extensions.Scope` 継承）

| メンバー | 説明 |
|---|---|
| `LoadingScope(bool blockInput = true)` | ローディングアイコン表示 + （既定で）入力ブロック |
| `void Dispose()`（基底 Scope） | アイコン非表示 + ブロック解除 |

## 注意点・罠

- **Dispose 漏れ = 操作不能**。必ず `using` で囲む。`await` を含む処理も using ブロック内に収めれば例外時も確実に解除される
- 例外ログが1件出ると**全ブロックが強制解除される**仕様（安全弁）。「例外後にブロックが消えている」のは意図通り
- `ForceUnlock()` は自分以外のブロックも解除するため通常フローで呼ばない（SystemModel のタイトル戻し等、全体リセット時のみ）
- 遮断されるのは uGUI（EventSystem 経由）のみ。`Input` 直接参照・物理レイキャスト入力は止まらない
- `OnUpdateStatusAsObservable()` は「変化時」のみ発火。購読開始時点の状態は `IsBlocking` を自分で見る（`InputBlockListener.Start` が実例）
- `BlockInputManager` は初回 `Instance` アクセスで自動生成される（`CreateInstance()` の手動呼び出し不要）
- ブロック中のタップを可視化するUIは無い（画面は見たままタップだけ無効）。「待たせている」ことを見せたい処理は `LoadingScope` を使う
- エディタで「ブロックしっぱなし」になったら `Extension/Utility/Open BlockInputMonitorWindow` で原因箇所（スタックトレース）を特定して手動 Unlock できる

## 関連

- [Window](Window.md) — `Window.Open/Close(blockInput: true)` が開閉アニメ中に本モジュールでブロック（Client側 WindowBase も同様）
- [PlayFab](PlayFab.md) — Client側 PlayFabManager の全APIが `LoadingScope` を使用
- [Scene](Scene.md) — シーン遷移基盤（Modules.Scene 自体は本モジュール未使用。遷移中の覆いは LoadingScreen 系）
- [Extensions/Core.md](../Extensions/Core.md) — `Scope` / `Singleton<T>`
