# Lua

> **namespace**: `Modules.Lua`（`Lua/`） / `Modules.Lua.Command`（`Lua.command/`） / `Modules.Lua.Text`（`Lua.text/`）
> **場所**: `Client/Assets/UniModules/Scripts/Modules/Lua/` + `Lua.command/` + `Lua.text/`（3フォルダで1系統）
> **依存**: xLua（`XLua.LuaEnv` / `LuaAsset`） / UniTask / R3 / Extensions（`LifetimeDisposable`, `AesCryptoKey`, `Security.XXTEA`） / Modules.Localize（`EditorLanguage`、Editor専用） / Modules.Devkit（Editor専用）

## 概要

xLua（Lua実行環境）との連携基盤。Luaスクリプトのロード・実行（`LuaController`）、C#コマンドのLua関数自動公開（`CommandLoader`）、Excel由来のIDテキスト管理（`Lua.Text`）を提供する。[Scenario](Scenario.md) モジュールがカットシーン記述用にこの基盤の上に構築されている。

主要クラス: `LuaController`（中核。`LuaEnv` 生成・Require・Lua関数実行・グローバル変数読み書き）/ `LuaLoader`（abstract。Luaファイルの遅延ロード基盤、`LoadAsync` を派生実装）/ `LuaReference`（ScriptableObject。path→`LuaAsset` 参照テーブル）/ `CommandLoader`+`ICommand`（C#コマンドのLua公開）/ `LuaText`+`LuaTextAsset`（ID→暗号化テキスト）。Editor側（`Lua.text/Editor/` 等）に Excel⇔LuaTextAsset 変換ツール群（`GenerateWindow` / `LuaTextConfig` / `LuaTextExcel` / `LuaTextAssetGenerator` / `LuaTextAssetUpdater`）と `LuaCodeGenerator`。

全ファイルが `#if ENABLE_XLUA` ガード内。利用側で `ENABLE_XLUA` シンボルが未定義の場合はコンパイル対象外になる。詳細な有効化条件は [Scenario](Scenario.md) の罠を参照。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| このモジュールを有効化したい | xLuaプラグイン導入 + `ENABLE_XLUA` 定義 |
| （有効時）Lua環境を構築・関数実行 | `LuaController.Setup(loader, reference)` → `Execute(functionName, cancelToken)` |
| （有効時）LuaファイルをDL/ロードする仕組みを実装 | `LuaLoader` 派生で `LoadAsync(filePath)` を実装 |
| （有効時）C#メソッドをLua関数として公開 | `ICommand` 実装 + `CommandLoader` 派生の `GetCommandTypes()` に登録 |
| （有効時）Lua用テキストをID指定で取得 | `LuaText.Set(luaTextAsset)` → `Get(id)` |
| （有効時）Excel→LuaTextAsset 変換 | Editor: `GenerateWindow`（Import/Export/Generate All）、自動更新は `LuaTextAssetUpdater.Prefs.autoUpdate` |
| ローカライズテキスト管理（別系統） | `Lua.Text` ではなく [TextData](TextData.md)（`TextData.Get`）を使う |

## 使い方

- 実行フロー（`LuaController` / `CommandLoader` の利用のされ方）の参考実コード: `Client/Assets/UniModules/Scripts/Modules/Scenario/ScenarioController.cs`（`ENABLE_XLUA` 内）
- `LuaText` の利用実態: `Client/Assets/UniModules/Scripts/Modules/Scenario/Command/Text/TextLoad.cs`

## 注意点・罠

- 有効化には `ENABLE_XLUA` 定義に加え xLuaプラグイン本体の導入が必須（`XLua.LuaEnv` / `LuaAsset` はプラグイン側の型。定義だけ足すとコンパイルエラー）。
- さらに `framework.AsyncTask` / `module.LazyRequire` という .lua スクリプトを実行時 `Require` するため、利用側で該当 .lua ファイルの整備が必要。
- `LuaController.Execute` は完了時に必ず `Exit()`（= `LuaEnv.Dispose()`）する使い捨て設計。連続実行するには `Setup` からやり直す。
- `LuaLoader` / `CommandLoader` は abstract。必ず派生実装が要る（実装例は [Scenario](Scenario.md) 参照）。
- コマンド登録の検証（`[CSharpCallLua]` 等）は実行時 `Debug.LogError` でしか検出されない。
- `LuaAsset.GetDecodeBytes` は不具合があるため使わず、拡張メソッド `LuaAsset.GetData()`（`LuaExtensions`）を使う前提。
- `Lua.text/Editor` の変換は外部コンバータexe（`LuaTextConfig` の `winConverterPath`）前提。パス未設定では動かない。
- ローカライズテキストが目的なら本モジュールではなく [TextData](TextData.md) を使うこと。

## 関連

- [Scenario](Scenario.md) — 本モジュール上に構築されたカットシーン基盤（コマンド実装例・有効化手順の詳細）
- [TextData](TextData.md) — テキスト管理（`Lua.Text` と役割が同種）
- [Localize](Localize.md) — `LuaTextLanguage` が参照する言語選択（`EditorLanguage`）
- [ExternalAsset](ExternalAsset.md) — `LuaLoader.LoadAsync` の実装先として想定される配信基盤
