# Scenario

Lua でシナリオや演出を進める機能。
表示・移動・フェード・サウンドなどの演出コマンドを Lua から呼び出して、カットシーンを制御できる。
xLua の導入と ENABLE_XLUA シンボルの定義が必要。

## 基本情報

> **namespace**: `Modules.Scenario` / `Modules.Scenario.Command`（`RubyTextMeshProUGUIExtension` のみ `TMPro`）
> **場所**: `Modules/Scenario/`
> **依存**: xLua（`XLua` namespace。全コードが `#if ENABLE_XLUA`。利用側で定義） / Modules.Lua系（`Modules.Lua` / `Modules.Lua.Command` / `Modules.Lua.Text`） / UniTask / R3 / DOTween（`Modules.DoTweenExtension`） / Modules.TimeUtil / Modules.TagText / Modules.ExternalAssets / Modules.Animation / Modules.Particle / Modules.Devkit（`UnityConsole`） / Modules.Sound + CRI（Sound系コマンドのみ） / RubyTextMeshPro（ThirdParty） / Extensions

## 概要

Luaスクリプト（xLua）でシナリオ・カットシーン進行を記述するための基盤。C#の演出コマンド群（表示・移動・フェード・サウンド等 46種。うちSound系8種はCRI導入時のみ）をLua関数として自動公開し、Lua側から `Wait(1.0)` / `Move(obj, ...)` のように呼び出して演出を制御する。

全ファイルが `#if ENABLE_XLUA` でガードされており、利用側で `ENABLE_XLUA` シンボルが未定義の場合はコンパイル対象外になる。Sound系コマンド8種とその基底 `PlaySound`、`SoundController` はさらに `ENABLE_CRIWARE_ADX` または `ENABLE_CRIWARE_ADX_LE` も必要（→ [CriWare](../CriWare/Manual.md)）。

主要クラス: `ScenarioController`（中核。abstract・非MonoBehaviour。派生で `CreateLuaLoader` / `CreateCommandLoader` / `GetCryptoKey` を実装）/ `ScenarioCommand`（全コマンド基底。`LuaName` / `Callback` を定義）/ `StandardCommand`（標準コマンド46種の `Type[] CommandTypes`。CRI未導入時はSound系を除く38種。一覧は `Command/StandardCommand.cs` 参照）/ `ManagedObjects`・`AssetController`・`TaskController`・`SoundController`（サブコントローラ）/ `TweenControl`（DOTween再生）/ `Message`・`AssetLoad<T>`・`PlaySound`（abstract のコマンド基底。`CommandTypes` には含まれない）/ `RubyTextMeshProUGUIExtension`。

標準コマンドのLua側の名前（`LuaName`）:

| 分類 | Lua関数名 |
|---|---|
| アセット | `Asset.Request` / `Asset.LoadInQueue` |
| タスク | `TaskRun` / `RemoveTask` |
| テキスト | `TextLoad` / `Text`（クラス名は `GetText`） |
| 共通 | `Wait` / `Show` / `Hide` / `CreateObject` / `DeleteObject` / `DeleteAllObject` / `GetObject` / `SetPriority` |
| Transform | `Move` / `MoveX` / `MoveY` / `MoveZ` / `LocalMove` / `LocalMoveX` / `LocalMoveY` / `LocalMoveZ` / `Rotate` / `RotateX` / `RotateY` / `RotateZ` / `Scale` / `ScaleX` / `ScaleY` / `ScaleZ` / `Shake` |
| アニメーション・パーティクル | `PlayAnimation` / `StopAnimation` / `PlayParticle` / `StopParticle` |
| フェード | `FadeIn` / `FadeOut` / `FadeColor` |
| サウンド（CRI導入時のみ） | `PlayBgm` / `PlaySe` / `PlayVoice` / `PlayJingle` / `PlayAmbience` / `PauseSound` / `StopSound` / `StopAllSound` |

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| このモジュールを有効化したい | xLuaプラグイン導入 + `ENABLE_XLUA` 定義 + `ScenarioController` / `CommandLoader` / `LuaLoader` の派生実装（罠参照） |
| （有効時）シナリオ実行の入口 | `ScenarioController` 派生を `Setup(luaPath, luaReference)` → `await Prepare(luaFunction)`（Luaのロード完了を待って準備用の関数を呼ぶ） → `await Execute(luaFunction, cancelToken)` |
| （有効時）実行中のシナリオを中断 | `ScenarioController.Cancel()` |
| （有効時）Luaが要求したアセット一覧を取得 | `ScenarioController.GetRequestAssets()`（Luaの `Asset.Request` で積まれた一覧） |
| （有効時）メッセージ送りを実装 | `Message` 継承（`TagText` を実装）+ `OnRequestTextChangeAsObservable()` で表示文字列を受け取り、`RequestNext()` で次へ進める |
| （有効時）Luaから使える標準コマンド一覧 | `StandardCommand.CommandTypes`（`Command/StandardCommand.cs`） |
| （有効時）新しいシナリオコマンドを追加 | `ScenarioCommand` 継承 + `[CSharpCallLua]`（手順は「使い方」参照） |
| （有効時）Luaと値を受け渡し | `ScenarioController.GetValue<T>(key)` / `SetValue<T>(key, value)` |
| （有効時）シーン上のオブジェクトをLua名で管理 | `ManagedObjects.Add/Get/Remove`（Luaからは `CreateObject` / `GetObject`） |
| （有効時）演出全体の速度変更（スキップ等） | `ScenarioController.TimeScale`（`Modules.TimeUtil.TimeScale`） |
| ルビ付きTMPテキストの行高さ揃え | `RubyTextMeshProUGUI.InsertEmptyRubyTag(text)`（TMPro拡張。これも `ENABLE_XLUA` 内） |

