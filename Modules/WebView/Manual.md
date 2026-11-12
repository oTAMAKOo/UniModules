# WebView

アプリ内で Web ページを表示する機能。
URL や HTML の読み込み、表示と非表示の切り替え、読み込み完了やエラーの通知ができる。
UniWebView か EmbeddedBrowser の導入と、対応するシンボルの定義が必要。

## 基本情報

> **namespace**: `Modules.WebView`
> **場所**: `Modules/WebView/`（実装 Content は `Content/`）
> **依存**: UniTask / R3 / Extensions。実装 Content は UniWebView（`#if ENABLE_UNIWEBVIEW`）/ ZenFulcrum EmbeddedBrowser（`#if ENABLE_EMBEDDEDBROWSER`）。シンボルは利用側で定義

## 概要

アプリ内 WebView（お知らせ・利用規約表示等を想定）の抽象化基盤。基底2クラス（`WebViewContent` / `WebViewObject`）+ WebView 製品ごとの実装 Content（`UniWebViewContent` / `EmbeddedBrowserContent`）の構成。

実装 Content は `#if ENABLE_UNIWEBVIEW` / `#if ENABLE_EMBEDDEDBROWSER` で囲まれており、利用側で該当シンボル + SDK が未導入の場合はコンパイル対象外になる。基底2クラスのみ常時コンパイルされる。

主要クラス: `WebViewObject`（abstract の MonoBehaviour。利用側の窓口。`GetContentPrefab()` で返した Prefab から `WebViewContent` を生成して子に持つ）/ `WebViewContent`（abstract の MonoBehaviour。製品ごとの実装の基底。読み込み状態とイベントを持つ）/ `UniWebViewContent`（UniWebView 実装）/ `EmbeddedBrowserContent`（EmbeddedBrowser 実装）/ `CustomEmbeddedBrowser`（`Browser` 派生。非アクティブ中も更新を回す）。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| このモジュールを有効化したい | WebView SDK（UniWebView か EmbeddedBrowser）導入 + `ENABLE_UNIWEBVIEW` 等シンボル定義 + `WebViewObject` 派生実装（`GetContentPrefab()` で Content の Prefab を返す） |
| URL を読み込む | `await WebViewObject.Load(url)`（未初期化なら `Initialize()` も行う） |
| HTML 文字列を読み込む | `await WebViewObject.LoadHTML(html, url)` |
| 表示・非表示・再読み込み・中止 | `WebViewObject.Show()` / `Hide()` / `Reload()` / `Stop()` |
| 読み込みの前後に処理を挟む | `WebViewObject` 派生で `OnInitialize()` / `OnLoad(url)` / `OnStop()` を override、または `OnLoadAsObservable()` |
| 読み込み完了・エラー・タイムアウト・クローズを受け取る | `WebViewContent.OnLoadCompleteAsObservable()` / `OnLoadErrorAsObservable()` / `OnLoadTimeOutAsObservable()` / `OnCloseAsObservable()`（派生内から `webViewContent` 経由） |
| ページからのメッセージを受け取る | `WebViewContent.OnReceivedMessageAsObservable()`（UniWebView は `UniWebViewMessage` が流れる） |
| 読み込みのタイムアウト・リトライを変える | `WebViewContent.TimeOutSeconds`（既定5）/ `RetryCount`（既定3）/ `RetryDelaySeconds`（既定1） |

## 注意点・罠

- 基底クラスだけ存在しても実装 Content が無いため実質使用不能。利用側で SDK 導入 + 派生実装が必須
- 実装 Content はシンボルに加えてプラットフォームの条件がある。`UniWebViewContent` の中身は iOS / Android の実機と macOS のエディタだけで有効で、それ以外ではクラスだけが残り基底の空実装になる。`EmbeddedBrowserContent` / `CustomEmbeddedBrowser` はエディタと Windows / macOS のスタンドアロンでだけコンパイルされ、中身は Windows（エディタ・スタンドアロン）だけで有効
- `EmbeddedBrowserContent` の Prefab には `Browser` ではなく `CustomEmbeddedBrowser` を付ける（`Initialize` が `CustomEmbeddedBrowser` として扱う）
- EmbeddedBrowser は **`Load` の読み込み完了待ちができない**（ソースコメント記載）。読み込み時間が問題になる場合は `LoadHTML` を使う
- 両実装とも `"webview"` URL スキームで JS 連携する設計（UniWebView: `AddUrlScheme("webview")` / EmbeddedBrowser: `webview://back` でクローズ、`openurl` で外部ブラウザ起動）
- `WebViewObject.CreateWebViewContent` は Awake を走らせるため一旦ルート階層に生成してから自身の子に付け替える
- `UniWebViewContent` は RectTransform 追従を **毎フレーム再代入**で実現（`ReferenceRectTransform` が代入時点の値しか見ないため）
- `WebViewObject.OnStopAsObservable()` は、`Stop()` で `OnStop()` と Content の停止を行ったあとに発火する
- `WebViewObject` の `webViewContent` は protected。イベントの購読や `Loading` の参照は派生クラスの中から行う

## 関連

- [Network](../Network/Manual.md) — HTTP クライアント
- [CriWare](../CriWare/Manual.md) / [Vivox](../Vivox/Manual.md) — 同じく「SDK 導入 + シンボル定義で有効化」する条件付きコンパイルモジュール
