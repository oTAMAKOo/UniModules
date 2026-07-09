# WebView

> **namespace**: `Modules.WebView`
> **場所**: `Client/Assets/UniModules/Scripts/Modules/WebView/`
> **依存**: UniTask / R3。実装 Content は UniWebView / ZenFulcrum EmbeddedBrowser

## 概要

アプリ内 WebView（お知らせ・利用規約表示等を想定）の抽象化基盤。基底2クラス（`WebViewContent` / `WebViewObject`）+ WebView 製品ごとの実装 Content（`UniWebViewContent` / `EmbeddedBrowserContent`）の構成。

実装 Content は `#if ENABLE_UNIWEBVIEW` / `#if ENABLE_EMBEDDEDBROWSER` で囲まれており、利用側で該当シンボル + SDK が未導入の場合はコンパイル対象外になる。基底2クラスのみ常時コンパイルされる。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| このモジュールを有効化したい | WebView SDK（UniWebView か EmbeddedBrowser）導入 + `ENABLE_UNIWEBVIEW` 等シンボル定義 + `WebViewObject` 派生実装 |

## 注意点・罠

- 基底クラスだけ存在しても実装 Content が無いため実質使用不能。利用側で SDK 導入 + 派生実装が必須
- EmbeddedBrowser は **`Load` の読み込み完了待ちができない**（ソースコメント記載）。読み込み時間が問題になる場合は `LoadHTML` を使う
- 両実装とも `"webview"` URL スキームで JS 連携する設計（UniWebView: `AddUrlScheme("webview")` / EmbeddedBrowser: `webview://back` でクローズ、`openurl` で外部ブラウザ起動）
- `WebViewObject.CreateWebViewContent` は Awake を走らせるため一旦ルート階層に生成してから自身の子に付け替える
- `UniWebViewContent` は RectTransform 追従を **毎フレーム再代入**で実現（`ReferenceRectTransform` が代入時点の値しか見ないため）

## 関連

- [Network](Network.md) — HTTP クライアント
- [CriWare](CriWare.md) / [Vivox](Vivox.md) — 同じく「SDK 導入 + シンボル定義で有効化」する休眠モジュール
