# Renderer2D

> **namespace**: `Modules.Renderer2D.DummyContent`（フォルダは `DummyContents/`・単複不一致）
> **場所**: `Client/Assets/UniModules/Scripts/Modules/Renderer2D/DummyContents/`
> **Client側使用**: .cs 参照 0ファイル / prefab 1件（2026-07時点）
> **依存**: R3 / Extensions（`UnityUtility`, `FixedQueue`） / UnityEditor.U2D（エディタ partial）

## 概要

`SpriteRenderer` 用の**エディタ専用ダミー画像機構** `DummySprite`。開発中はエディタ上でだけダミー Sprite を表示し、**シーン・プレハブ・ビルドには画像参照を一切含めない**（guid + spriteId の文字列だけ保存）。
実行時は「ダミー登録済みなのに sprite が null」の間 SpriteRenderer を自動非表示にする。uGUI `Image` 用の同名クラス `Modules.UI.DummyContent.DummySprite`（[UI](UI.md) 参照）の SpriteRenderer 版。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| SpriteRenderer に開発用の仮画像を出したい（ビルド非含有） | `DummySprite` をアタッチし、インスペクタの DummySprite 欄に Sprite を設定 |
| 実行時に本物の画像を差し込みたい | `dummySprite.SpriteRenderer.sprite = loadedSprite`（差し込むまで自動非表示） |
| uGUI Image で同じことをしたい | `Modules.UI.DummyContent.DummySprite`（[UI](UI.md)） |

## 主要クラス

| クラス | 種別 | 役割 |
|---|---|---|
| `DummySprite` | sealed partial MonoBehaviour（`[ExecuteAlways]` `[RequireComponent(typeof(SpriteRenderer))]`） | ダミー表示本体。エディタ処理は `DummySprite.editor.cs`（`#if UNITY_EDITOR`）に分離 |
| `DummySpriteInspector` | エディタ専用（`Editor/`、`ScriptlessEditor` 継承・複数編集可） | ObjectField で Sprite を選び、assetGuid / spriteId（`GetSpriteID()`）を保存 |

## 動作仕様

| タイミング | 挙動 |
|---|---|
| エディタ（非再生） | OnEnable で assetGuid + spriteId から Sprite を検索し、`Sprite.Create` で**複製**を生成して表示（name = `"*Sprite (DummyAsset)"`、`DontSaveInEditor`）。OnDisable で破棄。生成 Sprite は static な `FixedQueue`（250件）でキャッシュ |
| ビルド中 | `BuildPipeline.isBuildingPlayer` 中はスキップ（ダミーはビルドに含まれない） |
| 実行時 | assetGuid 登録済みの場合、`sprite` を `ObserveEveryValueChanged` で監視し **null なら `SpriteRenderer.enabled = false`**、差し込まれたら true |

## 使い方(実例)

実使用は prefab 設定のみ（`Client/Assets/Resource (Internal)/Scene/WorldMap/Prefab/Map/Tile.prefab` の SpriteRenderer にアタッチ）。コードから触る場合の想定例:

```csharp
// 想定例（実運用はインスペクタ設定のみ）.
// DummySprite 付きの SpriteRenderer は実行時 sprite == null の間は自動非表示.
// 実画像をロードして差し込むと表示される.
var spriteRenderer = dummySprite.SpriteRenderer;

spriteRenderer.sprite = await LoadTileSprite(tileId);
```

## API(主要公開メンバー)

### DummySprite

| メンバー | 説明 |
|---|---|
| `SpriteRenderer SpriteRenderer` | 対象 SpriteRenderer（遅延 GetComponent） |
| `const string DummyAssetName`（エディタ partial） | 生成ダミー Sprite の名前 `"*Sprite (DummyAsset)"`。この名前でダミー判定する |

設定値（`assetGuid` / `spriteId`）は private シリアライズフィールドで、`DummySpriteInspector` が Reflection 経由で書き込む。コードから設定する API はない。

## 注意点・罠

- **`Modules.UI.DummyContent.DummySprite`（Image 用）と同名クラス**。using 次第で衝突するため、混在コードでは完全修飾で区別する
- ダミー画像はビルドに含まれない。**実行時の画像設定は呼び出し側の責務**（未設定のままだと非表示のまま）
- 実行時の自動非表示は「assetGuid が登録されている場合」のみ。ダミー未登録の SpriteRenderer には何もしない
- ダミー Sprite は `Sprite.Create` の複製（FullRect / pivot 正規化済み）。エディタ上の見た目確認用であり、元 Sprite とはインスタンスが別
- モジュール内容は現状 `DummyContents/` のみ（"Renderer2D" という名前だが汎用 2D 描画基盤ではない）

## 関連

- [UI](UI.md) — uGUI 版 `DummySprite` / `DummyText`（`Modules.UI.DummyContent`）と同機構
- [ExternalAsset](ExternalAsset.md) — 実行時に本物の Sprite をロードする手段
