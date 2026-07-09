# Window

> **namespace**: `Modules.Window`
> **場所**: `Client/Assets/UniModules/Scripts/Modules/Window/`（Window.cs / PopupManager.cs / PopupStashManager.cs / TouchBlock.cs / PopupParent.cs の5ファイル。`Editor/` サブフォルダなし）
> **依存**: UniTask / R3 / Extensions（`UnityUtility`, `SingletonMonoBehaviour`, `Singleton`, `Scope`）/ Modules.InputControl（`BlockInput`）/ Modules.Scene（シーン遷移連携）

## 概要

ポップアップウィンドウ基盤。`Window` が開閉ライフサイクル（`Open{ Prepare → SetActive(true) → OnOpen → Status=Opened }` / `Close{ OnClose → SetActive(false) → Status=Closed → DeleteOnCloseなら破棄 }`）を、`PopupManager` が多重表示管理（表示順・背面タッチブロック・シーン遷移時の破棄）を担う。

主要クラス: `Window`（開閉ライフサイクルと `Status` 管理。`Prepare / OnOpen / OnClose` が override ポイント）/ `PopupManager<TInstance>`（開いている Window のリスト管理〈Scene用 / Global用の2系統〉・親GameObject生成・TouchBlock 制御）/ `PopupStashManager<...>`（ScenePopups の退避/復元スタック。加算シーン遷移用。シーン Leave 時に自動破棄）/ `TouchBlock`（背面タッチブロック兼 暗幕）/ `PopupParent`（ポップアップ親プレハブの参照ホルダ）/ `IPopupManager`（BackKey 連携用の抽象）。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| ウィンドウを開きたい | `await PopupManager<T>.Open(window)`（**static**。インスタンス経由呼び出しは CS0176 エラー） |
| シーン遷移で消えないウィンドウを開きたい | `PopupManager<T>.Open(window, isGlobal: true)`（DontDestroyOnLoad 配下の Global 親に載る） |
| ウィンドウが閉じるまで待ちたい | `await window.Wait()` |
| ウィンドウを閉じたい | `await window.Close()`（PopupManager からの除去は自動） |
| 閉じてもインスタンスを使い回したい | `window.DeleteOnClose = false;`（デフォルト true = Close時破棄） |
| 開く前にデータ・Viewを非同期更新したい | `protected override UniTask Prepare()` を override |
| 開閉完了を購読したい | `window.OnOpenAsObservable()` / `OnCloseAsObservable()` |
| 全ウィンドウの開閉をフックしたい | `PopupManager<T>` の `OnOpenWindowAsObservable() / OnOpenedWindowAsObservable() / OnClosedWindowAsObservable()` |
| 最前面のウィンドウを知りたい | `PopupManager<T>.Current`（`GetCurrentWindow()` も同等） |
| 何かウィンドウが開いている間待ちたい | `await UniTask.WaitWhile(() => PopupManager<T>.Current != null)` |
| 加算シーン遷移時にポップアップを保持→戻りで復元したい | `PopupStashManager.Instance.Stash()` / `Restore()` |

## 使い方

### 開閉ライフサイクル全体像（実行順序と override ポイント）

```
Window.Open(blockInput = true):
  [None/Closed のみ実行。Prepare/Opened/Close 中は無視]
  Status=Prepare → BlockInput生成（全入力ロック）
  → await Prepare()          ← override: 開く前のデータ取得・View更新（GameObjectはまだ非アクティブ）
  → SetActive(true)
  → await OnOpen()           ← override: 開くアニメ等
  → 入力ロック解除 → Status=Opened → OnOpenAsObservable 発火

Window.Close(blockInput = true):
  [Opened のみ実行]
  Status=Close → BlockInput生成
  → await OnClose()          ← override: 閉じるアニメ等
  → SetActive(false) → 入力ロック解除 → OnCloseAsObservable 発火 → Status=Closed
  → DeleteOnClose(デフォルトtrue) なら GameObject 破棄

PopupManager.Open(window, isGlobal = false, inputProtect = true):
  SetActive(false) → Register{Scene|Global}（Popup親へ SetParent + SetLayer + Close購読で自動リスト除去）
  → DisplayPriority 昇順ソート → UpdateContents()（siblingIndex再配置・TouchBlockを最前面ウィンドウ直下へ・FadeIn・Current更新）
  → OnOpenWindowAsObservable 発火 → await window.Open(inputProtect) → OnOpenedWindowAsObservable 発火
  ※ Status==Opened のwindowを渡すと登録のみ（最前面化・アニメ再実行なし。Stash復元用）
```

