# DoTween

> **namespace**: `Modules.Tweening`（**フォルダ名 `DoTween/` と不一致**）
> **場所**: `Client/Assets/UniModules/Scripts/Modules/DoTween/`（`TweenController.cs` の1ファイルのみ）
> **Client側使用**: using は1ファイル（2026-07時点）= `Dominion.Client.Battle.BattleTween`（`Client/Assets/Scripts/Client/Battle/Utility/BattleTween.cs`）。戦闘演出（バトルログ等）が BattleTween 経由で間接使用
> **依存**: DOTween（`Client/Assets/ThirdParty/DOTween/`）/ UniTask（`UNITASK_DOTWEEN_SUPPORT` 定義済み・csc.rsp）/ R3 / Extensions（`LifetimeDisposable`）/ Modules.TimeUtil（`TimeScale`）

## 概要

DOTween の `Tweener` を「await 可能・キャンセル対応・**再生速度一括制御**」で実行するコントローラ。
`Time.timeScale` を使わずに管理下の全 Tween の `timeScale` を書き換える方式のため、UI や他演出に影響を与えずに特定グループ（例: 戦闘演出）だけ倍速/停止できる。
DOTween を単発で使う分には本モジュールは不要。**速度連動・一括Killが必要なグループ演出**で使う。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| Tween を await で再生したい（キャンセル対応） | `await tweenController.Play(tweener, cancelToken)` |
| 戦闘演出の Tween を再生したい | `await BattleTween.Play(tweener, cancelToken)`（Client側ラッパー） |
| 再生中の Tween 全部の速度を変えたい | `tweenController.TimeScale = 2f;` |
| 演出を一時停止したい（グループ単位） | `TimeScale = 0f`（再開は元の値に戻す） |
| 管理下の Tween を全部止めたい | `KillAllTweeners()` |

## 主要クラス

| クラス | 種別 | 役割 |
|---|---|---|
| `TweenController` | sealed class（`LifetimeDisposable` 継承・非MonoBehaviour） | Tweener の await 再生・実行中リスト管理・TimeScale 一括反映・一括 Kill |

### Client側ラッパー

| クラス | 場所 | 役割 |
|---|---|---|
| `BattleTween` | `Client/Assets/Scripts/Client/Battle/Utility/BattleTween.cs` | 戦闘用 Singleton。`static Play()` と `TweenController` 公開。OnRelease で `KillAllTweeners()` |

## 使い方(実例)

### 実例1: Client側ラッパー（Singleton 化と解放）

```csharp
// 引用元: Client/Assets/Scripts/Client/Battle/Utility/BattleTween.cs
public sealed class BattleTween : Singleton<BattleTween>
{
    public TweenController TweenController { get; private set; }

    protected override void OnCreate()
    {
        TweenController = new TweenController();
    }

    protected override void OnRelease()
    {
        TweenController.KillAllTweeners();
    }

    public static async UniTask Play(Tweener tweener, CancellationToken cancelToken = default)
    {
        await Instance.TweenController.Play(tweener, cancelToken);
    }
}
```

### 実例2: Tween の作成 → Play（SetLink 併用が定石）

```csharp
// 引用元: Client/Assets/Scripts/Client/Scene/Battle/View/BattleLog/BattleLogItemView.cs（抜粋）
slideTweener = rectTransform.DOAnchorPosY(targetY, SlideDuration).SetEase(Ease.OutCubic).SetLink(gameObject);

BattleTween.Play(slideTweener, cancelToken).Forget();
```

```csharp
// 同ファイル（await する例）.
var fadeIn = DOTween.ToAlpha(() => logText.Text.color, color => logText.Text.color = color, originAlpha, FadeInDuration).SetLink(gameObject);

await BattleTween.Play(fadeIn, cancelToken);
```

### 実例3: 演出速度の一括反映（戦闘倍速・入力ロック）

```csharp
// 引用元: Client/Assets/Scripts/Client/Battle/Core/Manager/BattleManager.cs
// 再生中の演出 (バトルログ等) にも即時反映.
if (!IsPaused && BattleTween.Exists)
{
    BattleTween.Instance.TweenController.TimeScale = GetSpeedScale();
}
```

```csharp
// 引用元: Client/Assets/Scripts/Client/Scene/Battle/Manager/BattleControlManager.cs
battleTween.TweenController.TimeScale = IsInputLocked ? 0f : BattleManager.GetSpeedScale();
```

## API(主要公開メンバー)

### TweenController

| メンバー | 説明 |
|---|---|
| `TweenController()` | コンストラクタ。内部で `TimeScale`（Modules.TimeUtil）を生成し変更を購読 |
| `Play(Tweener tweener, CancellationToken cancelToken = default) : UniTask` | 一旦 Pause → 現在の TimeScale を適用 → Play → 完了/Kill まで await。キャンセルは握りつぶし、その他例外は `Debug.LogException`。終了後リストから除去 |
| `TimeScale : float` | 再生速度倍率。set で**実行中の全 tweener.timeScale に即時反映**（0 で停止、以後 Play するものにも適用） |
| `KillAllTweeners()` | 管理下の Tween を全て `Kill()`（Complete はしない）してリストをクリア |

## 注意点・罠

- **namespace は `Modules.Tweening`**。`using Modules.DoTween` ではない（フォルダ名と不一致）
- `Play()` は渡された Tweener を一度 `Pause()` してから再生する。**自動再生済みの Tween を渡しても最初から意図通り制御される**が、`Play()` を通さない Tween は TimeScale 制御の対象外
- Tween 作成時は `SetLink(gameObject)` を付けるのが実例準拠（GameObject 破棄時に自動 Kill され、`Play()` の await も解ける）。付けないと破棄済みオブジェクトを対象に Tween が走り続ける恐れ
- `KillAllTweeners()` は **Complete させない**（途中の値のまま停止）。終了値を保証したい演出は個別に `Complete()` を検討
- `TimeScale = 0` は「一時停止」。DOTween 側の `Pause()` は使っていないので、再開は値を戻すだけでよい
- `TweenController` は `LifetimeDisposable` 派生。使い捨てにせず、所有者（Singleton 等）が寿命管理し、解放時に `KillAllTweeners()` を呼ぶ（BattleTween.OnRelease 参照）
- 戦闘演出で新規に Tween を使う場合は **直接 DOTween を再生せず `BattleTween.Play()` を通す**こと（戦闘倍速・入力ロック停止に連動させるため。`Time.timeScale` は使わない方針）

## 関連

- [TimeUtil](TimeUtil.md) — `TimeScale`（値変更通知付きの倍率ホルダ。本モジュールの速度制御の実体）
- [UniTask](UniTask.md) — `tweener.ToUniTask()`（`UNITASK_DOTWEEN_SUPPORT` による DOTween 連携）
- [Extensions/Core.md](../Extensions/Core.md) — `LifetimeDisposable` / `Singleton<T>`
