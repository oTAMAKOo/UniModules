# MessagePack

MessagePack の共通設定をまとめた機能。
エディタと実機で使う Resolver を自動で切り替え、日時を文字列で読み書きできる。
実機では MessagePack の生成コードが必要になる。

## 基本情報

> **namespace**: `Modules.MessagePack`
> **場所**: `Modules/MessagePack/`
> **依存**: MessagePack-CSharp（UPM git package `com.github.messagepack-csharp`。バージョンは利用プロジェクトの manifest.json 参照） / Extensions / Editor側: UniTask / R3 / Modules.Devkit（`ProjectPrefs`, `SingletonScriptableObject`）

## 概要

MessagePack-CSharp のプロジェクト共通 Resolver（`UnityCustomResolver`）と、コード生成まわりのエディタ統合。
IL2CPP 実機では Dynamic 系リゾルバが使えないため生成コード（`GeneratedMessagePackResolver`）が必要になるが、**利用側で `MESSAGEPACK_ANALYZER_CODE` を定義すると MessagePack v3 の Source Generator によりコンパイル時に自動生成される**。エディタ（Dynamic系）/実機（GeneratedResolver）の Resolver 切替は `UnityCustomResolver` が吸収する。
LocalData / Master / 通信(Network) 等、バイナリシリアライズは基本的にここを通る。
主要クラス: `UnityCustomResolver`（プロジェクト標準 Resolver。static Instance） / `StringDateTimeResolver`・`StringDateTimeFormatter`（DateTime を文字列で書き込み。チェーン組込済）。ほか旧 mpc 方式のエディタ群（`MessagePackConfig` / `MessagePackConfigInspector` / `MessagePackCodeGenerateInfo` / `MessagePackHelper` / `MessagePackCodeGenerator`）がある。このうち `MessagePackCodeGenerator` とそのメニューは `MESSAGEPACK_ANALYZER_CODE` 定義時にコンパイルから外れる（罠参照）。
Resolver の並び: エディタは `StringDateTimeResolver` → `BuiltinResolver` → `UnityResolver` → 属性・Dynamic 系 → `PrimitiveObjectResolver`。実機は 生成コードの Resolver（`MESSAGEPACK_ANALYZER_CODE` 定義時は `GeneratedMessagePackResolver`、未定義時は mpc が生成する `GeneratedResolver`）→ `StringDateTimeResolver` → `UnityResolver` → `StandardResolver`。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| 新しいシリアライズ対象クラスを定義したい | `[MessagePackObject(true)]` + public プロパティ `{ get; set; }`（下記規約） |
| Resolver オプションを組みたい | `StandardResolverAllowPrivate.Options.WithResolver(UnityCustomResolver.Instance)` |
| DateTime を文字列でやり取りしたい | 自動適用（`StringDateTimeResolver` が Resolver チェーンに組込済） |
| 実機（IL2CPP）用の生成コードを作りたい | **不要（自動）**。`MESSAGEPACK_ANALYZER_CODE` 定義時は v3 Source Generator が `[MessagePackObject]` 型からビルド時に生成 |
| 旧 mpc 方式で生成コードを作りたい（`MESSAGEPACK_ANALYZER_CODE` 未定義時） | メニュー `Extension > Generators > Generate MessagePack`（= `MessagePackCodeGenerator.Generate()`。設定は `MessagePackConfig`） |
| デバッグで中身を見たい | `MessagePackSerializer.SerializeToJson(obj, options)` |

## 使い方

### シリアライズ規約

データ型定義は **map モード（プロパティ名がキー）** で統一:

- クラスに `[MessagePackObject(true)]` を付ける（true = keyAsPropertyName。プロパティ名キーの map モード）
- `[Key(int)]` 属性は使わない（`[MessagePackObject(true)]` の文字列キー方式で統一）
- **ネストする型にも全て** `[MessagePackObject(true)]` を付ける（付け忘れると実機のみ落ちる）
- メンバーは public プロパティ `{ get; set; }`。除外は `[IgnoreMember]`、コンストラクタ指定は `[SerializationConstructor]`（実例: `Modules.FileCache.CacheFileData`）
- オプションの定型は `StandardResolverAllowPrivate.Options.WithResolver(UnityCustomResolver.Instance)`

## 注意点・罠

- **IL2CPP 実機では生成コードが必須**（Dynamic 系は実機で使えない）。利用側は `MESSAGEPACK_ANALYZER_CODE` を定義して v3 Source Generator による自動生成に任せるのが推奨（手動のコード生成作業は不要）
- 自動生成でも **属性の付け忘れは Source Generator の生成対象から漏れる** → エディタ（Dynamic）では動くのに実機で落ちる、という罠は残る。新規データ型は `[MessagePackObject(true)]` を必ず付け、ネスト型まで確認する
- `MessagePackConfig.asset` / mpc 設定（`Extension > Generators > Generate MessagePack`）はレガシー。`MessagePackCodeGenerator` は `#if !MESSAGEPACK_ANALYZER_CODE` のため `MESSAGEPACK_ANALYZER_CODE` 定義時は丸ごとコンパイル除外（メニューも非表示）。`MessagePackConfig` などほかのエディタクラスは定義の有無によらずコンパイルされる
- `MESSAGEPACK_ANALYZER_CODE` を定義しない場合、実機ビルドは mpc が生成した `GeneratedResolver` クラスを参照する。生成コードが無いとコンパイルが通らない
- DateTime は標準の MessagePack Timestamp ではなく**文字列**（`yyyy-MM-ddTHH:mm:ss.FFFFFFFK`。読み込みは文字列/ネイティブ両対応）でシリアライズされる。外部システムとバイナリ互換を取る際は注意
- `[MessagePackObject(true)]`（map モード）はプロパティ名がそのままキーになるため、**保存済みデータがある型のプロパティ名変更は互換を壊す**
- LZ4 圧縮（`Lz4BlockArray`）と AES 暗号化は本モジュールではなく利用側の責務
- 利用側が asmdef 分割せず csc.rsp を使う構成の場合、csc.rsp の define は全コードに効く（asmdef 分割している場合は各 asmdef 側の Define Constraints を確認）

## 関連

- [LocalData](../LocalData/Manual.md) — セーブデータ。`[MessagePackObject(true)]` 規約の実運用先
- [Master](../Master/Manual.md) — マスターデータのシリアライズ（`MasterManager` が `UnityCustomResolver` 使用）
- [Network](../Network/Manual.md) — API 通信のボディシリアライズ
- [Crypto](../Crypto/Manual.md) — `AesCryptoKey`（シリアライズ結果の暗号化）
- [FileCache](../FileCache/Manual.md) — `CacheData` / `CacheFileData` のシリアライズ利用例
