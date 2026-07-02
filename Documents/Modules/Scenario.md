# Scenario

> **namespace**: `Modules.Scenario` / `Modules.Scenario.Command`（`RubyTextMeshProUGUIExtension` のみ `TMPro`）
> **場所**: `Client/Assets/UniModules/Scripts/Modules/Scenario/`
> **Client側使用**: 0ファイル（using 0・プレハブ/シーンからのGUID参照 0、2026-07時点）
> **依存**: xLua（`XLua` namespace、**未導入**） / Modules.Lua系（`Modules.Lua` / `Modules.Lua.Command` / `Modules.Lua.Text`） / UniTask / R3 / DOTween（`Modules.Tweening` = `DoTween/` フォルダ） / Modules.TimeUtil / Modules.TagText（実namespaceは `Modules.TagTect`） / Modules.ExternalAssets / Modules.Animation / Modules.Particle / Modules.Sound + CRI（Sound系コマンドのみ） / RubyTextMeshPro（ThirdParty） / Extensions

## 概要

Luaスクリプト（xLua）でシナリオ・カットシーン進行を記述するための基盤。C#の演出コマンド群（表示・移動・フェード・サウンド等 約46種）をLua関数として自動公開し、Lua側から `Wait(1.0)` / `Move(obj, ...)` のように呼び出して演出を制御する。

**本プロジェクトでは未使用（コンパイル対象外）**。全ファイルが `#if ENABLE_XLUA` でガードされており、`ENABLE_XLUA` は Scripting Define Symbols（`Client/ProjectSettings/ProjectSettings.asset`）にも `Client/Assets/csc.rsp` にも未定義。xLuaプラグイン自体も Assets に存在しない。Sound系コマンド8種と `SoundController` はさらに `ENABLE_CRIWARE_ADX(_LE)` も必要（→ [CriWare](CriWare.md)、こちらも未定義）で二重に無効。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| **会話劇・カットシーンを実装したい（本プロジェクト）** | このモジュールは**使えない**（xLua未導入・コンパイル対象外）。既存のClient側実装を探すかユーザーに設計相談すること |
| このモジュールを有効化したい | xLuaプラグイン導入 + `ENABLE_XLUA` 定義 + `ScenarioController` / `CommandLoader` / `LuaLoader` の派生実装（罠参照） |
| （有効時）シナリオ実行の入口 | `ScenarioController` 派生を `Setup()` → `await Prepare()` → `await Execute(luaFunction)` |
| （有効時）Luaから使える標準コマンド一覧 | `StandardCommand.CommandTypes`（下表） |
| （有効時）新しいシナリオコマンドを追加 | `ScenarioCommand` 継承 + `[CSharpCallLua]`（手順は「使い方」参照） |
| （有効時）Luaと値を受け渡し | `ScenarioController.GetValue<T>(key)` / `SetValue<T>(key, value)` |
| （有効時）シーン上のオブジェクトをLua名で管理 | `ManagedObjects.Add/Get/Remove`（Luaからは `CreateObject` / `GetObject`） |
| （有効時）演出全体の速度変更（スキップ等） | `ScenarioController.TimeScale`（`Modules.TimeUtil.TimeScale`） |
| ルビ付きTMPテキストの行高さ揃え | `RubyTextMeshProUGUI.InsertEmptyRubyTag(text)`（TMPro拡張。これも `ENABLE_XLUA` 内） |

## 主要クラス

全クラスが `#if ENABLE_XLUA` 内のため**本プロジェクトでは一切コンパイルされない**。

