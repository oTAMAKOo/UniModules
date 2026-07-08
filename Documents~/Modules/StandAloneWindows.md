# StandAloneWindows

> **namespace**: `Modules.StandAloneWindows`
> **場所**: `Client/Assets/UniModules/Scripts/Modules/StandAloneWindows/`
> **Client側使用**: 0ファイル（2026-07時点・未使用。基盤内からの参照もなし）
> **依存**: UniTask / R3 / Extensions（`Singleton<T>`）/ AOT / Win32 API（user32.dll を P/Invoke）

## 概要

**Windows スタンドアロンビルド専用**のネイティブウィンドウ制御基盤。ウィンドウハンドル取得、リサイズ時のアスペクト比固定（WndProc フック）、ウィンドウスタイル（枠・最大化ボタン等）の変更を行う。
全ファイルが `#if UNITY_STANDALONE_WIN` で囲まれており、**モバイルビルド・他プラットフォームではコンパイル対象外**（クラス自体が存在しない）。本プロジェクト（モバイル向け）では未使用。

主要クラス: `WindowHandle`（static。`WindowTitle` と一致する自プロセスのウィンドウハンドルを検索・保持）/ `AspectRatioHandler`（Singleton。WndProc をフックし `WM_SIZING` でリサイズ矩形を補正）/ `WindowStyleHandler`（Singleton。`GWL_STYLE` の取得・適用）/ `WindowStyles`（Win32 スタイル定数の `[Flags]` enum）。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| 自プロセスのウィンドウハンドル(HWND)を取りたい | `WindowHandle.WindowTitle = "..."` → `WindowHandle.Get()` |
| ウィンドウリサイズ中もアスペクト比を固定したい | `AspectRatioHandler.Instance.Initialize()` |
| 解像度変更を購読したい | `AspectRatioHandler.Instance.OnResolutionChangedAsObservable()` |
| ウィンドウの枠・ボタン等のスタイルを変えたい | `WindowStyleHandler.Instance`（`WindowStyle` 設定 → `Apply()`） |

## 注意点・罠

- `#if UNITY_STANDALONE_WIN` 専用。**このプロジェクト（モバイル）では参照するとコンパイルエラー**になるため、使う場合は呼び出し側も同シンボルで囲む
- `WindowHandle.WindowTitle` 未設定のまま `Get()` を呼ぶと `ArgumentException`（ハンドル検索はウィンドウタイトル一致で行うため最初に必ず設定する）
- 両 Handler の `Initialize()` は `Application.isEditor` で即 return（**エディタでは動作確認不可**）
- `AspectRatioHandler` は WndProc を差し替える。他のネイティブフックと競合注意（`Application.quitting` で元の WndProc に復元）
- 両 Handler とも `UniTask.DelayFrame(1)` の常駐監視ループを持つ（停止はアプリ終了時のみ）
- `WindowStyleHandler` は外部からスタイルが変えられても毎フレーム `WindowStyle` の値へ上書きし返す

## 関連

- [Camera](Camera.md) — `FixedAspectCamera`（描画側でアスペクト比を固定。こちらは全プラットフォーム対応）
