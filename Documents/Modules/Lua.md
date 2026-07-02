# Lua

> **namespace**: `Modules.Lua`（`Lua/`） / `Modules.Lua.Command`（`Lua.command/`） / `Modules.Lua.Text`（`Lua.text/`）
> **場所**: `Client/Assets/UniModules/Scripts/Modules/Lua/` + `Lua.command/` + `Lua.text/`（3フォルダで1系統）
> **Client側使用**: 0ファイル（2026-07時点。基盤内でも Scenario モジュールと EditorMenu からのみ参照＝間接利用のみ）
> **依存**: xLua（`XLua.LuaEnv` / `LuaAsset`、**プラグイン未導入**） / UniTask / R3 / Extensions（`LifetimeDisposable`, `AesCryptoKey`, `Security.XXTEA`） / Modules.Localize（`EditorLanguage`、Editor専用） / Modules.Devkit（Editor専用）

## 概要

xLua（Lua実行環境）との連携基盤。Luaスクリプトのロード・実行（`LuaController`）、C#コマンドのLua関数自動公開（`CommandLoader`）、Excel由来のIDテキスト管理（`Lua.Text`）を提供する。[Scenario](Scenario.md) モジュールがカットシーン記述用にこの基盤の上に構築されている。

**本プロジェクトでは未使用（コンパイル対象外）**。全ファイルが `#if ENABLE_XLUA` ガード内で、シンボルは未定義。xLuaプラグイン・.luaファイルとも Assets に存在しない。詳細な有効化条件は [Scenario](Scenario.md) の罠を参照。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| **Luaでカットシーン・スクリプト制御したい（本プロジェクト）** | このモジュールは**使えない**（xLua未導入）。ユーザーに設計相談すること |
| ローカライズテキスト管理（本プロジェクトの正規手段） | `Lua.Text` ではなく [TextData](TextData.md)（`TextData.Get`）を使う |
| （有効時）Lua環境を構築・関数実行 | `LuaController.Setup(loader, reference)` → `Execute(functionName, cancelToken)` |
| （有効時）LuaファイルをDL/ロードする仕組みを実装 | `LuaLoader` 派生で `LoadAsync(filePath)` を実装 |
| （有効時）C#メソッドをLua関数として公開 | `ICommand` 実装 + `CommandLoader` 派生の `GetCommandTypes()` に登録 |
| （有効時）Lua用テキストをID指定で取得 | `LuaText.Set(luaTextAsset)` → `Get(id)` |
| （有効時）Excel→LuaTextAsset 変換 | Editor: `GenerateWindow`（Import/Export/Generate All）、自動更新は `LuaTextAssetUpdater.Prefs.autoUpdate` |

## 主要クラス

全クラス `#if ENABLE_XLUA` 内のため**本プロジェクトでは一切コンパイルされない**。

