# UniModules

> A general-purpose Unity game foundation library built on UniTask and R3. Documentation is written in Japanese.

Unity 向けの汎用ゲーム基盤ライブラリです。
スマートフォン向け運用型ゲームで毎回必要になる機能（シーン遷移・ポップアップ・マスターデータ配信・ローカライズ・配信アセット・課金など）を、UniTask + R3 ベースの統一した作法で提供します。
拡張メソッド・基盤クラス群（`Extensions/`）と、機能単位の約 60 個のモジュール（`Modules/`）で構成され、特定のゲームタイトルには依存しません。

利用側プロジェクトの `Assets/` 配下へ git submodule として組み込み、基盤が提供する abstract / ジェネリック基底を継承してプロジェクトの骨格にする使い方を前提にしています。拡張メソッド集としての部分利用は想定していません。

## 特徴

- **UniTask + R3 ベース** — 非同期は `UniTask`、イベント通知は R3 の `Observable` で統一
- **画面基盤** — シーン遷移（`Scene`）・ポップアップ（`Window`）・View と ViewModel の接続（`View`）・uGUI 拡張と仮想スクロール（`UI`）
- **データ基盤** — マスターデータ配信（`Master`）・ローカライズテキスト（`TextData`）・ローカル永続化（`LocalData`）・配信アセット（`ExternalAssets`）
- **演出の await 統一** — Animator / DOTween / ParticleSystem の再生を `await` で終了待ちでき、再生速度も一括制御できる
- **開発支援** — エディタ拡張の共通部品・実機デバッグ・ビルド支援（`Devkit`）、Prefab のオフスクリーン描画と検査（`Automation`）
- **外部 SDK 連携はオプトイン** — CRIWARE / xLua / 宴 / Vivox 等はシンボル未定義ならコンパイル対象外

## 導入前に知っておくこと

採用の判断に影響する設計方針と制約です。

- **基盤は abstract / ジェネリック基底を提供し、具象は利用側で実装する** — uGUI ラッパー・`SceneBase` 派生・`PopupManager<T>` 派生・サウンド入口ラッパー等は利用側で定義します
- **明示的初期化** — 新規コードでは `Awake` / `Start` を使わず、`Initialize()` / `Setup()` を利用側の初期化フローから呼び出します（既存コードには Unity ライフサイクルメソッドを使う箇所が残っています）
- **イベント公開は Subject の遅延初期化** — `OnXxxAsObservable()` で公開し、購読側は `.AddTo(...)` で寿命に紐づけます
- **GameObject 操作は `UnityUtility` 経由** — 生成・破棄・`SetActive` 等を統一します
- **Assembly Definition を持たない** — 全コードが `Assembly-CSharp` に含まれます。asmdef から `Assembly-CSharp` は参照できないため、UniModules を使うコードも `Assembly-CSharp` に置く必要があります。外部 SDK 連携以外のモジュールをコンパイル対象から外すことはできません
- **データ変換ツールは同梱しない** — `Master` の `.record` 生成や `TextData` の Excel と yaml の相互変換は利用側でツールを用意します
- **フォルダ名と namespace が一致しないモジュールがある** — 例: `AmazonWebService/` は `Modules.Amazon.S3`、`Camera/` は `Modules.FixedAspectCamera`、`Prefs/` は `Extensions`。一覧は [Documents~/Overview.md](Documents~/Overview.md) を参照してください

## 動作環境

- Unity 6（6000.x）で検証。旧バージョン向けの分岐コードは残っていますが未検証です
- エディタ: Windows / macOS
- ターゲット: iOS / Android / Windows（`UNITY_STANDALONE_WIN` 専用モジュールあり）
- レンダーパイプライン: URP 前提の機能は `ENABLE_UNIVERSALRENDERPIPELINE` 定義時のみ有効

## 依存ライブラリ

依存の強さを 3 段階に分けています。1 は導入時に必ず揃え、2 と 3 は使う機能に応じて追加してください。

### 1. 基盤全体が依存するライブラリ（必須）

`Extensions/` や複数の中核モジュールが条件付きコンパイル無しで参照しています。いずれか 1 つでも欠けると UniModules 全体がコンパイルできません。

