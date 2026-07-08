# WebView

> **namespace**: `Modules.WebView`
> **場所**: `Client/Assets/UniModules/Scripts/Modules/WebView/`
> **Client側使用**: 0ファイル（2026-07時点・未使用）
> **依存**: UniTask / R3。実装 Content は UniWebView / ZenFulcrum EmbeddedBrowser（**いずれも未同梱**）

## 概要

アプリ内 WebView（お知らせ・利用規約表示等を想定）の抽象化基盤。基底2クラス（`WebViewContent` / `WebViewObject`）+ WebView 製品ごとの実装 Content（`UniWebViewContent` / `EmbeddedBrowserContent`）の構成。

**実装 Content は `#if ENABLE_UNIWEBVIEW` / `#if ENABLE_EMBEDDEDBROWSER` で囲まれており、本プロジェクトでは両シンボル未定義 + SDK 未導入のためコンパイル対象外**。基底2クラスのみ常時コンパイルされるが Client 側の利用実績もなし。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| このモジュールを有効化したい | WebView SDK（UniWebView か EmbeddedBrowser）導入 + `csc.rsp` に `-define:ENABLE_UNIWEBVIEW` 等を追加 + `WebViewObject` 派生実装 |
| 使いたい情報を Web で表示したい（本プロジェクト） | このモジュールは使えない。設計相談 |

## 注意点・罠

- **コンパイル対象外**。基底クラスだけ存在しても実装 Content が無いため実質使用不能
- EmbeddedBrowser は **`Load` の読み込み完了待ちができない**（ソースコメント記載）。読み込み時間が問題になる場合は `LoadHTML` を使う
- 両実装とも `"webview"` URL スキームで JS 連携する設計（UniWebView: `AddUrlScheme("webview")` / EmbeddedBrowser: `webview://back` でクローズ、`openurl` で外部ブラウザ起動）
- `WebViewObject.CreateWebViewContent` は Awake を走らせるため一旦ルート階層に生成してから自身の子に付け替える
- `UniWebViewContent` は RectTransform 追従を **毎フレーム再代入**で実現（`ReferenceRectTransform` が代入時点の値しか見ないため）

## 関連

- [Network](Network.md) — HTTP クライアント
- [CriWare](CriWare.md) / [Vivox](Vivox.md) — 同じく「シンボル + SDK 導入で有効化」する休眠モジュール