| クラス | 種別 | 役割 |
|---|---|---|
| `LuaController` | sealed class（`LifetimeDisposable`） | 中核。`LuaEnv` 生成・`Require/Request`・Lua関数実行（`Execute`）・Luaグローバル変数の読み書き（`GetValue<T>/SetValue<T>`）・`log/logf` のLua側ログ関数提供。`EveryUpdate` で `LuaEnv.Tick()` |
| `LuaLoader` | abstract class（`LifetimeDisposable`） | Luaファイルの遅延ロード基盤。Lua側 `LoadRequest(luaPath)`（`module.LazyRequire` 経由）で非同期ロード→`Require`。派生で `LoadAsync(filePath) : UniTask<LuaAsset>` を実装必須 |
| `LuaReference` | ScriptableObject | `path` → `LuaAsset` の参照テーブル。`autoload=true` のものは `Setup` 時に自動 `Require` |
| `LuaExtensions` | static class | `LuaEnv.Require/Request(luaPath)` 拡張、`LuaAsset.GetData()`（XXTEA復号。`GetDecodeBytes` の不具合回避用） |
| `CommandLoader` | abstract class（`Modules.Lua.Command`） | `ICommand` 群をリフレクション検証（`[CSharpCallLua]`・`ICommand`・デフォルトコンストラクタ）して登録し、Luaのテーブル定義＋呼び出し関数コードを自動生成（async メソッドは `await(...)` ラップ） |
| `ICommand` | interface（`Modules.Lua.Command`） | `LuaName`（Lua関数名）と `Callback`（C#メソッド名）の2プロパティのみ |
| `LuaText` | sealed class（`Modules.Lua.Text`） | ID→テキスト辞書。`Set(LuaTextAsset)` でAES復号して蓄積、`Get(id)` で取得 |
| `LuaTextAsset` | ScriptableObject（`Modules.Lua.Text`） | シート単位の暗号化テキストデータ（`Content[]` / `TextData(id, text)`）＋差分検知用 `Hash` |
| `LuaCodeGenerator` | static class（`Editor/`） | xLuaブリッジC#コード生成（`XLUA_GENERAL` 定義がさらに必要） |
| `LuaTextConfig` | SingletonScriptableObject（`Lua.text/Editor/`） | 変換設定（ファイル形式 Yaml/Json・AES鍵・Excelコンバータパス・転送元/先フォルダ） |
| `LuaTextLanguage` | Singleton（`Lua.text/Editor/`） | `EditorLanguage.selection` に連動した出力言語選択（`LanguageInfo`: 言語enum・識別子・テキスト列index） |
| `GenerateWindow` | SingletonEditorWindow（`Lua.text/Editor/`） | Import All / Export All / Generate All ボタンのエディタウィンドウ |
| `LuaTextExcel` | static class（`Lua.text/Editor/`） | 外部コンバータexeプロセス起動でExcel⇔中間ファイル（Yaml/Json）変換 |
| `LuaTextAssetGenerator` | static class（`Lua.text/Editor/`) | `BookData/SheetData` → AES暗号化して `LuaTextAsset` 生成。`Hash` 比較の `IsRequireUpdate` |
| `LuaTextAssetUpdater` | static class（`Lua.text/Editor/`） | Excel更新を3秒間隔で監視し `LuaTextAsset` を自動再生成（`Prefs.autoUpdate` で有効化） |

## 使い方(実例)

Client側・基盤内とも実使用ゼロ。実行フロー（`ScenarioController` からの利用のされ方）は実コード `Client/Assets/UniModules/Scripts/Modules/Scenario/ScenarioController.cs` に基づく以下の形。

```csharp
// 参考: Modules/Scenario/ScenarioController.cs（ENABLE_XLUA 内）での利用実態.
luaController = new LuaController();
luaLoader = CreateLuaLoader();              // LuaLoader 派生（プロジェクト側実装）.
commandLoader = CreateCommandLoader();      // CommandLoader 派生（プロジェクト側実装）.

luaController.Setup(luaLoader, luaReference);   // LuaReference は ScriptableObject.
commandLoader.Setup(luaController);             // ICommand 群を Lua 関数として登録.

await luaController.Execute(luaFunction, cancelToken);  // "_main_" 経由で async 実行.
```

```csharp
// Lua.Text の利用実態（Modules/Scenario/Command/Text/TextLoad.cs）.
var luaText = new LuaText(cryptoKey);
luaText.Set(textAsset);      // LuaTextAsset を復号して蓄積.
var text = luaText.Get(id);  // ID指定で取得.
```

## API(主要公開メンバー)

### LuaController

