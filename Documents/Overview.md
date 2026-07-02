# UniModules 基盤 Overview

`Client/Assets/UniModules` 全体の構造・共通パターン・横断的な罠のまとめ。個別モジュールの詳細は [INDEX.md](INDEX.md) から辿ること。

## 全体構造

```
Client/Assets/UniModules/Scripts/
├── Extensions/   … 拡張メソッド + 基盤クラス（namespace: Extensions / Extensions.Serialize / Extensions.Devkit）
├── Modules/      … 機能モジュール約60個（namespace: Modules.*）
└── Editor/       … EditorMenu 基底（Client側 Dominion.Editor.EditorMenu が継承）
```

- 合計約830ファイル / 11万行。Client側コード（`Client/Assets/Scripts`、約1500ファイル）の**57%が Extensions を、ほぼ全てが何らかの Modules を使用**する
- 基盤は汎用に作られており、CRI・xLua・宴・Vivox 等の**外部SDK依存モジュールはシンボル未定義でコンパイル対象外（休眠）**。休眠一覧は [INDEX.md](INDEX.md) 末尾参照

## ライブラリの実態（規約より実コードを信じること）

| 分野 | 実態 |
|---|---|
| Rx | **R3**（UPM `com.cysharp.r3` v1.3.0）。**UniRx はライブラリごと不在**（`using UniRx` は0件）。ルートCLAUDE.mdの「UniRx」記述は歴史的名残で、パターン自体（Subject遅延生成+AddTo）はR3型で有効。移行対応表は [Modules/R3Extension.md](Modules/R3Extension.md) 冒頭 |
| 非同期 | UniTask v2.3.1（`Assets/ThirdParty/UniTask` に直接同梱）。`Task` は使わない |
| シリアライズ | MessagePack（git UPM v3系）。**コード生成は Source Generator で自動**（csc.rsp の `MESSAGEPACK_ANALYZER_CODE`）。手動生成は不要 |
| Tween | DOTween（`UNITASK_DOTWEEN_SUPPORT` 定義済み、`tweener.ToUniTask()` 可） |
| サーバー | PlayFab **CSharpSDK**（Unity SDKではない。Taskベース・例外は投げず `Error` 格納） |
| クラッシュレポート | Bugsnag（`ENABLE_BUGSNAG` 定義済み・実機のみ有効） |
| 実機デバッグ | SRDebugger（`ENABLE_SRDEBUGGER` 定義済み） |
| 描画 | URP（`ENABLE_UNIVERSALRENDERPIPELINE` 定義済み） |

シンボル定義の正は `Client/Assets/csc.rsp`（+ ProjectSettings の Scripting Define Symbols）。

## 基盤とClient側の二層構造【最重要】

基盤側は abstract / ジェネリック基底が多く、**実際に呼ぶ「入口」は Client 側の具象クラス**にある。基盤クラスを直接 new/継承する前に、以下の Client 側入口を必ず確認すること。

| 機能 | Client側の入口 | 場所 |
|---|---|---|
| UI部品 | `UIButton` / `UIText` / `UIImage` 等（`Dominion.Client`） | `Client/Assets/Scripts/Client/Core/UI/` |
| サウンド | `SoundPlayer.Bgm() / .Se() / .StopBgm()`（static） | Client/Core 配下 |
| サーバーAPI | `PlayFabManager`（Singletonファサード）+ `PlayFabCloudScript.Execute` | 同上 |
| シーン遷移 | `SceneManager`（`Dominion.Client`） | `Client/Core/Scene/` |
| 画面基底 | `SceneBase<TArgument, TViewModel>` / `WindowBase` | `Client/Core/Scene/` / `Client/Core/Popup/` |
| ポップアップ管理 | `PopupManager`（`Dominion.Client.Module.Window`） | Client側 |
| 通信中の入力ブロック+ローディング表示 | `LoadingScope`（using で囲む） | Client側 |
| 現在時刻 | `systemModel.LocalTime`（`DateTime.Now` 禁止） | `SystemModel.time.cs`（PlayFabサーバー時間ベース） |
| 起動時初期化 | `InitializeObject`（各マネージャーの生成起点） | Client側 |

## 共通実装パターン

