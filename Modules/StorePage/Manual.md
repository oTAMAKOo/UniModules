# StorePage

アプリのストアページを開く機能。
アプリの識別子からストアの URL を作り、ストアアプリで開くことができる。
iOS と Android の実機でのみ動く。

## 基本情報

> **namespace**: `Modules.StorePage`
> **場所**: `Modules/StorePage/StorePage.cs`（1ファイルのみ）
> **依存**: UnityEngine のみ

## 概要

アプリのストアページ（Google Play / App Store）を開くだけの極小 static クラス `StorePage`（唯一のクラス。ストアURL組み立てと `Application.OpenURL` 呼び出しのみ）。
主用途は**強制アップデートダイアログ等の「ストアを開く」ボタン**。ストアアプリを直接起動する URL スキーム（`market://` / `itms-apps://`）を使う。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| ストアページを開きたい | `StorePage.SetAppIdentifier(id)` → `StorePage.OpenStorePage()` |
| 生成されたストアURLだけ欲しい | `StorePage.StorePageUrl`（`SetAppIdentifier` 後に有効。Android は `market://details?id=<id>`、iOS は `itms-apps://itunes.apple.com/app/id<id>?mt=8`） |
| 設定した識別子を知りたい | `StorePage.AppIdentifier` |

## 使い方

- 「ストアを開く」実装パターン: `Application.platform` で Android/iOS それぞれの appIdentifier を選択 → `StorePage.SetAppIdentifier(appIdentifier)` → `StorePage.OpenStorePage()`

## 注意点・罠

- **実機（Android / iOS）以外では何も起きない**。Editor・Standalone では `Application.platform` が switch にマッチせず、`SetAppIdentifier` は `AppIdentifier` だけを保持して `StorePageUrl` は null のまま、`OpenStorePage` は無反応（動作確認は実機のみ）
- `SetAppIdentifier` を呼ぶ前に `OpenStorePage()` しても URL が null のまま。**必ずセット → オープンの順**
- identifier はプラットフォームで意味が違う（Android: applicationId / iOS: Apple の App ID 数値）
- レビュー誘導（アプリ内レビュー）は本モジュールの担当外

## 関連

- [ApplicationEvent](../ApplicationEvent/Manual.md) — アプリライフサイクル関連
- [InAppPurchasing](../InAppPurchasing/Manual.md) — ストア課金（本モジュールとは独立）
