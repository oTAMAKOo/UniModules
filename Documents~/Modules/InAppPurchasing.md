# InAppPurchasing

> **namespace**: `Modules.InAppPurchasing`
> **場所**: `Client/Assets/UniModules/Scripts/Modules/InAppPurchasing/`
> **依存**: Unity IAP（`UnityEngine.Purchasing`。全コードが `#if UNITY_PURCHASING`。利用側で定義）/ R3 / UniTask / Extensions（Singleton）/ Modules.Devkit.Console

## 概要

Unity IAP（In App Purchasing）のラッパー基盤。ストア商品の登録・購入開始・購入/復元結果の Observable 通知・Pending（購入未確定）管理を提供する。
**サーバー検証前提の設計**: `ProcessPurchase` は常に `Pending` を返し、サーバーでのレシート検証・付与が成功した後に `PurchaseFinish` で確定（`ConfirmPendingPurchase`）する。アプリ強制終了時もレシートが Pending に残り、次回起動の Restore で救済される。
Client 側は本基盤を継承した Manager を1つ用意し、`FetchProducts()` で「販売する商品定義」を返す実装を書く。
主要クラス: `PurchaseManager<TInstance>`（基盤本体。abstract、`FetchProducts()` 実装必須）/ `IStorePurchasing`・`ApplePurchasing`・`GooglePlayStorePurchasing`（ストア固有処理）/ `PurchaseResult`（購入結果）/ `BuyFailureReason`（購入開始失敗理由の enum）。

## 課金フローの全体像

```
起動時   派生 Manager.CreateInstance() → Initialize()   … 購入完了/復元の購読を開始
             ↓
商品取得 派生 Manager.UpdateProducts()
           ├ FetchProducts()  … 販売対象の ProductDefinition を作成（利用側実装）
           └ UnityPurchasing.Initialize → OnInitialized    … StoreProducts / PendingProducts 更新、IsPurchaseReady = true
             ↓
購入     派生 Manager.Purchase(purchaseId, cancelToken)   … 利用側 public API
           └ 基盤 Purchase(productId, developerPayload) → storeController.InitiatePurchase
             ↓
ストア   ProcessPurchase（IStoreListener コールバック）
           … Pending に積み、OnStorePurchaseComplete を通知（戻り値は常に PurchaseProcessingResult.Pending）
             ↓
検証付与 OnStorePurchaseComplete → 利用側の ApplyPurchaseContents(purchaseResult)
           ├ サーバーでレシート検証 + アイテム付与
           └ 成功: PurchaseFinish(product)  … ConfirmPendingPurchase で確定
             ↓
復元     purchaseManager.Restore()   … PendingProducts を再通知 → 同じ検証・付与ルートへ
```

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| 課金アイテムを購入したい | 派生 Manager の `Purchase(purchaseId, cancelToken)`（利用側で public 化） |
| 販売中の商品一覧（価格文字列等）を取得したい | `purchaseManager.StoreProducts`（`Product.metadata.localizedPriceString` 等） |
| ストア商品情報を更新したい | `await purchaseManager.UpdateProducts()` |
| 購入完了（検証・付与後）を購読したい | 派生 Manager 側で公開する Observable |
| 購入エラーを購読したい | 派生 Manager 側で公開する Observable（`BuyFailureReason`） |
| 未確定（Pending）の購入を復元したい | `purchaseManager.Restore()` |
| 課金システムの準備完了を判定したい | `purchaseManager.IsPurchaseReady` / `PendingProducts.Any()` なら準備完了待ち |
| GooglePlay の保留決済（コンビニ払い等）を検知したい | `GooglePlayStorePurchasing.OnDeferredPurchaseAsObservable()` |

## 使い方

- **派生 Manager の定義**（`FetchProducts` override + 完了/復元の購読）を利用側に用意する。`FetchProducts()` は販売する商品を `ProductDefinition(identifier, ProductType.Consumable)` の列で返す
- **起動時の初期化**: `派生Manager.CreateInstance()` → `Initialize()`
- **商品取得**: `await 派生Manager.UpdateProducts()`
- **購入**: 派生 `Purchase(purchaseId, cancelToken)`。Android は必要に応じて developerPayload を付与
- **検証・付与 → 確定**: 派生 `ApplyPurchaseContents(purchaseResult)` を実装し、成功時に `PurchaseFinish(product)`
- **復元と Pending 待ち**: 適切なタイミング（例: 特定シーンの Enter）で `Restore()`、`Prepare` 等で `PendingProducts.Any()` なら `IsPurchaseReady` になるまで待機

## 注意点・罠

- **`UNITY_PURCHASING` シンボル必須**: モジュール全コードが `#if UNITY_PURCHASING`。利用側でシンボル定義が必要
- **基盤の `Purchase(productId)` は protected**: 新規 UI からは派生 Manager 側の `Purchase(purchaseId, cancelToken)` を呼ぶ（マスター解決・payload 付与・完了待ちを内包する形にラップ）
- **完了通知を購読していないと購入できない**: `onStorePurchaseComplete` が null だと `NoReceivePurchaseMessage` で即失敗。派生 `Initialize()` で必ず購読する
- **復元通知も購読必須**: `onStorePurchaseRestore` が未購読だと `Restore()` は `NoReceiveRestoreMessage` で失敗する
- **`ProcessPurchase` は常に Pending を返す**: 付与はストアではなくサーバーの検証成功に紐づく。`PurchaseFinish` を呼び忘れると商品が Pending に残り続け、次回 Restore で再検証される（＝多重付与はサーバー側で防ぐ前提）
- **`PurchaseFinish` はサーバー検証・付与成功後にのみ呼ぶ**（Pending 確定＝`ConfirmPendingPurchase`）
- **`PurchaseResult.Validate` はローカル簡易検証**: `RECEIPT_VALIDATION` 定義時に `CrossPlatformValidator` を実行するだけで、付与判断には使わない
- **`IsPurchasing` 中の再購入は `InPurchasing` で弾かれる**: 連打対策は派生側でポーリング等により完了待ちにする
- **`ApplePurchasing.OnRestoreFinishAsObservable()` の true は「処理終了」**: 復元対象があったかどうかの意味ではない
- **GooglePlay の保留決済**: `IsPurchasedProductDeferred` な商品は完了通知を出さず Pending のまま保持（決済成立後の起動で処理）
- **エディタでは FakeStore**: `FakeStoreUIMode.StandardUser` の擬似ストアダイアログが出る（実課金なし）。`StorePurchasing` は null（ストア名は `DummyStore`）
- **通知は R3 の `Observable`**。購読は `.AddTo(Disposable)`
- **`UpdateProducts` の2回目以降は追加取得**: `FetchAdditionalProducts` を使うため、初期化後に商品が増えた場合も再構築ではなく追加登録になる

## 関連

- [PlayFab](PlayFab.md) — レシート検証・付与をサーバー経由で行う場合の主経路
- [Master](Master.md) — 商品 ID・プラットフォームの定義元
- [R3Extension](R3Extension.md) — Observable 購読パターン
- [../Extensions/Core.md](../Extensions/Core.md) — `Singleton<T>` / `CreateInstance` / `AddTo(Disposable)`