- **Singleton**: マネージャーは `Singleton<T>`（非MonoBehaviour）/ `SingletonMonoBehaviour<T>` 継承 + `CreateInstance()`。詳細は [Extensions/Core.md](Extensions/Core.md)
- **明示的初期化**: Unityライフサイクル（Awake/Start）ではなく `Setup()` / `Initialize()` + `private bool initialized = false;` フラグで一度だけ実行（プロジェクトルールでもある）
- **イベント公開**: `private Subject<T> onXxx = null;` 遅延生成 + `OnXxxAsObservable()`、購読側は `.Subscribe(...).AddTo(this)` または `.AddTo(Disposable)`（`LifetimeDisposable`）
- **非同期**: `async UniTask`、fire-and-forget は `.Forget()`（GameObject寿命に連動させるなら `.Forget(component)`）
- **演出のawait**: AnimationPlayer / TweenController / ParticlePlayer はいずれも「Play を await すると終了まで待てる + 速度一括制御」で統一されている

## namespace とフォルダ名の不一致一覧【grep時の罠】

using を書く時・コードを探す時はフォルダ名でなく namespace を使うこと。

| フォルダ | 実際の namespace |
|---|---|
| `Modules/Network/` | `Modules.Net` / `Modules.Net.WebRequest` / `Modules.Net.WebDownload` |
| `Modules/PlayFab/` | `Modules.PlayFabCSharp` |
| `Modules/ExternalAsset/` | `Modules.ExternalAssets`（内部に `Modules.AssetBundles` も同居） |
| `Modules/BehaviourControl/` | `Modules.BehaviorControl`（英米綴り違い） |
| `Modules/TagText/` | `Modules.TagTect`（**誤記のまま実装されている**） |
| `Modules/Shader/` | `Modules.Shaders` |
| `Modules/Camera/` | `Modules.FixedAspectCamera` |
| `Modules/Prefs/` | `Extensions`（`SecurePrefs`） |
| `Modules/UniTask/` | `Modules.UniTaskExtension` |
| `Modules/DoTween/` | `Modules.Tweening` |
| `Modules/Renderer2D/` | `Modules.Renderer2D.DummyContent` |
| `Modules/AmazonWebService/` | `Modules.Amazon.S3` |
| `Modules/Utage/` | `Modules.UtageExtension` |
| `Modules/Devkit/ApiMonitor/` | `Modules.Net.WebRequest` |
| `Modules/Devkit/MasterGenerator/` | `Modules.Master` |
| `Extensions/Methods/Vector.cs` | `UnityEngine` |
| `Extensions/Devkit/Log/DebugLog.cs` | `Modules.Devkit.Log` |

## 既知の基盤バグ【2026-07 全12件対処済み】

ドキュメント作成時（2026-07）の全ファイル読解で「動かない・名前と挙動が違う」実装を12件発見し、**同月中に全件対処済み**（削除5件: `Compress<T>`/`Decompress<T>`・`Vector2Extensions.Reflect`・`IsShorterThan`/`IsLongerThan`・`EnumExtensions.HasFlag<T>` / 修正6件: `ColorScope`・`SortingLayerSetter`・`PathFinding.AStar`・`RecoveryValue.GetNextRecoveryTime`・`AsyncHandler`・`TextData.Format`・`SoundManagement.CrossFade` / 配線1件: `SceneArgument.RegisterHistory`）。各修正の経緯・挙動変化は該当モジュール .md の「注意点・罠」に記録されている。

挙動が変わった主な修正: BGM切替が真のクロスフェードに（Sound）、`TextData.Format` が未定義テキストでクラッシュせず空文字+エラーログに、シーン履歴が `RegisterHistory` プロパティで制御可能に（Scene）。

ファイル名とクラス名の不一致（現存）: `FileDownloader.cs`→`FileDownLoader`、`DragTarget.cs`→`DragObject`、`ButtonInteractablemageSprite.cs`→`ButtonInteractableImageSprite`。

## ドキュメント群の歩き方

1. 実装したいことが決まっている → [INDEX.md](INDEX.md) の「やりたいこと逆引き」
2. モジュール名は知っている → INDEX のモジュール一覧から個別 .md へ
3. 汎用処理を書きそうになった → まず [Extensions/Methods.md](Extensions/Methods.md)
4. 基盤の挙動が不審 → 各 .md の「注意点・罠」を確認

> 各ドキュメントは2026-07時点のコードを全ファイル読解して作成。「Client側使用」数値は同時点の目安。コード変更でドキュメントと実態がズレた場合は実コードを正とし、ドキュメントを更新すること。