| クラス | 種別 | 役割 |
|---|---|---|
| `ScenarioController` | abstract class（非MonoBehaviour） | 中核。Lua環境構築（`Setup`）・Lua関数実行（`Prepare`/`Execute`）・各サブコントローラ保持。派生で `CreateLuaLoader` / `CreateCommandLoader` / `GetCryptoKey` を実装する |
| `ScenarioCommand` | abstract class（`ICommand` 実装） | 全コマンドの基底。`LuaName`（Lua側関数名）と `Callback`（呼び出すC#メソッド名）を定義。`scenarioController` 参照を保持 |
| `StandardCommand` | static class | 標準コマンド46種の `Type[] CommandTypes` 定義（`CommandLoader` 派生から登録に使う） |
| `ManagedObjects` | sealed class | 文字列キー→object の管理辞書。`CreateObject` で登録されLuaからキーで参照される |
| `AssetController` | sealed class | アセットの事前リクエスト一覧・遅延ロードキュー・ロード済みアセット辞書 |
| `TaskController` | sealed class | 名前付き `UniTask` リスト管理。並行演出を `TaskRun` でまとめて完了待ちする |
| `SoundController` | sealed class（LifetimeDisposable、**CRI有効時のみ**） | シナリオ中に再生した `SoundElement` の追跡・一括停止用 |
| `TweenControl` | Singleton（`Extensions.Singleton<T>`） | DOTween の Tweener を `Modules.Tweening.TweenController` 経由で再生。ease名文字列→ `Ease` enum 解決 |
| `RubyTextMeshProUGUIExtension` | static class（namespace `TMPro`） | ルビタグを含む行の先頭に空ルビタグを挿入し行高さを揃える拡張メソッド |
| `Message` | abstract class（`Command/Message/`） | メッセージ送りコマンド基底。`[w]`（クリック待ち）/`[p]`（改ページ）タグ解析・1文字ずつ表示・`TimeScale` 連動の早送り。派生で `TagText` を注入する |
| `AssetLoad<T>` | abstract class（`Command/Asset/`） | アセットロードコマンド基底。`ExternalAsset.LoadAsset<T>` して `AssetController` に格納。`immediate` 指定で即時/キュー切替 |
| `PlaySound` | abstract class（`Command/Sound/`、CRI有効時のみ） | サウンド再生コマンド基底。`ExternalAsset.GetCueInfo` → `SoundManagement.Play` |

### 標準コマンド一覧（`StandardCommand.CommandTypes`・Lua側から呼ぶ関数）

「型」列: async = C#側が `async UniTask`（Lua側では自動生成ラッパーが `await(...)` で完了待ち）。省略可能引数は C# 側 Nullable（Luaから nil 可）。

| LuaName | クラス | 引数（→戻り値） | 型 | 内容 |
|---|---|---|---|---|
| `Asset.Request` | `AssetRequest` | `(target)` | 同期 | 事前DLが必要なアセットパスを登録（`GetRequestAssets` で回収） |
| `Asset.LoadInQueue` | `AssetLoadInQueue` | `()` | async | キュー済みロードタスクを一括実行し完了待ち |
| `TaskRun` | `TaskRun` | `(taskName)` | async | 名前付きタスク群の完了を待つ |
| `RemoveTask` | `RemoveTask` | `(taskName)` | 同期 | 名前付きタスクを破棄 |
| `TextLoad` | `TextLoad` | `(assetPath, immediate?)` | async | `LuaTextAsset` をロードし `LuaText` に設定（`AssetLoad<LuaTextAsset>` 派生） |
| `Text` | `GetText` | `(id) → string` | 同期 | `LuaText` からID指定でテキスト取得 |
| `Wait` | `Wait` | `(value)` | async | 指定秒数待機 |
| `Show` / `Hide` | `Show` / `Hide` | `(target)` | 同期 | `SetActive(true/false)` |
| `CreateObject` | `CreateObject` | `(name, parent, assetPath) → GameObject` | 同期 | ロード済プレハブを生成し `ManagedObjects` に `name` で登録 |
| `DeleteObject` | `DeleteObject` | `(target)` | 同期 | 削除して `ManagedObjects` から除去 |
| `DeleteAllObject` | `DeleteAllObject` | `()` | 同期 | 管理オブジェクトを全削除 |
| `GetObject` | `GetObject` | `(key) → object` | 同期 | `ManagedObjects` からキーで取得 |
| `SetPriority` | `SetPriority` | `(target, priority)` | 同期 | `SetSiblingIndex` で描画順変更 |
| `Move` / `MoveX` / `MoveY` / `MoveZ` | 同名クラス | `(target, endValue, duration?, ease?)` | async | ワールド座標移動（`DOMove` 系）。`duration` 省略で即時セット |
| `LocalMove` / `LocalMoveX/Y/Z` | 同名クラス | 同上 | async | ローカル座標移動 |
| `Rotate` / `RotateX/Y/Z` | 同名クラス | 同上 | async | 回転 |
| `Scale` / `ScaleX/Y/Z` | 同名クラス | 同上 | async | スケール |
| `Shake` | `Shake` | `(target, duration, strength, sync?, vibrato?, randomness?)` | async | `DOShakePosition`。`sync=false`（既定）で待たずに進行 |
| `PlayAnimation` | `PlayAnimation` | `(target, animation, sync?)` | async | `AnimationPlayer.Play`。`TimeScale` を `SpeedRate` に反映 |
| `StopAnimation` | `StopAnimation` | `(target)` | 同期 | `AnimationPlayer.Stop` |
| `PlayParticle` | `PlayParticle` | `(target, sync?)` | async | `ParticlePlayer.Play`。`TimeScale` 反映 |
| `StopParticle` | `StopParticle` | `(target)` | 同期 | `ParticlePlayer.Stop` |
| `FadeIn` | `FadeIn` | `(duration, endValue?, ease?)` | async | `TargetGraphic` を `DOFade`（既定 endValue=0 / OutQuad） |
| `FadeOut` | `FadeOut` | `(duration, endValue?, ease?)` | async | 同上（既定 endValue=1） |
| `FadeColor` | `FadeColor` | `(color)` | 同期 | `TargetGraphic.color` を直接変更 |
| `PlayBgm` / `PlaySe` / `PlayVoice` / `PlayJingle` / `PlayAmbience` | `PlaySound` 派生 | `(resourcePath, cue) → SoundElement` | async | **CRI有効時のみ登録**。`ExternalAsset.GetCueInfo` → `SoundManagement.Play(SoundType.Xxx)` |
| `PauseSound` / `StopSound` | 同名クラス | `(sound)` | 同期 | **CRI有効時のみ**。`SoundManagement.Pause/Stop(SoundElement)` |
| `StopAllSound` | `StopAllSound` | `()` | 同期 | **CRI有効時のみ**。`SoundController.Elements` を全停止 |

