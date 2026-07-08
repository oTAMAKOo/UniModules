# InAppPurchasing

> **namespace**: `Modules.InAppPurchasing`
> **場所**: `Client/Assets/UniModules/Scripts/Modules/InAppPurchasing/`
> **Client側使用**: 4ファイル（2026-07時点）
> **依存**: Unity IAP（`UnityEngine.Purchasing`。全コードが `#if UNITY_PURCHASING`、Dominion では `Client/Assets/csc.rsp` で常時定義）/ R3 / UniTask / Extensions（Singleton）/ Modules.Devkit.Console

## 概要

Unity IAP（In App Purchasing）のラッパー基盤。ストア商品の登録・購入開始・購入/復元結果の Observable 通知・Pending（購入未確定）管理を提供する。
**サーバー検証前提の設計**: `ProcessPurchase` は常に `Pending` を返し、PlayFab でのレシート検証・付与が成功した後に `PurchaseFinish` で確定（`ConfirmPendingPurchase`）する。アプリ強制終了時もレシートが Pending に残り、次回起動の Restore で救済される。
Client 側の入口は `Dominion.Client.PurchaseManager`（`Client/Assets/Scripts/Client/Core/Purchase/PurchaseManager.cs`）。
主要クラス: `PurchaseManager<TInstance>`（基盤本体。abstract、`FetchProducts()` 実装必須）/ `IStorePurchasing`・`ApplePurchasing`・`GooglePlayStorePurchasing`（ストア固有処理）/ `PurchaseResult`（購入結果）/ `BuyFailureReason`（購入開始失敗理由の enum）。

## 課金フロー（Dominion 実装の全体像）

```
起動時   InitializeObject.manager.cs
           PurchaseManager.CreateInstance() → Initialize()   … 購入完了/復元の購読を開始
             ↓
商品取得 GameStartupManager.Setup()
           await purchaseManager.UpdateProducts()
             ├ FetchProducts()  … PurchaseMaster × PlayFab カタログを突合（両方に存在する商品のみ ProductDefinition 化）
             └ UnityPurchasing.Initialize → OnInitialized    … StoreProducts / PendingProducts 更新、IsPurchaseReady = true
             ↓
購入     PurchaseManager.Purchase(purchaseId, cancelToken)   … Client 側 public API（課金処理の入口）
           ├ Android は developerPayload（Identifier + UserCode）を付与
           └ 基盤 Purchase(productId, developerPayload) → storeController.InitiatePurchase
             ↓
ストア   ProcessPurchase（IStoreListener コールバック）
           … Pending に積み、OnStorePurchaseComplete を通知（戻り値は常に PurchaseProcessingResult.Pending）
             ↓
検証付与 OnStorePurchaseComplete → ApplyPurchaseContents(purchaseResult)
           ├ PlayFabManager.PurchaseGooglePlay / PurchaseAppleStore   … PlayFab サーバーでレシート検証 + アイテム付与
           └ 成功: UserGiftBoxModel.RefreshFeacthTime() + PurchaseFinish(product)  … ConfirmPendingPurchase で確定
             ↓
復元     HomeScene.Enter() … purchaseManager.Restore()   … PendingProducts を再通知 → 同じ検証・付与ルートへ
```

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| 課金アイテムを購入したい | `PurchaseManager.Instance.Purchase(purchaseId, cancelToken)`（Client 側。マスターの PurchaseId 指定） |
| 販売中の商品一覧（価格文字列等）を取得したい | `PurchaseManager.Instance.StoreProducts`（`Product.metadata.localizedPriceString` 等） |
| ストア商品情報を更新したい | `await PurchaseManager.Instance.UpdateProducts()` |
| 購入完了（検証・付与後）を購読したい | `OnPurchaseFinishAsObservable()`（Client 側） |
| 購入エラーを購読したい | `OnPurchaseErrorAsObservable()`（Client 側、`BuyFailureReason`） |
| 未確定（Pending）の購入を復元したい | `PurchaseManager.Instance.Restore()`（Home 入場毎に実施済み） |
| 課金システムの準備完了を判定したい | `PurchaseManager.Instance.IsPurchaseReady` / `PendingProducts.Any()` なら準備完了待ち |
| レシートをサーバー検証したい | `PlayFabManager.Instance.PurchaseGooglePlay / PurchaseAppleStore(purchaseResult)`（Client の完了ハンドラが実施済み） |
| GooglePlay の保留決済（コンビニ払い等）を検知したい | `GooglePlayStorePurchasing.OnDeferredPurchaseAsObservable()` |

## 使い方

定型パターンと参照先:

- **Client 側 Manager の定義**（`FetchProducts` override + 完了/復元の購読）: `Client/Assets/Scripts/Client/Core/Purchase/PurchaseManager.cs`。PurchaseMaster（Platform 一致）と PlayFab カタログの両方に存在する商品のみ `ProductDefinition(identifier, ProductType.Consumable)` 化
- **起動時の初期化**: `Client/Assets/Scripts/Client/Core/Initialize/InitializeObject/InitializeObject.manager.cs`（`CreateInstance()` → `Initialize()`）。商品取得は `Client/Assets/Scripts/Client/Manager/GameStartupManager.cs`（`await purchaseManager.UpdateProducts()`）
- **購入**: Client 側 `Purchase(purchaseId, cancelToken)`。`LoadingScope` 内で `IsPurchasing` を 0.5 秒間隔ポーリングして完了待ち。Android は developerPayload（Identifier + UserCode）を付与
- **検証・付与 → 確定**: Client 側 `ApplyPurchaseContents(purchaseResult)`。検証 API の実体は `PlayFabExtensions.Api.PurchaseGooglePlay / PurchaseAppleStore`（`Client/Assets/Scripts/PlayFab/Api/Purchase/`。レシート JSON を分解し `PlayFabClientAPI.ValidateGooglePlayPurchaseAsync` / `ValidateIOSReceiptAsync` を呼ぶ）
- **復元と Pending 待ち**: `Client/Assets/Scripts/Client/Scene/Home/HomeScene.cs`（`Enter()` で `IsPurchaseReady` なら `Restore()`、`Prepare` で `PendingProducts.Any()` なら `IsPurchaseReady` になるまで待機）

## 注意点・罠

- **`UNITY_PURCHASING` シンボル必須**: モジュール全コードが `#if UNITY_PURCHASING`。Dominion は `Client/Assets/csc.rsp` の `-define:UNITY_PURCHASING` で常時有効（`RECEIPT_VALIDATION` / `DISABLE_RUNTIME_IAP_ANALYTICS` も同ファイルで定義）
- **基盤の `Purchase(productId)` は protected**: 新規 UI からは Client 側 `PurchaseManager.Instance.Purchase(purchaseId, cancelToken)` を呼ぶ（マスター解決・payload 付与・完了待ちを内包）
- **完了通知を購読していないと購入できない**: `onStorePurchaseComplete` が null だと `NoReceivePurchaseMessage` で即失敗。Client 側 `Initialize()` が購読するため、**必ず `Initialize()` 後に購入すること**（起動フローで実施済み）
- **復元通知も購読必須**: `onStorePurchaseRestore` が未購読だと `Restore()` は `NoReceiveRestoreMessage` で失敗する
- **`ProcessPurchase` は常に Pending を返す**: 付与はストアではなく PlayFab の検証成功に紐づく。`PurchaseFinish` を呼び忘れると商品が Pending に残り続け、次回 Restore で再検証される（＝多重付与はサーバー側で防ぐ前提）
- **`PurchaseFinish` はサーバー検証・付与成功後にのみ呼ぶ**（Pending 確定＝`ConfirmPendingPurchase`）
- **`PurchaseResult.Validate` はローカル簡易検証**: `RECEIPT_VALIDATION` 定義時に `CrossPlatformValidator` を実行するだけで、付与判断には使っていない。本検証は `ValidateGooglePlayPurchaseAsync` / `ValidateIOSReceiptAsync`（PlayFab）
- **`IsPurchasing` 中の再購入は `InPurchasing` で弾かれる**: Client 側 `Purchase` は 0.5 秒間隔のポーリングで完了を待つ。連打対策は `LoadingScope` + この仕組みで担保
- **Restore は Home 入場毎に走る**: `HomeScene.Enter()`。復元時も購入時と同じ `ApplyPurchaseContents`（検証→付与→確定）ルートを通る
- **`ApplePurchasing.OnRestoreFinishAsObservable()` の true は「処理終了」**: 復元対象があったかどうかの意味ではない
- **GooglePlay の保留決済**: `IsPurchasedProductDeferred` な商品は完了通知を出さず Pending のまま保持（決済成立後の起動で処理）
- **エディタでは FakeStore**: `FakeStoreUIMode.StandardUser` の擬似ストアダイアログが出る（実課金なし）。`StorePurchasing` は null（ストア名は `DummyStore`）
- **通知は R3 の `Observable`**（UniRx ではない）。購読は `.AddTo(Disposable)`
- **`UpdateProducts` の2回目以降は追加取得**: `FetchAdditionalProducts` を使うため、初期化後に商品が増えた場合も再構築ではなく追加登録になる

## 関連

- [PlayFab](PlayFab.md) — レシート検証・付与（`PurchaseGooglePlay` / `PurchaseAppleStore`、カタログ取得 `FeacthCatalogItems`）
- [Master](Master.md) — `PurchaseMaster`（商品 ID・プラットフォームの定義元）
- [R3Extension](R3Extension.md) — Observable 購読パターン
- [../Extensions/Core.md](../Extensions/Core.md) — `Singleton<T>` / `CreateInstance` / `AddTo(Disposable)`
