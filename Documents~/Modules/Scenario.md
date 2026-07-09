# Scenario

> **namespace**: `Modules.Scenario` / `Modules.Scenario.Command`（`RubyTextMeshProUGUIExtension` のみ `TMPro`）
> **場所**: `Client/Assets/UniModules/Scripts/Modules/Scenario/`
> **依存**: xLua（`XLua` namespace） / Modules.Lua系（`Modules.Lua` / `Modules.Lua.Command` / `Modules.Lua.Text`） / UniTask / R3 / DOTween（`Modules.Tweening` = `DoTween/` フォルダ） / Modules.TimeUtil / Modules.TagText（実namespaceは `Modules.TagTect`） / Modules.ExternalAssets / Modules.Animation / Modules.Particle / Modules.Sound + CRI（Sound系コマンドのみ） / RubyTextMeshPro（ThirdParty） / Extensions

## 概要

Luaスクリプト（xLua）でシナリオ・カットシーン進行を記述するための基盤。C#の演出コマンド群（表示・移動・フェード・サウンド等 約46種）をLua関数として自動公開し、Lua側から `Wait(1.0)` / `Move(obj, ...)` のように呼び出して演出を制御する。

全ファイルが `#if ENABLE_XLUA` でガードされており、利用側で `ENABLE_XLUA` シンボルが未定義の場合はコンパイル対象外になる。Sound系コマンド8種と `SoundController` はさらに `ENABLE_CRIWARE_ADX(_LE)` も必要（→ [CriWare](CriWare.md)）。

主要クラス: `ScenarioController`（中核。abstract・非MonoBehaviour。派生で `CreateLuaLoader` / `CreateCommandLoader` / `GetCryptoKey` を実装）/ `ScenarioCommand`（全コマンド基底。`LuaName` / `Callback` を定義）/ `StandardCommand`（標準コマンド46種の `Type[] CommandTypes`。一覧は `Command/StandardCommand.cs` 参照）/ `ManagedObjects`・`AssetController`・`TaskController`・`SoundController`（サブコントローラ）/ `TweenControl`（DOTween再生）/ `Message`（メッセージ送りコマンド基底）/ `RubyTextMeshProUGUIExtension`。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| このモジュールを有効化したい | xLuaプラグイン導入 + `ENABLE_XLUA` 定義 + `ScenarioController` / `CommandLoader` / `LuaLoader` の派生実装（罠参照） |
| （有効時）シナリオ実行の入口 | `ScenarioController` 派生を `Setup()` → `await Prepare()` → `await Execute(luaFunction)` |
| （有効時）Luaから使える標準コマンド一覧 | `StandardCommand.CommandTypes`（`Command/StandardCommand.cs`） |
| （有効時）新しいシナリオコマンドを追加 | `ScenarioCommand` 継承 + `[CSharpCallLua]`（手順は「使い方」参照） |
| （有効時）Luaと値を受け渡し | `ScenarioController.GetValue<T>(key)` / `SetValue<T>(key, value)` |
| （有効時）シーン上のオブジェクトをLua名で管理 | `ManagedObjects.Add/Get/Remove`（Luaからは `CreateObject` / `GetObject`） |
| （有効時）演出全体の速度変更（スキップ等） | `ScenarioController.TimeScale`（`Modules.TimeUtil.TimeScale`） |
| ルビ付きTMPテキストの行高さ揃え | `RubyTextMeshProUGUI.InsertEmptyRubyTag(text)`（TMPro拡張。これも `ENABLE_XLUA` 内） |

## 使い方

実行フローの実シグネチャは `Client/Assets/UniModules/Scripts/Modules/Scenario/ScenarioController.cs`、コマンド登録機構は `Client/Assets/UniModules/Scripts/Modules/Lua.command/CommandLoader.cs` を参照。

### 新しいシナリオコマンドを追加する手順（有効化されている場合の参考）