## 使い方(実例)

Client側・基盤内ともに使用例が存在しないため、**実コードのシグネチャから構成した最小の想定例**（動作確認不可。`ENABLE_XLUA` 有効化が前提）。

### 想定例1: ScenarioController 派生と実行フロー

```csharp
// 想定例（実在コードではない）. 実シグネチャは
// Client/Assets/UniModules/Scripts/Modules/Scenario/ScenarioController.cs 参照.
public sealed class CutsceneController : ScenarioController
{
    protected override LuaLoader CreateLuaLoader() { return new CutsceneLuaLoader(); }

    protected override CommandLoader CreateCommandLoader() { return new CutsceneCommandLoader(); }

    protected override AesCryptoKey GetCryptoKey() { return new AesCryptoKey("--- key ---", "--- iv ---"); }
}

// 実行側.
var controller = new CutsceneController();

controller.Setup(luaPath, luaReference);

await controller.Prepare("OnPrepare");

// Asset.Request で登録されたアセットを事前DLする場合はここで controller.GetRequestAssets() を使用.

await controller.Execute("OnExecute", cancelToken);
```

### 想定例2: CommandLoader 派生（コマンド登録）

```csharp
// 想定例（実在コードではない）. 登録機構は
// Client/Assets/UniModules/Scripts/Modules/Lua.command/CommandLoader.cs 参照.
public sealed class CutsceneCommandLoader : CommandLoader
{
    protected override IEnumerable<Type> GetCommandTypes()
    {
        // 標準コマンド + プロジェクト独自コマンド.
        return StandardCommand.CommandTypes.Append(typeof(MyCustomCommand));
    }
}
```

### 想定例3: Luaスクリプト側の呼び出し

```lua
-- 想定例. CommandLoader が LuaName から自動生成する関数定義に基づく.
-- 例: LuaName "Asset.Request" → Asset = { Request = function(target) ... }
Asset.Request("Scenario/Cutscene001/Character")
Asset.LoadInQueue()

local chara = CreateObject("chara01", parentObject, "Scenario/Cutscene001/Character")

Move(chara, targetPosition, 0.5, "OutQuad")
Wait(1.0)
FadeOut(0.3)
```

