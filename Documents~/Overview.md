# UniModules 基盤 Overview

`Client/Assets/UniModules` 全体の構造・共通パターン・横断的な罠のまとめ。個別モジュールの詳細は [INDEX.md](INDEX.md) から辿ること。

## 全体構造

```
Client/Assets/UniModules/Scripts/
├── Extensions/   … 拡張メソッド + 基盤クラス（namespace: Extensions / Extensions.Serialize / Extensions.Devkit）
├── Modules/      … 機能モジュール約60個（namespace: Modules.*）
└── Editor/       … EditorMenu 基底（利用側で継承して拡張）
```

- Extensions は最重要の共通層（拡張メソッド・Singleton・LifetimeDisposable 等）、Modules は機能単位の集まり
- 基盤は汎用に作られており、CRI・xLua・宴・Vivox 等の**外部SDK依存モジュールはシンボル未定義でコンパイル対象外（休眠）**。休眠一覧と有効化条件は [INDEX.md](INDEX.md) 末尾参照

## 依存ライブラリ

| 分野 | ライブラリ |
|---|---|
| Rx | **R3**（Cysharp）。UniRx ではない。移行対応表は [Modules/R3Extension.md](Modules/R3Extension.md) 冒頭 |
| 非同期 | UniTask（`Task` は使わない） |
| シリアライズ | MessagePack。コード生成は Source Generator で自動（csc.rsp の `MESSAGEPACK_ANALYZER_CODE` 有効化時）。手動生成は不要 |
| Tween | DOTween（`UNITASK_DOTWEEN_SUPPORT` 定義で `tweener.ToUniTask()` 可） |
| サーバー | PlayFab **CSharpSDK**（Unity SDKではない。Taskベース・例外は投げず `Error` 格納） |
| クラッシュレポート | Bugsnag（`ENABLE_BUGSNAG` 定義時のみ有効） |
| 実機デバッグ | SRDebugger（`ENABLE_SRDEBUGGER` 定義時のみ有効） |
| 描画 | URP（`ENABLE_UNIVERSALRENDERPIPELINE` 定義時のみ有効） |

シンボル定義は利用側の `csc.rsp`（+ ProjectSettings の Scripting Define Symbols）で管理する。

## 基盤設計の前提

- **基盤側は abstract / ジェネリック基底が多い**。実際の入口は利用側で具象クラスを実装するのが定石（例: uGUI ラッパー・SceneBase 派生・PopupManager 派生・サウンド入口ラッパー 等）
- 利用側の入口設計・実装パターンはプロジェクト側ドキュメントに集約する（この基盤ドキュメントは基盤自体の API と挙動のみを扱う）

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

ファイル名とクラス名の不一致: `FileDownloader.cs`→`FileDownLoader`、`DragTarget.cs`→`DragObject`、`ButtonInteractablemageSprite.cs`→`ButtonInteractableImageSprite`。

## ドキュメント群の歩き方

1. 実装したいことが決まっている → [INDEX.md](INDEX.md) の「やりたいこと逆引き」
2. モジュール名は知っている → INDEX のモジュール一覧から個別 .md へ
3. 汎用処理を書きそうになった → まず [Extensions/Methods.md](Extensions/Methods.md)
4. 基盤の挙動が不審 → 各 .md の「注意点・罠」を確認

> コード変更でドキュメントと実態がズレた場合は実コードを正とし、ドキュメントを更新すること。
