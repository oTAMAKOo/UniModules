# SpriteAnimation

> **namespace**: `Modules.SpriteAnimation`
> **場所**: `Client/Assets/UniModules/Scripts/Modules/SpriteAnimation/`
> **Client側使用**: 0ファイル（2026-07時点。プレハブ/シーンからの参照も無し）
> **依存**: R3 / Extensions / Modules.Cache（`SpriteAtlasCache`） / UnityEngine.U2D（`SpriteAtlas`）

## 概要

SpriteAtlas 内の連番スプライト（`{アニメ名}_0`, `{アニメ名}_1`, ...）を一定間隔で切り替えるパラパラアニメ（フリップブック）基盤。Animator を使うほどでもない UI の簡易コマアニメ向け。

**コンパイル対象**（シンボルゲート無し・外部SDK不要）で使用可能な状態だが、本プロジェクトでは Client コード・アセットともに未使用。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| uGUI の `Image` でコマアニメを再生したい | `SpriteAnimationForUI`（Image と同じ GameObject に付与） |
| 表示先を自分で制御してコマアニメしたい（SpriteRenderer 等） | `SpriteAnimation` + `OnUpdateAnimationAsObservable()` を購読して自前で適用 |
| 再生 / 停止 | `Play(animationName, startIndex, loop)` / `Stop()` |
| コマ送り速度を変えたい | `UpdateInterval`（**1コマの表示秒数**） |
| 最終コマ到達を検知したい | `OnLastFrameAsObservable()` |

## 主要クラス

| クラス | 種別 | 役割 |
|---|---|---|
| `SpriteAnimation` | MonoBehaviour | 本体。`Update()` で `UpdateInterval` 秒ごとに次のコマの `Sprite` を `OnUpdateAnimationAsObservable` へ流す。**自分では描画に反映しない**（購読側が適用する設計） |
| `SpriteAnimationForUI` | sealed MonoBehaviour（`SpriteAnimation` 派生、`[RequireComponent(typeof(Image))]`） | `OnEnable` で `Image.sprite` への適用を購読。`playOnEnable` で有効化時に自動再生も可 |

## 使い方(実例)

Client側の使用実績が無いため最小の想定例。

```csharp
// 想定例（Client側使用実績なし）.
using Modules.SpriteAnimation;

// --- uGUI Image の場合: SpriteAnimationForUI を Image と同じ GameObject に付与し、
// inspector で spriteAtlas / updateInterval / animationName / playOnEnable を設定するだけでもよい.

[SerializeField]
private SpriteAnimationForUI spriteAnimation = null;

public void Setup()
{
    spriteAnimation.UpdateInterval = 0.1f;  // 1コマ0.1秒.

    // アトラス内の "attack_0", "attack_1", ... を順に再生.
    spriteAnimation.Play("attack", startIndex: 0, loop: false);

    // 最終コマ到達（非ループなら停止直前）の検知.
    spriteAnimation.OnLastFrameAsObservable()
        .Subscribe(_ => OnAnimationEnd())
        .AddTo(this);
}
```

```csharp
// 想定例: 表示先を自前制御する場合（SpriteRenderer 等）.
spriteAnimation.OnUpdateAnimationAsObservable()
    .Subscribe(sprite => spriteRenderer.sprite = sprite)
    .AddTo(this);

spriteAnimation.Play("walk");
```

## API(主要公開メンバー)

### SpriteAnimation

| メンバー | 説明 |
|---|---|
| `Play(string animationName, int? startIndex = null, bool loop = true)` | 再生開始。アトラスから `{animationName}_{n}` を 0 から順に探してコマ数を確定し、`startIndex`（省略時0）のコマから開始 |
| `Stop()` | 停止（`CurrentState = Stop`。コマ位置はそのまま） |
| `SpriteAtlas : SpriteAtlas` | 対象アトラス（SerializeField。コードからの差し替えも可） |
| `UpdateInterval : float` | 1コマの表示秒数（SerializeField、**既定 1f = 1秒/コマ**） |
| `Loop : bool` / `CurrentState : State`（Play/Stop） / `CurrentAnimation` / `AnimationName` | 状態の参照・ループ切替 |
| `OnUpdateAnimationAsObservable() : Observable<Sprite>` | コマ切替のたびに表示すべき `Sprite` を通知 |
| `OnLastFrameAsObservable() : Observable<Unit>` | 最終コマの表示時間が終わり先頭へ折り返すタイミングで通知 |

### SpriteAnimationForUI（追加分）

| メンバー | 説明 |
|---|---|
| `SetAnimationName(string)` / `SetLoop(bool)` | `playOnEnable` 用の再生設定を差し替え（再生中のものは変わらない） |
| （SerializeField）`playOnEnable` / `animationName` / `startIndex` / `loop` | 有効化時の自動再生設定 |

## 注意点・罠

- **`UpdateInterval` はフレームレートではなく「1コマの秒数」**。既定値が 1f（1秒/コマ）なので、設定し忘れると異常に遅いアニメになる。
- スプライト命名規約は `{アニメ名}_{連番}`（0始まり、例: `attack_0`）。連番が途切れた所までがコマ数になる（`Play` 時に `GetSprite` を 0 から順に叩いて数える）。
- `SpriteAnimation` 単体は描画コンポーネントに触らない。`OnUpdateAnimationAsObservable()` を購読しないと何も表示されない（uGUI は `SpriteAnimationForUI` が購読を担う）。SpriteRenderer 向けの派生は無いので購読で自作する。
- **非ループ再生は最後のコマの後、先頭コマ（index 0）を表示した状態で停止する**（末尾コマでは止まらない）。末尾で止めたい場合は `OnLastFrameAsObservable` で自前制御が必要。
- `SpriteAnimationForUI` は `OnEnable` 駆動（購読は `TakeUntilDisable`）。非アクティブ→アクティブで購読し直す。`playOnEnable = false` の場合は手動で `Play` を呼ぶ。
- `Play` のたびに `SpriteAtlasCache` を生成し直す。頻繁にアニメ名を切り替える用途ではロードコストに注意（アトラスが同じなら `SpriteAtlasCache` はキャッシュから Sprite を返す）。
- 進行は `Update()` + `Time.deltaTime`（Unityライフサイクル駆動、`Time.timeScale` の影響を受ける）。

## 関連

- [Cache](Cache.md) — `SpriteAtlasCache`（アトラスからの Sprite 取得・キャッシュ）
- [Animation](Animation.md) — AnimationClip / Animator ベースの再生基盤（複雑なアニメはこちら）
- [UI](UI.md) — uGUI 基盤