## 注意点・罠

- **DeleteOnClose はデフォルト true**。`Close()` で GameObject ごと破棄される。使い回す場合は Open 前に `DeleteOnClose = false`。破棄後のフィールド参照に注意
- **`Wait()` は Open 前に呼ぶと即抜けする**（非アクティブ判定のため）。必ず `await PopupManager.Open(window)` → `await window.Wait()` の順
- **Open/Close は Status ガードで多重呼び出しを無視する**。Opened 中の再 `PopupManager.Open()` は「最前面化（再登録）」になり、Openアニメ・onOpen 通知は再実行されない
- **シーン Leave 時に ScenePopups は `Clean()` で即破棄される**（Close を通らない = OnClose も OnCloseAsObservable も発火しない）。シーンを跨ぎたいものは `isGlobal: true`、加算遷移で戻るなら PopupStashManager
- **Instantiate の親は null でよい**。`PopupManager.Open` が Popup 親へ SetParent + SetLayer する。逆に PopupManager を通さず自前で `window.Open()` だけ呼ぶと、親・レイヤー・TouchBlock・BackKey 対象（Current）管理から外れる
- **Open/Close 実行中は BlockInput で全入力ロック**。`Prepare()` に通信等の長い処理を書くとその間タップ不能になる。ロック不要なら `Open(blockInput: false)`
- **背景タップで閉じる挙動・BackKey 連携は利用側の PopupManager 派生側で実装する**（`Current` の `CloseIfTouchOutside` を参照して Close 等）
- **TouchBlock（暗幕）は1枚だけを最前面ウィンドウの直下に差し込む方式**。多重表示時は最前面のみ操作可能になる。暗幕の見た目を個別ウィンドウで変えることはできない
- **`PopupManager.Unregister` は「閉じる」ではない**。リストから外すだけでウィンドウは表示されたまま（Stash 専用と考える）。通常は `window.Close()` を呼べば Close 購読経由で自動除去される
- **`CreateInstance()` は SerializeField が空のインスタンスを作ってしまうため使わない**。必ずプレハブから `Instantiate` → `Initialize()`
- **Stash は ScenePopups のみ対象**（Global は退避されない）。退避中の Window は所有シーンの Leave 時に自動破棄される（戻らず別シーンへ抜けた場合のリーク対策が組み込み済み）
- `Current` は Global 優先（GlobalPopups にあればそちらの最前面を返す。無ければ ScenePopups の最前面、どちらも無ければ null）
- Window の GameObject が Open/Close 途中で破棄されると入力ロック解除が GC（Scope のファイナライザ）任せになる。演出中の強制破棄は避け、`Close()` を経由する

## 関連

- [View](View.md) — VM 接続（`WindowViewModel`）・子View の VM 自動解決
- [Scene](Scene.md) — シーン遷移イベント（OnPrepare / OnLeaveComplete / 加算遷移）。PopupManager の親再生成・Clean・Stash が連動
- [UI](UI.md) — `UIButton` / `UIText` 等、ウィンドウ内部の UI 部品
- [Animation](Animation.md) — `AnimationPlayer`（開閉アニメ再生に利用可能。`Modules.Animation`）
- [BackKey](BackKey.md) — `BackKeyReceiver` / `BackKeyManager`（Android バックキー基盤。`IPopupManager` 経由で本モジュールと連携）
- [InputControl](InputControl.md) — `BlockInput`（開閉中の入力ロック）
- [Sound](Sound.md) — 開閉SE等の再生
- [Extensions/Core.md](../Extensions/Core.md) — `SingletonMonoBehaviour` / `Singleton` / `Scope`