### 新しいシナリオコマンドを追加する手順（有効化されている場合の参考）

1. `Modules.Scenario.Command` namespace に `ScenarioCommand` 継承の sealed クラスを作成する
2. クラスに `[CSharpCallLua]` 属性を付ける（無いと `CommandLoader` が実行時に `Debug.LogError`）
3. `LuaName`（Lua側関数名。`"Xxx.Yyy"` とドット区切りにするとLuaテーブル階層になる）と `Callback`（`nameof(LuaCallback)`）を override する
4. `public` な `LuaCallback` メソッドを定義する（引数がLua関数の引数になる。省略可能引数は `bool?` 等の Nullable、演出待ちが必要なら `async UniTask`、Luaへ値を返すなら戻り値を付ける）
5. 実装内では `scenarioController.ManagedObjects` / `AssetController` / `TaskController` / `TimeScale` を利用できる（`Setup` で注入済み）。`object target` 引数は `ToComponent<T>(target)` でコンポーネント化する
6. プロジェクト側 `CommandLoader` 派生の `GetCommandTypes()` 戻り値に型を追加する（標準コマンドの追加位置は `Command/StandardCommand.cs` の `CommandTypes`）

要件（`CommandLoader.RegisterCommand` の検証項目）: `[CSharpCallLua]` 属性 / `ICommand` 実装 / デフォルトコンストラクタ の3点。

## API(主要公開メンバー)

### ScenarioController（abstract）

| メンバー | 説明 |
|---|---|
| `Setup(string luaPath, LuaReference luaReference)` | Lua環境・全サブコントローラ・コマンド群を構築（各 `ScenarioCommand.Setup(this)` も実行） |
| `Prepare(string luaFunction) : UniTask` | Luaファイルロード後、準備用Lua関数を実行 |
| `Execute(string luaFunction, CancellationToken) : UniTask` | メインのLua関数を実行（例外はcatchして `Debug.LogException`） |
| `GetRequestAssets() : string[]` | `Asset.Request` で登録された事前DL対象アセットパス一覧 |
| `GetValue<T>(string key)` / `SetValue<T>(string key, T value)` | Luaグローバル変数の読み書き |
| `Cancel()` | 実行中シナリオのキャンセル（内部 `CancellationTokenSource` を作り直す） |
| `LuaPath` / `LuaReference` / `LuaController` / `LuaLoader` / `LuaText` / `CommandLoader` / `TimeScale` / `ManagedObjects` / `AssetController` / `TaskController` / `SoundController`(CRI時) | 各サブシステムへの参照プロパティ |
| `CreateLuaLoader()` / `CreateCommandLoader()` / `GetCryptoKey()` | **abstract**。派生で実装必須 |

### ScenarioCommand（abstract） / ManagedObjects / AssetController / TaskController

| メンバー | 説明 |
|---|---|
| `ScenarioCommand.LuaName` / `Callback` | abstract。Lua関数名 / 呼び出すメソッド名 |
| `ScenarioCommand.Setup(ScenarioController)` | controller 注入（`ScenarioController.Setup` から自動で呼ばれる） |
| `ScenarioCommand.ToComponent<T>(object target)` | static。GameObject/Component いずれで渡されても `T` に解決 |
| `ManagedObjects.Add(key, target)` / `Get(key)` / `Get<T>(key)` / `Remove(key or target)` / `GetAll()` / `Clear()` | Lua名⇔オブジェクトの管理。`Get<T>` はGameObjectからのコンポーネント解決付き。見つからない場合 `Debug.LogError` |
| `AssetController.AddRequest(target)` / `GetAllRequestAssets()` | 事前DLリクエストの登録・取得 |
| `AssetController.AddLoadTask(UniTask)` / `RunLoadTasks() : UniTask` | 遅延ロードキュー追加・一括実行 |
| `AssetController.SetLoadedAsset<T>(key, asset)` / `GetLoadedAsset<T>(key)` | ロード済アセットの格納・取得 |
| `TaskController.AddTask(taskName, UniTask)` / `ExecuteTask(taskName) : UniTask` / `RemoveTask(taskName)` / `Clear()` | 名前付き並行タスクの登録・完了待ち・破棄 |