| メンバー | 説明 |
|---|---|
| `Setup(LuaLoader loader, LuaReference reference)` | `LuaEnv` 生成・ローダー登録・autoload分を `Require` |
| `Prepare() : UniTask` | `LuaLoader.IsLoading` 完了待ち（遅延ロード対策で3フレーム余分に待つ） |
| `Execute(string functionName, CancellationToken) : UniTask` | `_main_`（自動生成の async ラッパー）経由でLua関数実行。完了/キャンセルまで待機し `finally` で `Exit()` |
| `Require(string luaPath)` / `Request(string luaPath)` | Lua の `require` / `request` 実行 |
| `GetValue<T>(key)` / `SetValue<T>(key, value)` | Luaグローバル変数の読み書き |
| `Exit()` | `LuaEnv.Dispose()`。**Execute 完了時に毎回呼ばれる**（LuaEnv使い捨て） |
| `LuaEnv` / `LuaReference` / `IsExecute` | 状態参照 |

### LuaLoader / CommandLoader / LuaText

| メンバー | 説明 |
|---|---|
| `LuaLoader.Initialize(LuaController)` | ローダー登録＋`module.LazyRequire` を `Require`。`LuaController.Setup` から呼ばれる |
| `LuaLoader.LoadRequest(string luaPath)` | Lua側から呼ぶ遅延ロード要求（ロード完了後に自動 `Require`） |
| `LuaLoader.LoadAsync(string filePath) : UniTask<LuaAsset>` | **abstract**。実ロード手段（ExternalAsset等）を派生で実装 |
| `CommandLoader.Setup(LuaController)` | `_command_` 仮想Luaファイルを生成・登録 |
| `CommandLoader.GetCommand<T>()` | 登録済みコマンドインスタンス取得 |
| `CommandLoader.GetCommandTypes() : IEnumerable<Type>` | **abstract**。登録するコマンド型一覧（[Scenario](Scenario.md) の `StandardCommand.CommandTypes` が実例） |
| `LuaText.Set(LuaTextAsset)` / `Get(string id)` | AES復号して蓄積 / ID取得（無ければ null） |
| `LuaText.GetAssetFileName(fileName, identifier)` | static。言語識別子付きアセットファイル名（`name-identifier.asset`）構築 |

## 注意点・罠

- **本プロジェクトでは未使用・コンパイル対象外**。Lua/スクリプト制御前提の実装をしないこと。有効化には `ENABLE_XLUA` 定義に加え xLuaプラグイン本体の導入が必須（`XLua.LuaEnv` / `LuaAsset` はプラグイン側の型。定義だけ足すとコンパイルエラー）。
- さらに `framework.AsyncTask` / `module.LazyRequire` という .lua スクリプトを実行時 `Require` するが、**プロジェクト内に .lua ファイルは1つも存在しない**（別途整備が必要）。
- `LuaController.Execute` は完了時に必ず `Exit()`（= `LuaEnv.Dispose()`）する使い捨て設計。連続実行するには `Setup` からやり直す。
- `LuaLoader` / `CommandLoader` は abstract。必ず派生実装が要る（実装例は [Scenario](Scenario.md) 参照。ただしそちらも未使用）。
- コマンド登録の検証（`[CSharpCallLua]` 等）は実行時 `Debug.LogError` でしか検出されない。
- `LuaAsset.GetDecodeBytes` は不具合があるため使わず、拡張メソッド `LuaAsset.GetData()`（`LuaExtensions`）を使う前提。
- `Lua.text/Editor` の変換は外部コンバータexe（`LuaTextConfig` の `winConverterPath`）前提。パス未設定では動かない。
- ローカライズテキストが目的なら本モジュールではなく [TextData](TextData.md) を使うこと（プロジェクト規約: テキスト直書き禁止 → `TextData.Get`）。

## 関連

- [Scenario](Scenario.md) — 本モジュール上に構築されたカットシーン基盤（コマンド実装例・有効化手順の詳細）
- [TextData](TextData.md) — 本プロジェクトの正規テキスト管理（`Lua.Text` と役割が同種）
- [Localize](Localize.md) — `LuaTextLanguage` が参照する言語選択（`EditorLanguage`）
- [ExternalAsset](ExternalAsset.md) — `LuaLoader.LoadAsync` の実装先として想定される配信基盤