| ライブラリ | 主な参照箇所 | 導入時の注意 |
|---|---|---|
| [UniTask](https://github.com/Cysharp/UniTask) | `Extensions` と 40 以上のモジュール | 非同期処理の基盤 |
| [R3](https://github.com/Cysharp/R3) | `Extensions`（`LifetimeDisposable` / `UnityUtility`）と約 50 モジュール | R3 本体（NuGet）と `R3.Unity`（UPM）の 2 つが必要。UniRx とは別物 |
| [LINQ to GameObject](https://github.com/neuecc/LINQ-to-GameObject-for-Unity)（`Unity.Linq`） | `Extensions`（`RectTransformExtensions`）/ UI / View / Scene / Animation / Particle / Resolution | 階層走査（`Descendants` / `Ancestors` 等） |
| Newtonsoft.Json | `Extensions`（`ObjectExtensions`）/ Prefs / Network / Devkit | `com.unity.nuget.newtonsoft-json` |
| [MessagePack for C#](https://github.com/MessagePack-CSharp/MessagePack-CSharp) | `Extensions`（`MessagePackFileUtility`）/ Master / LocalData / FileCache / Network | v3 系。本体（NuGet）と Unity 用パッケージ（UPM）の 2 つが必要。Resolver の選択は下記「動作切替シンボル」参照 |
| [DOTween](http://dotween.demigiant.com/) | `Extensions`（`ScrollRectExtensions`）/ UI（`VirtualScroll`）/ DoTweenExtension | `Tweener.ToUniTask()` を無条件で使うため `UNITASK_DOTWEEN_SUPPORT` の定義が必須。DOTween Utility Panel で asmdef を作成しておく |
| TextMesh Pro | UI / TextData / Hyphenation / Devkit | Unity 6 では `com.unity.ugui` に同梱 |
| [YamlDotNet](https://github.com/aaubry/YamlDotNet) | `Extensions/Utility/Editor`（`SerializationFileUtility`）/ TextData / BehaviorControl の各 Editor | エディタ専用。エディタのコンパイルに必須（ランタイムには不要） |

### 2. プラットフォーム条件で特定モジュールが依存するライブラリ

シンボルでは無効化できず、対象プラットフォームでビルドする時点で必要になります。

| ライブラリ | 依存するモジュール | 条件 |
|---|---|---|
| Mobile Notifications（`com.unity.mobile.notifications`） | Notifications | iOS / Android の実機ビルド時（エディタでは参照されない） |

### 3. シンボルでオプトインする特定モジュールの依存ライブラリ

対応するシンボルを定義した場合のみ、そのモジュールがコンパイルされます。SDK を導入していない環境では定義しないでください。

| ライブラリ | 有効化シンボル | コンパイル対象になるモジュール |
|---|---|---|
| Unity IAP | `UNITY_PURCHASING`（レシート検証は `RECEIPT_VALIDATION`） | InAppPurchasing |
| URP | `ENABLE_UNIVERSALRENDERPIPELINE` | Rendering |
| Unity Timeline | `ENABLE_UNITY_TIMELINE` | TimeLine |
| PlayFab CSharpSDK | `ENABLE_PLAYFAB_CSHARP` | PlayFab |
| Bugsnag | `ENABLE_BUGSNAG` | Bugsnag |
| SRDebugger | `ENABLE_SRDEBUGGER`（`FORCE_SRDIAGNOSIS_ENABLE` でリリースビルドでも有効化） | Devkit の SRDebugger 連携 |
| AWS SDK for .NET（S3） | `ENABLE_AMAZON_WEB_SERVICE` | AmazonWebService と、Devkit / ExternalAssets の S3 アップロード |
| Google GData | `ENABLE_GOOGLE_GDATA` | Devkit の Spreadsheet 連携 |
| Visual Studio Tools for Unity | `ENABLE_VSTU` | Devkit の Visual Studio プロジェクト生成 |
| CRIWARE | `ENABLE_CRIWARE_ADX` / `ENABLE_CRIWARE_ADX_LE` / `ENABLE_CRIWARE_SOFDEC` / `ENABLE_CRIWARE_FILESYSTEM`（追加機能: `ENABLE_CRIWARE_POS3D` / `ENABLE_CRIWARE_LIPSYNC`） | CriWare / Movie / Sound の CRI 実装 / ExternalAssets の CRI アセット配信 |
| xLua | `ENABLE_XLUA` | Lua / Lua.command / Lua.text / Scenario |
| 宴（Utage） | `ENABLE_UTAGE` | UtageExtension |
| Live2D Cubism SDK | `ENABLE_LIVE2D` | Live2D |
| Vivox | `ENABLE_VIVOX` | Vivox |
| UniWebView / Embedded Browser | `ENABLE_UNIWEBVIEW` / `ENABLE_EMBEDDEDBROWSER` | WebView の実装 Content（基底クラスは常時コンパイル） |
| （Unity 組込） | `UNITY_STANDALONE_WIN` | StandAloneWindows |

Sound は CRIWARE のシンボルが無い場合、Unity Audio の実装（DOTween 使用）に切り替わります。

### 動作切替シンボル

外部ライブラリの有無ではなく、UniModules の挙動を切り替えるシンボルです。

| シンボル | 挙動 |
|---|---|
| `MESSAGEPACK_ANALYZER_CODE` | 定義時は Source Generator が出力する `GeneratedMessagePackResolver` を使用。未定義時は mpc（`MessagePackCodeGenerator` メニュー）で生成した `GeneratedResolver` を使用。どちらか一方の生成物が必要 |
| `ENABLE_DEVKIT` | リリースビルドでも `UnityConsole` 等の開発機能を有効化 |

シンボルは利用側の `Assets/csc.rsp` または Scripting Define Symbols で定義します。

## 導入

1. 利用側プロジェクトの `Assets/` 配下へ submodule として追加します。

   ```bash
   git submodule add https://github.com/oTAMAKOo/UniModules.git Assets/UniModules
   ```

2. 「依存ライブラリ」の 1（必須）を導入し、使う機能に応じて 2・3 を追加します。
3. 使用する外部 SDK に合わせてシンボルを `Assets/csc.rsp` に定義します（例）。

   ```text
   -define:MESSAGEPACK_ANALYZER_CODE
   -define:UNITASK_DOTWEEN_SUPPORT
   -define:ENABLE_UNIVERSALRENDERPIPELINE
   ```

`Documents~/` は末尾の `~` により Unity のインポート対象外です。GitHub 上またはエディタ外から参照してください。

## ディレクトリ構成

```text
UniModules/
├── Scripts/
│   ├── Extensions/   拡張メソッド・基盤クラス（主な namespace: Extensions / Extensions.Serialize / Extensions.Devkit）
│   ├── Modules/      機能モジュール群（namespace: Modules.*）
│   └── Editor/       Modules.EditorMenu（利用側で継承して [MenuItem] を追加）
├── Documents~/       リファレンスドキュメント（INDEX.md から辿る）
├── CLAUDE.md         基盤コードの編集ルール（AI コーディング支援向けの記述を含む）
└── LICENSE
```

## モジュール一覧

各モジュールの API・使い方・注意点は [Documents~/INDEX.md](Documents~/INDEX.md) のやりたいこと逆引きと個別ドキュメントを参照してください。以下はフォルダ名の一覧です。

- **Extensions**: [Methods](Documents~/Extensions/Methods.md)（汎用拡張メソッド）/ [Core](Documents~/Extensions/Core.md)（`Singleton<T>` / `LifetimeDisposable` / `UnityUtility` / `Prefab` / インスペクタ属性）/ [Devkit](Documents~/Extensions/Devkit.md)（エディタ拡張の共通部品）
- **画面・UI**: [Scene](Documents~/Modules/Scene.md) / [Window](Documents~/Modules/Window.md) / [View](Documents~/Modules/View.md) / [UI](Documents~/Modules/UI.md) / [TextData](Documents~/Modules/TextData.md) / [Localize](Documents~/Modules/Localize.md) / [Resolution](Documents~/Modules/Resolution.md) / [InputControl](Documents~/Modules/InputControl.md) / [BackKey](Documents~/Modules/BackKey.md) / [TouchEffect](Documents~/Modules/TouchEffect.md) / [OffScreenRendering](Documents~/Modules/OffScreenRendering.md) / [Hyphenation](Documents~/Modules/Hyphenation.md) / [TagText](Documents~/Modules/TagText.md) / [SpriteAnimation](Documents~/Modules/SpriteAnimation.md) / [PatternTexture](Documents~/Modules/PatternTexture.md)
- **データ・通信**: [Master](Documents~/Modules/Master.md) / [LocalData](Documents~/Modules/LocalData.md) / [Prefs](Documents~/Modules/Prefs.md) / [Cache](Documents~/Modules/Cache.md) / [FileCache](Documents~/Modules/FileCache.md) / [Crypto](Documents~/Modules/Crypto.md) / [ExternalAssets](Documents~/Modules/ExternalAssets.md) / [Network](Documents~/Modules/Network.md) / [MessagePack](Documents~/Modules/MessagePack.md) / [Notifications](Documents~/Modules/Notifications.md) / [StorePage](Documents~/Modules/StorePage.md)
- **演出・サウンド・描画**: [Sound](Documents~/Modules/Sound.md) / [Animation](Documents~/Modules/Animation.md) / [DoTweenExtension](Documents~/Modules/DoTweenExtension.md) / [Particle](Documents~/Modules/Particle.md) / [ObjectPool](Documents~/Modules/ObjectPool.md) / [Camera](Documents~/Modules/Camera.md) / [Shaders](Documents~/Modules/Shaders.md) / [SortingLayerSetter](Documents~/Modules/SortingLayerSetter.md) / [Renderer2D](Documents~/Modules/Renderer2D.md)
- **制御・ユーティリティ**: [ApplicationEvent](Documents~/Modules/ApplicationEvent.md) / [StateControl](Documents~/Modules/StateControl.md) / [TimeUtil](Documents~/Modules/TimeUtil.md) / [Performance](Documents~/Modules/Performance.md) / [DeviceOrientation](Documents~/Modules/DeviceOrientation.md) / [PathFinding](Documents~/Modules/PathFinding.md) / [BehaviorControl](Documents~/Modules/BehaviorControl.md) / [R3Extension](Documents~/Modules/R3Extension.md) / [UniTaskExtension](Documents~/Modules/UniTaskExtension.md)
- **開発支援**: [Devkit](Documents~/Modules/Devkit.md) / [Automation](Documents~/Modules/Automation.md)
- **外部 SDK 連携（シンボルでオプトイン）**: [InAppPurchasing](Documents~/Modules/InAppPurchasing.md) / [Rendering](Documents~/Modules/Rendering.md) / [TimeLine](Documents~/Modules/TimeLine.md) / [PlayFab](Documents~/Modules/PlayFab.md) / [Bugsnag](Documents~/Modules/Bugsnag.md) / [AmazonWebService](Documents~/Modules/AmazonWebService.md) / [CriWare](Documents~/Modules/CriWare.md) / [Movie](Documents~/Modules/Movie.md) / [Lua](Documents~/Modules/Lua.md)（`Lua` / `Lua.command` / `Lua.text` の 3 フォルダ）/ [Scenario](Documents~/Modules/Scenario.md) / [UtageExtension](Documents~/Modules/UtageExtension.md) / [Live2D](Documents~/Modules/Live2D.md) / [Vivox](Documents~/Modules/Vivox.md) / [WebView](Documents~/Modules/WebView.md) / [StandAloneWindows](Documents~/Modules/StandAloneWindows.md)

## ドキュメント

| ファイル | 内容 |
|---|---|
| [Documents~/INDEX.md](Documents~/INDEX.md) | 入口。やりたいこと逆引き・全モジュール一覧 |
| [Documents~/Overview.md](Documents~/Overview.md) | 全体構造・依存ライブラリ・共通パターン・namespace 不一致一覧 |
| [Documents~/Extensions/Methods.md](Documents~/Extensions/Methods.md) | 汎用処理を書く前に確認する拡張メソッド一覧 |
| [CLAUDE.md](CLAUDE.md) | 基盤コードの編集ルール（コーディングスタイル・プロジェクト非依存・影響調査） |

## ライセンス

[MIT License](LICENSE) — Copyright (c) 2018 Makoto Takeuchi

利用・改変・再配布は自由です。再配布時は著作権表示とライセンス文を同梱してください。無保証で提供されます。