### TweenControl / Message / RubyTextMeshProUGUIExtension

| メンバー | 説明 |
|---|---|
| `TweenControl.Play(Tweener tweener, string ease = null) : UniTask` | static。ease名（`Ease` enum名）を解決して再生・完了待ち |
| `TweenControl.TimeScale` | Tween再生速度（`Modules.Tweening.TweenController` に委譲。`ScenarioController.TimeScale` とは別物） |
| `Message.LuaCallback(string text, float? charDelayTime) : UniTask` | メッセージ表示（`[w]` クリック待ち / `[p]` 改ページ対応） |
| `Message.RequestNext()` | クリック等による表示送り要求 |
| `Message.OnRequestTextChangeAsObservable() : Observable<string>` | 表示テキスト更新通知（Viewが購読してTMPに反映する想定） |
| `Message.DefaultCharDelayTime` | 1文字あたり表示間隔（既定0.04秒） |
| `RubyTextMeshProUGUI.InsertEmptyRubyTag(string text) : string` | ルビ行の高さ揃え用に空ルビタグを行頭挿入 |

## 注意点・罠

- **本プロジェクトでは未使用（コンパイル対象外）**。シナリオ・カットシーン実装をこのモジュール前提で書かないこと。有効化には (1) xLuaプラグイン導入（Assetsに無い）、(2) `ENABLE_XLUA` 定義、(3) `ScenarioController` / `CommandLoader` / `LuaLoader` の派生実装、(4) Sound系はさらにCRI導入（→ [CriWare](CriWare.md) の罠参照）が必要で、シンボル定義だけでは動かない。
- `ScenarioController` は MonoBehaviour ではない（シーンに置けない）。かつ abstract のため必ず派生実装が要る。
- コマンド登録は実行時リフレクション。`[CSharpCallLua]` 属性・`ICommand` 実装・デフォルトコンストラクタのどれが欠けても実行時 `Debug.LogError`（コンパイルエラーにならない）。
- 省略可能引数は Nullable で受けるのが規約（例: `bool? sync`）。Lua側で nil を渡せる。
- `Message.TagText`（abstract プロパティ）・`FadeIn/FadeOut/FadeColor.TargetGraphic`・`TextLoad.EditAssetPathCallback` はプロジェクト側からの注入が前提。未設定のまま該当コマンドを呼ぶと NullReference になる。
- `TimeScale` は2系統ある: `ScenarioController.TimeScale`（`Modules.TimeUtil.TimeScale`。メッセージ送り・アニメ/パーティクルの `SpeedRate` に反映）と `TweenControl.TimeScale`（DOTween側）。連動は自動ではない。
- `Message.cs` の using は `Modules.TagTect`（TagTextモジュールの実namespaceがこの綴り）。grep時に `TagText` で探すと見落とすので注意。
- `RubyTextMeshProUGUIExtension` は namespace `TMPro` に定義されている（`Modules.Scenario` ではない）。内部でリフレクションにより `RubyTextMeshProUGUI` の private フィールド（`rubyScale` / `m_maxFontSize`）へアクセスしており、ThirdParty更新で壊れうる。
- Sound系コマンドはUnityAudio版 `SoundManagement` に非対応（CRIの `CueInfo` 前提）。本プロジェクトのサウンド構成（→ [Sound](Sound.md)）とは互換がない。

## 関連

- Lua（ドキュメント未作成）— `Modules.Lua` / `Lua.Command` / `Lua.Text`。本モジュールの実行基盤（同じく `ENABLE_XLUA` でコンパイル対象外）
- [Utage](Utage.md) — 別系統のADV/会話劇基盤（宴）。こちらも本プロジェクトでは未使用
- [CriWare](CriWare.md) — Sound系コマンドの前提（未導入）
- [Sound](Sound.md) — 本プロジェクトの実サウンド基盤
- [ExternalAsset](ExternalAsset.md) — `AssetLoad<T>` / `PlaySound` が使用する配信アセット基盤
- [Animation](Animation.md) — `PlayAnimation` が操作する `AnimationPlayer`
- TagText / TimeUtil / DoTween / Particle（各ドキュメント未作成）
