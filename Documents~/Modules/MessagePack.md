# MessagePack

> **namespace**: `Modules.MessagePack`
> **場所**: `Client/Assets/UniModules/Scripts/Modules/MessagePack/`
> **Client側使用**: 直接 using は2ファイル（`MessagePackUtility.cs` / `BattleTraceArchiver.cs`）。ただし **LocalData / Master / Network / PlayFab のシリアライズが全て本モジュールの Resolver を経由**し、`[MessagePackObject(true)]` 付きデータ型は30ファイル超（2026-07時点）
> **依存**: MessagePack-CSharp **v3.1.3**（UPM git package `com.github.messagepack-csharp`） / Extensions / Editor側: Modules.Devkit（`ProjectPrefs`, `SingletonScriptableObject`）

## 概要

MessagePack-CSharp のプロジェクト共通 Resolver（`UnityCustomResolver`）と、コード生成まわりのエディタ統合。
IL2CPP 実機では Dynamic 系リゾルバが使えないため生成コード（`GeneratedMessagePackResolver`）が必要になるが、**本プロジェクトは `MESSAGEPACK_ANALYZER_CODE` 定義（`Assets/csc.rsp`）+ MessagePack v3 の Source Generator によりコンパイル時に自動生成**される。エディタ（Dynamic系）/実機（GeneratedResolver）の Resolver 切替は `UnityCustomResolver` が吸収する。
LocalData / Master / 通信(Network) / BattleTrace 等、バイナリシリアライズは全てここを通る。
主要クラス: `UnityCustomResolver`（プロジェクト標準 Resolver。static Instance） / `StringDateTimeResolver`・`StringDateTimeFormatter`（DateTime を文字列で書き込み。チェーン組込済）。ほか `MessagePackConfig` / `MessagePackCodeGenerator` 等の旧 mpc 方式エディタ群は現在無効（罠参照）。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| 新しいシリアライズ対象クラスを定義したい | `[MessagePackObject(true)]` + public プロパティ `{ get; set; }`（下記規約） |
| クラスを MessagePack でシリアライズしたい | `Dominion.Client.Utility.MessagePackUtility.Serialize / Deserialize`（Client 側ヘルパー。AES + LZ4 対応） |
| Resolver オプションを自分で組みたい | `StandardResolverAllowPrivate.Options.WithResolver(UnityCustomResolver.Instance)`（プロジェクト定型） |
| DateTime を文字列でやり取りしたい | 自動適用（`StringDateTimeResolver` が Resolver チェーンに組込済） |
| 実機（IL2CPP）用の生成コードを作りたい | **不要（自動）**。v3 Source Generator が `[MessagePackObject]` 型からビルド時に生成 |
| デバッグで中身を見たい | `MessagePackSerializer.SerializeToJson(obj, options)`（BattleTraceArchiver に実例） |

## 使い方

### シリアライズ規約（LocalData / Master / PlayFab 共通）

データ型定義は **map モード（プロパティ名がキー）** で統一（実例: `Client/Assets/Scripts/Client/Data/Battle/BattleRecordData.cs`）:

- クラスに `[MessagePackObject(true)]` を付ける（true = keyAsPropertyName。プロパティ名キーの map モード）
- `[Key(int)]` 属性は使わない（`[MessagePackObject(true)]` の文字列キー方式で統一）
- **ネストする型にも全て** `[MessagePackObject(true)]` を付ける（付け忘れると実機のみ落ちる）
- メンバーは public プロパティ `{ get; set; }`。除外は `[IgnoreMember]`、コンストラクタ指定は `[SerializationConstructor]`（実例: `Modules.FileCache.CacheFileData`）
- オプションの定型は `StandardResolverAllowPrivate.Options.WithResolver(UnityCustomResolver.Instance)`（LocalData / Master / Network / Client 全てこの形）

### 定型パターン

- **シリアライズ/デシリアライズ（Client 側共通ヘルパー。LZ4 圧縮 + AES 暗号化対応）**: `Client/Assets/Scripts/Client/Utility/MessagePackUtility.cs`
- **デバッグ用 JSON 出力**（`MessagePackSerializer.SerializeToJson`）: `Client/Assets/Scripts/Client/Battle/Develop/Trace/BattleTraceArchiver.cs`

## 注意点・罠

- **IL2CPP 実機では生成コードが必須**という原則は不変（Dynamic 系は実機で使えない）。ただし本プロジェクトは **v3 Source Generator + `MESSAGEPACK_ANALYZER_CODE`（`Assets/csc.rsp`）で自動生成**のため、**手動のコード生成作業は不要**
  - ※ [LocalData](LocalData.md) の「実機ビルド前に `MessagePackCodeGenerator` でコード生成」という記述は旧 mpc 方式（v2系）時代のもの。現在そのメニュー自体が無効化されている
- 自動生成でも **属性の付け忘れは Source Generator の生成対象から漏れる** → エディタ（Dynamic）では動くのに実機で落ちる、という罠は残る。新規データ型は `[MessagePackObject(true)]` を必ず付け、ネスト型まで確認する
- `MessagePackConfig.asset` / mpc 設定（`Extension > Generators > Generate MessagePack`）はレガシー。`MessagePackCodeGenerator` は `#if !MESSAGEPACK_ANALYZER_CODE` のため丸ごとコンパイル除外（メニューも非表示）。**`MESSAGEPACK_ANALYZER_CODE` を外さない限り触る必要はない**
- DateTime は標準の MessagePack Timestamp ではなく**文字列**（`yyyy-MM-ddTHH:mm:ss.FFFFFFFK`。読み込みは文字列/ネイティブ両対応）でシリアライズされる。外部システムとバイナリ互換を取る際は注意
- `[MessagePackObject(true)]`（map モード）はプロパティ名がそのままキーになるため、**保存済みデータがある型のプロパティ名変更は互換を壊す**
- LZ4 圧縮（`Lz4BlockArray`）と AES 暗号化は本モジュールではなく利用側（`MessagePackUtility` / `MessagePackFileUtility` / LocalData）の責務
- asmdef 分割は無くプロジェクト全体が Assembly-CSharp のため、csc.rsp の define は全コードに効いている

## 関連

- [LocalData](LocalData.md) — セーブデータ。`[MessagePackObject(true)]` 規約の実運用先
- [Master](Master.md) — マスターデータのシリアライズ（`MasterManager` が `UnityCustomResolver` 使用）
- [Network](Network.md) — API 通信のボディシリアライズ
- [Crypto](Crypto.md) — `AesCryptoKey`（シリアライズ結果の暗号化）
- [FileCache](FileCache.md) — `CacheData` / `CacheFileData` のシリアライズ利用例
