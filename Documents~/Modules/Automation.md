# Automation

> **namespace**: `Modules.Automation.*`（サブ機能ごとに分割。現在は `Modules.Automation.PrefabPreview`）
> **場所**: `Scripts/Modules/Automation/`（サブ機能フォルダ直下の `Editor/` にエディタ専用コードを置く）
> **依存**: Extensions（`Scope` / `UnityUtility`）/ TextData（`TextDataLoader`）

## 概要

エディタ操作をコードから呼び出すための自動化モジュール。EditorWindow やメニューではなく、スクリプト・外部ツール・CI などプログラムから呼ばれることを前提に、結果をファイルや戻り値で返す static API を提供する。
人が GUI で使う開発支援ツール群（[Devkit](Devkit.md)）とは役割を分ける。

### PrefabPreview（`Modules.Automation.PrefabPreview`）

uGUI の Prefab を隔離したプレビューシーンに展開し、オフスクリーン描画して PNG や Texture2D を得る。**開いているシーン・Prefab Stage・アセットには一切書き込まない**。

| クラス | 役割 |
|---|---|
| `PrefabPreviewRenderer`（static） | `Render(prefabAssetPath または GameObject, outputFilePath, options)` で PNG を書き出し `PrefabPreviewResult` を返す |
| `PrefabPreviewScope`（`Extensions.Scope` 派生） | プレビューシーン・カメラ・キャンバス・Prefab インスタンスを生成し、`Dispose` で全て破棄する。`Instance` で展開したインスタンスを検査でき、`Render()` で Texture2D を得る |
| `PrefabPreviewOptions` | 解像度（既定 1920x1080）・背景色・生成直後フック |
| `PrefabPreviewResult` | Prefab のアセットパス・出力パス・解像度 |

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| Prefab の見た目を PNG に書き出したい | `PrefabPreviewRenderer.Render(assetPath, outputFilePath)` |
| Prefab を展開した状態で階層・コンポーネントを検査したい | `using (var scope = new PrefabPreviewScope(prefab)) { scope.Instance ... }` |
| 描画結果を Texture2D で受け取りたい | `scope.Render()`（戻り値は呼び出し側で `UnityUtility.SafeDelete(texture, true)`） |
| 生成直後に独自処理を挟みたい | `PrefabPreviewOptions.OnInstantiated` |
| 解像度・背景色を変えたい | `PrefabPreviewOptions.Width` / `Height` / `BackgroundColor` |

## 使い方

- 基本形: `PrefabPreviewRenderer.Render("Assets/.../Xxx.prefab", "<絶対パス>/Xxx.png")`。出力先ディレクトリは無ければ作成する。Prefab が見つからない場合は `Debug.LogError` を出して null を返す
- 検査と描画を同時に行う: `PrefabPreviewScope` を `using` で開き、`scope.Instance.GetComponentsInChildren<T>(true)` で列挙してから `scope.Render()`
- `RebuildLayout()` は生成時に一度実行済み。フックや検査でレイアウトを変更した場合は再度呼んでから `Render()` する

## 注意点・罠

- **描画されるのはディスク上の保存済み Prefab**。Prefab Stage で編集中の未保存内容は反映されない
- **描画は WorldSpace キャンバス + 正射影カメラ**（1px = 1unit、キャンバスサイズ = 解像度）。CanvasScaler は介在しないため、Screen Space キャンバス由来の縮尺は再現しない
- **カメラは `CameraType.Game`**。`CameraType.Preview` ではキャンバスの更新内容が描画に反映されない
- 生成時に `TextDataLoader.IsLoaded` が false なら `TextDataLoader.Reload()` を呼ぶ（ドメインリロード直後は未ロード）。`Reload()` は開いているシーン内の TextSetter へも文言を再適用する
- カメラとキャンバスは `HideFlags.HideAndDontSave` で生成し、直後にプレビューシーンへ移動する
- `Extensions.Scope` 派生のためファイナライザから `Dispose` されうるが、メインスレッド以外では後始末を行わない。**必ず `using` で使う**（漏れるとプレビューシーンが残る）
- `UnityEngine.SceneManagement.Scene` は `Modules.Scene` 名前空間と衝突するため、本モジュール内では完全修飾で書く
- エディタ専用（`Editor/` 配下）。ランタイムから参照しない

## 関連

- [Devkit](Devkit.md) — 人が GUI から使う開発支援ツール群（本モジュールはプログラムから呼ぶ側）
- [TextData](TextData.md) — `TextDataLoader`（生成時の文言解決）
- [../Extensions/Core.md](../Extensions/Core.md) — `Scope` / `UnityUtility`
