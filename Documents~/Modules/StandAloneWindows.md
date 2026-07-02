# StandAloneWindows

> **namespace**: `Modules.StandAloneWindows`
> **場所**: `Client/Assets/UniModules/Scripts/Modules/StandAloneWindows/`
> **Client側使用**: 0ファイル（2026-07時点・未使用。基盤内からの参照もなし）
> **依存**: UniTask / R3 / Extensions（`Singleton<T>`）/ AOT / Win32 API（user32.dll を P/Invoke）

## 概要

**Windows スタンドアロンビルド専用**のネイティブウィンドウ制御基盤。ウィンドウハンドル取得、リサイズ時のアスペクト比固定（WndProc フック）、ウィンドウスタイル（枠・最大化ボタン等）の変更を行う。
全ファイルが `#if UNITY_STANDALONE_WIN` で囲まれており、**モバイルビルド・他プラットフォームではコンパイル対象外**（クラス自体が存在しない）。本プロジェクト（モバイル向け）では未使用。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| 自プロセスのウィンドウハンドル(HWND)を取りたい | `WindowHandle.WindowTitle = "..."` → `WindowHandle.Get()` |
| ウィンドウリサイズ中もアスペクト比を固定したい | `AspectRatioHandler.Instance.Initialize()` |
| 解像度変更を購読したい | `AspectRatioHandler.Instance.OnResolutionChangedAsObservable()` |
| ウィンドウの枠・ボタン等のスタイルを変えたい | `WindowStyleHandler.Instance`（`WindowStyle` 設定 → `Apply()`） |

## 主要クラス

| クラス | 種別 | 役割 |
|---|---|---|
| `WindowHandle` | static | `WindowTitle` と一致する自プロセスのウィンドウハンドルを `EnumWindows` で検索・保持。`GetWindowLong` / `SetWindowLong`（32/64bit 自動分岐）のラッパー |
| `AspectRatioHandler` | Singleton（`Extensions.Singleton<T>`） | WndProc をフックし `WM_SIZING` でリサイズ矩形を補正してアスペクト比を維持。フルスクリーン切替も監視 |
| `AspectRatioHandler.ResolutionChangeInfo` | class（ネスト） | 解像度変更通知（`Width` / `Height` / `FullScreen`） |
| `WindowStyleHandler` | Singleton（`Extensions.Singleton<T>`） | ウィンドウスタイル（`GWL_STYLE`）の取得・適用。毎フレーム監視して外部変更を上書き |
| `WindowStyles` | enum（`[Flags]` : uint） | Win32 の `WS_OVERLAPPED` / `WS_POPUP` / `WS_CAPTION` 等のスタイル定数定義 |

## 使い方(最小の想定例)

Client側・基盤内に使用実績がないため想定例（`AspectRatioHandler` のデフォルトは 16:9、最小 512x512・最大 2048x2048）。

```csharp
// 想定例（本プロジェクトに実使用コードなし）.
// ハンドル検索はウィンドウタイトル一致で行うため最初に必ず設定する.
WindowHandle.WindowTitle = Application.productName;

var aspectRatioHandler = AspectRatioHandler.Instance;

aspectRatioHandler.SetAspectRatio(16, 9);
aspectRatioHandler.SetMinSize(960, 540);
aspectRatioHandler.SetAllowFullscreen(false);
aspectRatioHandler.Initialize();

aspectRatioHandler.OnResolutionChangedAsObservable()
    .Subscribe(x => Debug.Log($"{x.Width}x{x.Height} FullScreen={x.FullScreen}"))
    .AddTo(aspectRatioHandler.Disposable);

// ウィンドウスタイル変更（リサイズ枠と最大化ボタンを外す例）.
var windowStyleHandler = WindowStyleHandler.Instance;

windowStyleHandler.Initialize();
windowStyleHandler.WindowStyle = (int)(WindowStyles.WS_CAPTION | WindowStyles.WS_SYSMENU | WindowStyles.WS_MINIMIZEBOX | WindowStyles.WS_VISIBLE);
windowStyleHandler.Apply();
```

## API(主要公開メンバー)

### WindowHandle（static）

| メンバー | 説明 |
|---|---|
| `string WindowTitle` | 検索対象のウィンドウタイトル。`Get()` 前に設定必須 |
| `IntPtr Get()` | ハンドル取得（初回のみ `EnumWindows` で検索しキャッシュ） |
| `IntPtr GetWindowLong(int nIndex)` / `IntPtr SetWindowLong(int nIndex, IntPtr dwNewLong)` | user32 ラッパー（Set は 32/64bit を自動分岐） |

### AspectRatioHandler（Singleton）

| メンバー | 説明 |
|---|---|
| `void Initialize()` | WndProc フック + 監視ループ開始。エディタ実行時は no-op |
| `void SetAspectRatio(float w, float h)` / `float AspectRatio` | 維持するアスペクト比の設定 / 取得 |
| `void SetMinSize(int w, int h)` / `void SetMaxSize(int w, int h)` | リサイズ可能なピクセル範囲 |
| `void SetAllowFullscreen(bool)` | false ならフルスクリーン化を毎フレーム強制解除 |
| `void Apply()` | 現在幅を基準に `Screen.SetResolution` を即時適用 |
| `Observable<ResolutionChangeInfo> OnResolutionChangedAsObservable()` | 解像度変更通知（R3） |

### WindowStyleHandler（Singleton）

| メンバー | 説明 |
|---|---|
| `void Initialize()` | 現在スタイルを取得し監視ループ開始。エディタ実行時は no-op |
| `int WindowStyle`（get/set） | 適用したいスタイル値（`WindowStyles` フラグの組み合わせ） |
| `IntPtr GetStyle()` | 現在の `GWL_STYLE` 取得 |
| `void Apply()` | `WindowStyle` を即時適用（`SetWindowPos` で再描画） |

## 注意点・罠

- `#if UNITY_STANDALONE_WIN` 専用。**このプロジェクト（モバイル）では参照するとコンパイルエラー**になるため、使う場合は呼び出し側も同シンボルで囲む
- `WindowHandle.WindowTitle` 未設定のまま `Get()` を呼ぶと `ArgumentException`
- 両 Handler の `Initialize()` は `Application.isEditor` で即 return（**エディタでは動作確認不可**）
- `AspectRatioHandler` は WndProc を差し替える。他のネイティブフックと競合注意（`Application.quitting` で元の WndProc に復元）
- 両 Handler とも `UniTask.DelayFrame(1)` の常駐監視ループを持つ（停止はアプリ終了時のみ）
- `WindowStyleHandler` は外部からスタイルが変えられても毎フレーム `WindowStyle` の値へ上書きし返す

## 関連

- [Camera](Camera.md) — `FixedAspectCamera`（描画側でアスペクト比を固定。こちらは全プラットフォーム対応）
