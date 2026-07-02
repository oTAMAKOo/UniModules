# StorePage

> **namespace**: `Modules.StorePage`
> **場所**: `Client/Assets/UniModules/Scripts/Modules/StorePage/`
> **Client側使用**: 1ファイル（2026-07時点: `AppVersionDialog.cs`）
> **依存**: UnityEngine のみ

## 概要

アプリのストアページ（Google Play / App Store）を開くだけの極小 static クラス。
実用途は**強制アップデートダイアログ（`AppVersionDialog`）の「ストアを開く」ボタン**。ストアアプリを直接起動する URL スキーム（`market://` / `itms-apps://`）を使う。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| ストアページを開きたい | `StorePage.SetAppIdentifier(id)` → `StorePage.OpenStorePage()` |
| 生成されたストアURLだけ欲しい | `StorePage.StorePageUrl`（`SetAppIdentifier` 後に有効） |

## 主要クラス

| クラス | 種別 | 役割 |
|---|---|---|
| `StorePage` | static class | ストアURL組み立てと `Application.OpenURL` 呼び出しのみ |

## 使い方(実例)

```csharp
// 引用元: Client/Assets/Scripts/Client/Core/Dialog/AppVersionDialog.cs
private static void OpenStore()
{
    var appConfig = AppConfig.Instance;

    var appIdentifier = string.Empty;

    switch (Application.platform)
    {
        case RuntimePlatform.Android:
            appIdentifier = appConfig.AndroidAppIdentifier;
            break;
        case RuntimePlatform.IPhonePlayer:
            appIdentifier = appConfig.IOSAppIdentifier;
            break;
    }

    StorePage.SetAppIdentifier(appIdentifier);

    StorePage.OpenStorePage();
}
```

## API(主要公開メンバー)

| メンバー | 説明 |
|---|---|
| `static void SetAppIdentifier(string identifier)` | プラットフォーム別にストアURLを組み立てる。Android: パッケージ名 / iOS: App Store の数値ID（`AppConfig` に定義済み） |
| `static void OpenStorePage()` | `Application.OpenURL(StorePageUrl)` を実行 |
| `static string AppIdentifier { get; }` | 設定済み identifier |
| `static string StorePageUrl { get; }` | 組み立て済みURL（Android: `market://details?id={0}` / iOS: `itms-apps://itunes.apple.com/app/id{0}?mt=8`） |

## 注意点・罠

- **実機（Android / iOS）以外では何も起きない**。`SetAppIdentifier` / `OpenStorePage` とも Editor・Standalone では switch にマッチせず無反応（動作確認は実機のみ）
- `SetAppIdentifier` を呼ぶ前に `OpenStorePage()` しても URL が null のまま。**必ずセット → オープンの順**（実例のように直前にセットするのが安全）
- identifier はプラットフォームで意味が違う（Android: applicationId / iOS: Apple の App ID 数値）。値は `AppConfig.Instance.AndroidAppIdentifier / IOSAppIdentifier` から取得する
- レビュー誘導（アプリ内レビュー）は本モジュールではなく `AppReviewManager` 系の担当

## 関連

- [ApplicationEvent](ApplicationEvent.md) — アプリライフサイクル関連
- [InAppPurchasing](InAppPurchasing.md) — ストア課金（本モジュールとは独立）
