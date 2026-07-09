# Renderer2D

> **namespace**: `Modules.Renderer2D.DummyContent`（フォルダは `DummyContents/`・単複不一致）
> **場所**: `Client/Assets/UniModules/Scripts/Modules/Renderer2D/DummyContents/`
> **依存**: R3 / Extensions（`UnityUtility`, `FixedQueue`） / UnityEditor.U2D（エディタ partial）

## 概要

`SpriteRenderer` 用の**エディタ専用ダミー画像機構** `DummySprite`。開発中はエディタ上でだけダミー Sprite を表示し、**シーン・プレハブ・ビルドには画像参照を一切含めない**（guid + spriteId の文字列だけ保存）。
実行時は「ダミー登録済みなのに sprite が null」の間 SpriteRenderer を自動非表示にする。uGUI `Image` 用の同名クラス `Modules.UI.DummyContent.DummySprite`（[UI](UI.md) 参照）の SpriteRenderer 版。

主要クラス: `DummySprite`（sealed partial MonoBehaviour。エディタ処理は `DummySprite.editor.cs` に分離）/ `DummySpriteInspector`（エディタ専用。ObjectField で Sprite を選び assetGuid / spriteId を保存）。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| SpriteRenderer に開発用の仮画像を出したい（ビルド非含有） | `DummySprite` をアタッチし、インスペクタの DummySprite 欄に Sprite を設定 |
| 実行時に本物の画像を差し込みたい | `dummySprite.SpriteRenderer.sprite = loadedSprite`（差し込むまで自動非表示） |
| uGUI Image で同じことをしたい | `Modules.UI.DummyContent.DummySprite`（[UI](UI.md)） |

## 使い方

対象の SpriteRenderer プレハブに `DummySprite` コンポーネントを付与し、インスペクタで参照する Sprite を設定するだけ（コード側の対応は不要）。

## 注意点・罠

- **`Modules.UI.DummyContent.DummySprite`（Image 用）と同名クラス**。using 次第で衝突するため、混在コードでは完全修飾で区別する
- ダミー画像はビルドに含まれない（`BuildPipeline.isBuildingPlayer` 中はスキップ）。**実行時の画像設定は呼び出し側の責務**（未設定のままだと非表示のまま）
- 実行時の自動非表示は「assetGuid が登録されている場合」のみ。ダミー未登録の SpriteRenderer には何もしない
- ダミー Sprite は `Sprite.Create` の複製（FullRect / pivot 正規化済み）。エディタ上の見た目確認用であり、元 Sprite とはインスタンスが別
- 設定値（`assetGuid` / `spriteId`）は private シリアライズフィールドで、`DummySpriteInspector` が Reflection 経由で書き込む。コードから設定する API はない
- モジュール内容は現状 `DummyContents/` のみ（"Renderer2D" という名前だが汎用 2D 描画基盤ではない）

## 関連

- [UI](UI.md) — uGUI 版 `DummySprite` / `DummyText`（`Modules.UI.DummyContent`）と同機構
- [ExternalAsset](ExternalAsset.md) — 実行時に本物の Sprite をロードする手段