## 使い方

実行フローの実シグネチャは `Modules/Scenario/ScenarioController.cs`、コマンド登録機構は `Modules/Lua.command/CommandLoader.cs` を参照。

### 新しいシナリオコマンドを追加する手順（有効化されている場合の参考）

1. `Modules.Scenario.Command` namespace に `ScenarioCommand` 継承の sealed クラスを作成する
2. クラスに `[CSharpCallLua]` 属性を付ける（無いと `CommandLoader` が実行時に `Debug.LogError`）
3. `LuaName`（Lua側関数名。`"Xxx.Yyy"` とドット区切りにするとLuaテーブル階層になる）と `Callback`（`nameof(LuaCallback)`）を override する
4. `public` な `LuaCallback` メソッドを定義する（引数がLua関数の引数になる。省略可能引数は `bool?` 等の Nullable、演出待ちが必要なら `async UniTask`、Luaへ値を返すなら戻り値を付ける）
5. 実装内では `scenarioController.ManagedObjects` / `AssetController` / `TaskController` / `TimeScale` を利用できる（`Setup` で注入済み）。`object target` 引数は `ToComponent<T>(target)` でコンポーネント化する
6. 利用側 `CommandLoader` 派生の `GetCommandTypes()` 戻り値に型を追加する（標準コマンドの追加位置は `Command/StandardCommand.cs` の `CommandTypes`）

要件（`CommandLoader.RegisterCommand` の検証項目）: `[CSharpCallLua]` 属性 / `ICommand` 実装 / デフォルトコンストラクタ の3点。

## 注意点・罠

- 有効化には (1) xLuaプラグイン導入、(2) `ENABLE_XLUA` 定義、(3) `ScenarioController` / `CommandLoader` / `LuaLoader` の派生実装、(4) Sound系はさらにCRI導入（→ [CriWare](../CriWare/Manual.md) の罠参照）が必要で、シンボル定義だけでは動かない。
- `ScenarioController` は MonoBehaviour ではない（シーンに置けない）。かつ abstract のため必ず派生実装が要る。
- コマンド登録は実行時リフレクション。`[CSharpCallLua]` 属性・`ICommand` 実装・デフォルトコンストラクタのどれが欠けても実行時 `Debug.LogError`（コンパイルエラーにならない）。
- 省略可能引数は Nullable で受けるのが規約（例: `bool? sync`）。Lua側で nil を渡せる。
- `Message.TagText`（abstract プロパティ）・`FadeIn/FadeOut/FadeColor.TargetGraphic` は利用側からの注入が前提。未設定のまま該当コマンドを呼ぶと NullReference になる。`TextLoad.EditAssetPathCallback` は任意（未設定ならアセットパスをそのまま使う）。
- `Message` / `AssetLoad<T>` / `PlaySound` は abstract で `StandardCommand.CommandTypes` に含まれない。メッセージ送りは利用側で `Message` の派生を作り `GetCommandTypes()` に足す。
- `Message` の本文は `[p]` でページ、`[w]` で入力待ちに区切られる。区切りごとに `RequestNext()` が呼ばれるまで待つ。
- `TimeScale` は2系統ある: `ScenarioController.TimeScale`（`Modules.TimeUtil.TimeScale`。メッセージ送り・アニメ/パーティクルの `SpeedRate` に反映）と `TweenControl.TimeScale`（DOTween側）。連動は自動ではない。
- `RubyTextMeshProUGUIExtension` は namespace `TMPro` に定義されている（`Modules.Scenario` ではない）。内部でリフレクションにより `RubyTextMeshProUGUI` の private フィールド（`rubyScale` / `m_maxFontSize`）へアクセスしており、ThirdParty更新で壊れうる。
- Sound系コマンドはUnityAudio版 `SoundManagement` に非対応（CRIの `CueInfo` 前提）。CRI導入時のみ動作する。

## 関連

- [Lua](../Lua/Manual.md) — `Modules.Lua` / `Lua.Command` / `Lua.Text`。本モジュールの実行基盤（同じく `ENABLE_XLUA` 定義で有効化）
- [UtageExtension](../UtageExtension/Manual.md) — 別系統のADV/会話劇基盤（宴）
- [CriWare](../CriWare/Manual.md) — Sound系コマンドの前提
- [Sound](../Sound/Manual.md) — サウンド基盤
- [ExternalAssets](../ExternalAssets/Manual.md) — `AssetLoad<T>` / `PlaySound` が使用する配信アセット基盤
- [Animation](../Animation/Manual.md) — `PlayAnimation` が操作する `AnimationPlayer`
- [TagText](../TagText/Manual.md) / [TimeUtil](../TimeUtil/Manual.md) / [DoTweenExtension](../DoTweenExtension/Manual.md) / [Particle](../Particle/Manual.md)