1. `Modules.Scenario.Command` namespace に `ScenarioCommand` 継承の sealed クラスを作成する
2. クラスに `[CSharpCallLua]` 属性を付ける（無いと `CommandLoader` が実行時に `Debug.LogError`）
3. `LuaName`（Lua側関数名。`"Xxx.Yyy"` とドット区切りにするとLuaテーブル階層になる）と `Callback`（`nameof(LuaCallback)`）を override する
4. `public` な `LuaCallback` メソッドを定義する（引数がLua関数の引数になる。省略可能引数は `bool?` 等の Nullable、演出待ちが必要なら `async UniTask`、Luaへ値を返すなら戻り値を付ける）
5. 実装内では `scenarioController.ManagedObjects` / `AssetController` / `TaskController` / `TimeScale` を利用できる（`Setup` で注入済み）。`object target` 引数は `ToComponent<T>(target)` でコンポーネント化する
6. 利用側 `CommandLoader` 派生の `GetCommandTypes()` 戻り値に型を追加する（標準コマンドの追加位置は `Command/StandardCommand.cs` の `CommandTypes`）

要件（`CommandLoader.RegisterCommand` の検証項目）: `[CSharpCallLua]` 属性 / `ICommand` 実装 / デフォルトコンストラクタ の3点。

## 注意点・罠

- 有効化には (1) xLuaプラグイン導入、(2) `ENABLE_XLUA` 定義、(3) `ScenarioController` / `CommandLoader` / `LuaLoader` の派生実装、(4) Sound系はさらにCRI導入（→ [CriWare](CriWare.md) の罠参照）が必要で、シンボル定義だけでは動かない。
- `ScenarioController` は MonoBehaviour ではない（シーンに置けない）。かつ abstract のため必ず派生実装が要る。
- コマンド登録は実行時リフレクション。`[CSharpCallLua]` 属性・`ICommand` 実装・デフォルトコンストラクタのどれが欠けても実行時 `Debug.LogError`（コンパイルエラーにならない）。
- 省略可能引数は Nullable で受けるのが規約（例: `bool? sync`）。Lua側で nil を渡せる。
- `Message.TagText`（abstract プロパティ）・`FadeIn/FadeOut/FadeColor.TargetGraphic`・`TextLoad.EditAssetPathCallback` は利用側からの注入が前提。未設定のまま該当コマンドを呼ぶと NullReference になる。
- `TimeScale` は2系統ある: `ScenarioController.TimeScale`（`Modules.TimeUtil.TimeScale`。メッセージ送り・アニメ/パーティクルの `SpeedRate` に反映）と `TweenControl.TimeScale`（DOTween側）。連動は自動ではない。
- `Message.cs` の using は `Modules.TagTect`（TagTextモジュールの実namespaceがこの綴り）。grep時に `TagText` で探すと見落とすので注意。
- `RubyTextMeshProUGUIExtension` は namespace `TMPro` に定義されている（`Modules.Scenario` ではない）。内部でリフレクションにより `RubyTextMeshProUGUI` の private フィールド（`rubyScale` / `m_maxFontSize`）へアクセスしており、ThirdParty更新で壊れうる。
- Sound系コマンドはUnityAudio版 `SoundManagement` に非対応（CRIの `CueInfo` 前提）。CRI導入時のみ動作する。

## 関連

- [Lua](Lua.md) — `Modules.Lua` / `Lua.Command` / `Lua.Text`。本モジュールの実行基盤（同じく `ENABLE_XLUA` 定義で有効化）
- [Utage](Utage.md) — 別系統のADV/会話劇基盤（宴）
- [CriWare](CriWare.md) — Sound系コマンドの前提
- [Sound](Sound.md) — サウンド基盤
- [ExternalAsset](ExternalAsset.md) — `AssetLoad<T>` / `PlaySound` が使用する配信アセット基盤
- [Animation](Animation.md) — `PlayAnimation` が操作する `AnimationPlayer`
- [TagText](TagText.md) / [TimeUtil](TimeUtil.md) / [DoTween](DoTween.md) / [Particle](Particle.md)
