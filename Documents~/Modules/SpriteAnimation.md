# SpriteAnimation

> **namespace**: `Modules.SpriteAnimation`
> **場所**: `Client/Assets/UniModules/Scripts/Modules/SpriteAnimation/`
> **Client側使用**: 0ファイル（2026-07時点。プレハブ/シーンからの参照も無し）
> **依存**: R3 / Extensions / Modules.Cache（`SpriteAtlasCache`） / UnityEngine.U2D（`SpriteAtlas`）

## 概要

SpriteAtlas 内の連番スプライト（`{アニメ名}_0`, `{アニメ名}_1`, ...）を一定間隔で切り替えるパラパラアニメ（フリップブック）基盤。Animator を使うほどでもない UI の簡易コマアニメ向け。

主要クラス: `SpriteAnimation`（本体。`Update()` で `UpdateInterval` 秒ごとに次のコマの `Sprite` を `OnUpdateAnimationAsObservable` へ流す。**自分では描画に反映しない**＝購読側が適用する設計）/ `SpriteAnimationForUI`（派生、`[RequireComponent(typeof(Image))]`。`OnEnable` で `Image.sprite` への適用を購読、`playOnEnable` で自動再生も可）。

**コンパイル対象**（シンボルゲート無し・外部SDK不要）で使用可能な状態だが、本プロジェクトでは Client コード・アセットともに未使用。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| uGUI の `Image` でコマアニメを再生したい | `SpriteAnimationForUI`（Image と同じ GameObject に付与） |
| 表示先を自分で制御してコマアニメしたい（SpriteRenderer 等） | `SpriteAnimation` + `OnUpdateAnimationAsObservable()` を購読して自前で適用 |
| 再生 / 停止 | `Play(animationName, startIndex, loop)` / `Stop()` |
| コマ送り速度を変えたい | `UpdateInterval`（**1コマの表示秒数**） |
| 最終コマ到達を検知したい | `OnLastFrameAsObservable()` |

## 注意点・罠

- **`UpdateInterval` はフレームレートではなく「1コマの秒数」**。既定値が 1f（1秒/コマ）なので、設定し忘れると異常に遅いアニメになる。
- スプライト命名規約は `{アニメ名}_{連番}`（0始まり、例: `attack_0`）。連番が途切れた所までがコマ数になる（`Play` 時に `GetSprite` を 0 から順に叩いて数える）。
- `SpriteAnimation` 単体は描画コンポーネントに触らない。`OnUpdateAnimationAsObservable()` を購読しないと何も表示されない（uGUI は `SpriteAnimationForUI` が購読を担う）。SpriteRenderer 向けの派生は無いので購読で自作する。
- **非ループ再生は最後のコマの後、先頭コマ（index 0）を表示した状態で停止する**（末尾コマでは止まらない）。末尾で止めたい場合は `OnLastFrameAsObservable` で自前制御が必要。
- `SpriteAnimationForUI` は `OnEnable` 駆動（購読は `TakeUntilDisable`）。非アクティブ→アクティブで購読し直す。`playOnEnable = false` の場合は手動で `Play` を呼ぶ。`SetAnimationName` / `SetLoop` は `playOnEnable` 用の再生設定の差し替えで、再生中のものは変わらない。
- `Play` のたびに `SpriteAtlasCache` を生成し直す。頻繁にアニメ名を切り替える用途ではロードコストに注意（アトラスが同じなら `SpriteAtlasCache` はキャッシュから Sprite を返す）。
- 進行は `Update()` + `Time.deltaTime`（Unityライフサイクル駆動、`Time.timeScale` の影響を受ける）。

## 関連

- [Cache](Cache.md) — `SpriteAtlasCache`（アトラスからの Sprite 取得・キャッシュ）
- [Animation](Animation.md) — AnimationClip / Animator ベースの再生基盤（複雑なアニメはこちら）
- [UI](UI.md) — uGUI 基盤
