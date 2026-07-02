# WebView

> **namespace**: `Modules.WebView`
> **場所**: `Client/Assets/UniModules/Scripts/Modules/WebView/`
> **Client側使用**: 0ファイル（2026-07時点・未使用）
> **依存**: UniTask / R3（+ R3.Triggers） / Extensions。実装 Content は UniWebView / ZenFulcrum EmbeddedBrowser（**いずれも未同梱**）

## 概要

アプリ内 WebView（お知らせ・利用規約表示等を想定）の抽象化基盤。共通インターフェース（基底2クラス）と、WebView 製品ごとの実装 Content を分離した構成。
**実装 Content は `#if ENABLE_UNIWEBVIEW` / `#if ENABLE_EMBEDDEDBROWSER` で囲まれており、本プロジェクトでは両シンボル未定義 + SDK 未導入のためコンパイル対象外**。基底2クラス（`WebViewContent` / `WebViewObject`）のみ常時コンパイルされるが、Client 側の利用実績はない。

## 主要クラス

| クラス | 種別 | 役割 | コンパイル条件 |
|---|---|---|---|
| `WebViewContent` | abstract MonoBehaviour | WebView 実装の共通基底。`Load` / `LoadHTML` / `Stop` / `Show` / `Hide` / `Reload`（virtual）と読み込み完了・エラー・タイムアウト・メッセージ受信・クローズの Observable | 常時 |
| `WebViewObject` | abstract MonoBehaviour | 上位ラッパー。`GetContentPrefab()`（abstract）の prefab から Content を生成・所有し、同名 API を委譲 | 常時 |
| `UniWebViewContent` | class（`WebViewContent` 継承） | UniWebView（有料アセット）実装。モバイル実機 + macOS エディタ向け。RectTransform 追従・"webview" URL スキーム | `ENABLE_UNIWEBVIEW` |
| `EmbeddedBrowserContent` | class（`WebViewContent` 継承） | ZenFulcrum EmbeddedBrowser 実装。Windows スタンドアロン + Windows エディタ向け | `ENABLE_EMBEDDEDBROWSER` |
| `CustomEmbeddedBrowser` | class（`Browser` 継承） | 非アクティブ中も `Update` / `LateUpdate` を強制実行する EmbeddedBrowser 拡張 | `ENABLE_EMBEDDEDBROWSER` |

## 使い方(最小の想定例)

Client側に使用実績がないため想定例。`WebViewObject` を派生して Content prefab（実装 Content + SDK コンポーネントを載せたもの）を返す。

```csharp
// 想定例（本プロジェクトに実使用コードなし）.
public sealed class NewsWebView : WebViewObject
{
    [SerializeField]
    private GameObject contentPrefab = null;

    protected override GameObject GetContentPrefab()
    {
        return contentPrefab;
    }
}

// 利用側: Load 内部で Initialize → Content 生成 → 読み込みまで行う.
await newsWebView.Load("https://example.com/news");

newsWebView.OnStopAsObservable()
    .Subscribe(_ => CloseWindow())
    .AddTo(this);
```

## API(主要公開メンバー)

### WebViewObject（abstract）

| メンバー | 説明 |
|---|---|
| `UniTask Initialize()` | Content prefab 生成 + `OnInitialize()`（virtual）。多重呼び出し安全 |
| `UniTask Load(string url)` / `UniTask LoadHTML(string html, string url = null)` | 読み込み（内部で Initialize 済み保証） |
| `void Stop()` / `void Show()` / `void Hide()` / `void Reload()` | Content へ委譲 |
| `OnLoadAsObservable()` / `OnStopAsObservable()` | 読み込み実行 / 停止イベント |

### WebViewContent（abstract）

| メンバー | 説明 |
|---|---|
| `bool Loading` / `int TimeOutSeconds`（既定5） / `int RetryCount`（既定3） / `int RetryDelaySeconds`（既定1） | 読み込み待ちの制御値 |
| `WaitForLoadFinish(url)`（protected） | `Loading` が false になるまで待機（タイムアウト + リトライ付き） |
| `OnLoadCompleteAsObservable()` / `OnLoadErrorAsObservable()` / `OnLoadTimeOutAsObservable()` | 読み込み結果イベント |
| `OnReceivedMessageAsObservable()` / `OnCloseAsObservable()` | JS からのメッセージ受信（`object`）/ クローズ通知 |

## 注意点・罠

- **コンパイル対象外（実装部）**。使用するには WebView SDK（UniWebView か ZenFulcrum EmbeddedBrowser）導入 + `Client/Assets/csc.rsp` へ `-define:ENABLE_UNIWEBVIEW` 等の追加が必要
- EmbeddedBrowser は **`Load` の読み込み完了待ちができない**（ソースコメントより。読み込み時間が問題になる場合は `LoadHTML` を使う）
- 両実装とも `"webview"` URL スキームで JS 連携する設計（UniWebView: `AddUrlScheme("webview")` / EmbeddedBrowser: `webview://back` でクローズ、`openurl` 関数で外部ブラウザ起動）
- `WebViewObject.CreateWebViewContent` は Awake を走らせるため一旦ルート階層に生成してから自身の子に付け替える
- `UniWebViewContent` は RectTransform 追従を **毎フレーム再代入**で実現（`ReferenceRectTransform` が代入時点の値しか見ないため）

## 関連

- [Network](Network.md) — API 通信（WebView ではなく HTTP クライアント）
- [CriWare](CriWare.md) / [Vivox](Vivox.md) — 同じく「シンボル + SDK 導入で有効化」する休眠モジュール
