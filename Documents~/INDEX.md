# UniModules 基盤リファレンス INDEX

`Client/Assets/UniModules`（プロジェクト共通ゲーム基盤）のリファレンスドキュメント群。

**このドキュメントの目的**: Claude が実装時に基盤の既存機能を漏れなく活用し、車輪の再発明をしないこと。

## 使い方（Claude向け）

1. Client側の実装前に、まずこの INDEX の「やりたいこと逆引き」を確認する
2. 該当する個別 .md **だけ** を開いて参照する（全ドキュメントを読み込まない）
3. 汎用処理（文字列・コレクション・GameObject操作等）を書く前に [Extensions/Methods.md](Extensions/Methods.md) を確認（車輪の再発明防止の最重要ファイル）
4. 基盤全体の構造・共通パターン・既知の罠は [Overview.md](Overview.md) を参照
5. **「休眠（コンパイル対象外）」のモジュールを使う実装は提案しない**（有効化にはSDK導入等が必要）

## やりたいこと逆引き

### 汎用処理・基盤クラス

| やりたいこと | 参照先 |
|---|---|
| 文字列/コレクション/Dictionary/日時/暗号/圧縮等の汎用操作 | [Extensions/Methods.md](Extensions/Methods.md)（書く前に必ず確認） |
| GameObject生成・破棄・コンポーネント取得・SetActive | [Extensions/Core.md](Extensions/Core.md)（`UnityUtility`） |
| Singleton・マネージャークラスを作る | [Extensions/Core.md](Extensions/Core.md)（`Singleton<T>` / `SingletonMonoBehaviour<T>`） |
| プレハブ参照+インスタンス化 | [Extensions/Core.md](Extensions/Core.md)（`Prefab`） |
| インスペクタ属性（ReadOnly/EnumFlags/Label等） | [Extensions/Core.md](Extensions/Core.md) |
| Rx（Observable/Subject）を書く | [Modules/R3Extension.md](Modules/R3Extension.md) — **R3を使う（UniRxは不在）**。冒頭の移行表参照 |
| Observableの購読側処理の完了を発火側で待つ | [Modules/R3Extension.md](Modules/R3Extension.md)（`AsyncHandler`） |
| 1フレーム内の処理量を制限（分割実行） | [Modules/Performance.md](Modules/Performance.md)（`FunctionFrameLimiter`） |

### 画面実装

| やりたいこと | 参照先 |
|---|---|
| 新しいシーン（画面）を追加 | [Modules/Scene.md](Modules/Scene.md)（手順あり）+ [Modules/View.md](Modules/View.md) |
| 新しいウィンドウ/ポップアップを追加 | [Modules/Window.md](Modules/Window.md)（手順あり）+ [Modules/View.md](Modules/View.md) |
| 画面の状態管理（ViewModel）・子Viewとの共有 | [Modules/View.md](Modules/View.md) |
| ボタン・画像・テキスト等のUI部品 | [Modules/UI.md](Modules/UI.md)（`Extension/` の abstract 基底を継承した具象クラスを利用側で実装） |
| 大量アイテムのスクロールリスト | [Modules/UI.md](Modules/UI.md)（`VirtualScroll` — 再発明厳禁） |
| テキスト表示（ローカライズ対応） | [Modules/TextData.md](Modules/TextData.md)（`TextData.Get/Format` 必須） |
| セーフエリア・レターボックス対応 | [Modules/Resolution.md](Modules/Resolution.md) |
| Android戻るキー対応 | [Modules/BackKey.md](Modules/BackKey.md) |

### データ・通信

| やりたいこと | 参照先 |
|---|---|
| マスターデータの参照・新規マスター追加 | [Modules/Master.md](Modules/Master.md)（追加チェックリストあり） |
| セーブデータ等のローカル永続化 | [Modules/LocalData.md](Modules/LocalData.md)（追加手順あり） |
| 軽量なキー値の永続化 | [Modules/Prefs.md](Modules/Prefs.md)（`SecurePrefs`） |
| メモリキャッシュ / ストレージの使い分け | [Modules/Cache.md](Modules/Cache.md)（冒頭に比較表） |
| サーバーAPI呼び出し・CloudScript追加 | [Modules/PlayFab.md](Modules/PlayFab.md)（手順あり。入口ファサードは利用側で実装） |
| 現在時刻の取得 | `systemModel.LocalTime`（`DateTime.Now`禁止）— 供給元は [Modules/PlayFab.md](Modules/PlayFab.md) |
| 配信アセット（画像・プレハブ等）のロード | [Modules/ExternalAsset.md](Modules/ExternalAsset.md) |
| ファイルダウンロード・オフライン検知 | [Modules/Network.md](Modules/Network.md) |
| MessagePackシリアライズ対応クラスの定義 | [Modules/MessagePack.md](Modules/MessagePack.md) |
| 課金処理 | [Modules/InAppPurchasing.md](Modules/InAppPurchasing.md) |
| ローカルプッシュ通知 | [Modules/Notifications.md](Modules/Notifications.md) |

### 演出・サウンド

| やりたいこと | 参照先 |
|---|---|
| BGM/SE再生 | [Modules/Sound.md](Modules/Sound.md)（利用側でロード付き入口ラッパーを実装するのが定石） |
| Animatorのステート再生を await で待つ | [Modules/Animation.md](Modules/Animation.md)（`AnimationPlayer`） |
| Tween（DOTween）を await・速度制御付きで | [Modules/DoTween.md](Modules/DoTween.md) |
| パーティクル再生を await で待つ | [Modules/Particle.md](Modules/Particle.md) |
| エフェクト・リストアイテムの使い回し | [Modules/ObjectPool.md](Modules/ObjectPool.md) |

### 制御・その他

| やりたいこと | 参照先 |
|---|---|
| 通信中・遷移中のタップブロック | [Modules/InputControl.md](Modules/InputControl.md)（`using (new BlockInput())` スコープ） |
| アプリのポーズ/レジューム/終了検知 | [Modules/ApplicationEvent.md](Modules/ApplicationEvent.md)（`OnApplicationPause`直書き不要） |
| 強制アップデート（ストアページ誘導） | [Modules/StorePage.md](Modules/StorePage.md) |
| クラッシュ・エラーレポート | [Modules/Bugsnag.md](Modules/Bugsnag.md) |
| 開発用ログ出力 | [Modules/Devkit.md](Modules/Devkit.md)（`UnityConsole`。本番に残すエラーは `Debug.LogError`） |
| エディタ拡張（EditorWindow/Inspector）を書く | [Extensions/Devkit.md](Extensions/Devkit.md) + [Modules/Devkit.md](Modules/Devkit.md) |
| S3への配信データアップロード（エディタ） | [Modules/AmazonWebService.md](Modules/AmazonWebService.md) |

## モジュール一覧


### Extensions（拡張メソッド・基盤クラス群）

| ドキュメント | 内容 |
|---|---|
| [Extensions/Methods.md](Extensions/Methods.md) | 汎用拡張メソッド群（string/コレクション/UI/暗号等） |
| [Extensions/Core.md](Extensions/Core.md) | Singleton・LifetimeDisposable・Prefab・インスペクタ属性・Serialize型 |
| [Extensions/Devkit.md](Extensions/Devkit.md) | エディタ拡張の共通GUI部品・アセット操作（EditorWindow/Inspector実装基盤） |

### Modules（使用中）

| モジュール | 説明 |
|---|---|
| [TextData](Modules/TextData.md) | ローカライズ対応テキスト管理（Excel原本・enum/文字列キー取得） |
| [View](Modules/View.md) | View-ViewModel接続基盤（画面状態の共有・自動解決） |
| [Master](Modules/Master.md) | マスターデータの配信・暗号化キャッシュ・MessagePackロード・参照基盤 |
| [Devkit](Modules/Devkit.md) | エディタ開発支援ツール群+実機デバッグ（UnityConsole/SRDebugger/レポート送信） |
| [UI](Modules/UI.md) | uGUIラッパー・仮想スクロール・SpriteLoader等（Extension/ の基底は abstract。利用側で具象を実装） |
| [ObjectPool](Modules/ObjectPool.md) | GameObjectを使い回す汎用プール（リストアイテム・エフェクト向け） |
| [LocalData](Modules/LocalData.md) | 端末ローカル永続データの型ベースLoad/Save基盤（MessagePack+AES） |
| [Cache](Modules/Cache.md) | 文字列キーのメモリキャッシュ（冒頭にストレージ使い分け比較表） |
| [ExternalAsset](Modules/ExternalAsset.md) | 配信アセットのDL・キャッシュ・ロード基盤（AssetBundle/生ファイル） |
| [Sound](Modules/Sound.md) | BGM/SE再生管理（音量・同時再生制限・フェード） |
| [PlayFab](Modules/PlayFab.md) | PlayFab CSharpSDK補助（生HTTP・エラー判定）。API入口ファサードは利用側で実装 |
| [InputControl](Modules/InputControl.md) | 全画面タップ無効化基盤（`using(new BlockInput())` スコープ・多重管理） |
| [R3Extension](Modules/R3Extension.md) | R3⇔UniTask橋渡し（AsyncHandler等）。冒頭にR3移行状況あり |
| [Scene](Modules/Scene.md) | シーン遷移基盤（Initialize→Prepare→Enter→Leave、加算遷移・キャッシュ・履歴） |
| [ApplicationEvent](Modules/ApplicationEvent.md) | ポーズ/レジューム/終了/低メモリのstatic Observable配信 |
| [Animation](Modules/Animation.md) | Animatorステート再生をawaitで終了待ちできるAnimationPlayer基盤 |
| [Localize](Modules/Localize.md) | エディタ言語選択と言語別スプライト切替（実行時の言語切替は利用側で実装） |
| [InAppPurchasing](Modules/InAppPurchasing.md) | Unity IAPラッパー。サーバー検証前提のPending型課金基盤 |
| [Network](Modules/Network.md) | HTTP通信基盤（ファイルDL・到達性監視）。API通信はPlayFab経由が原則 |
| [Bugsnag](Modules/Bugsnag.md) | クラッシュ・エラーレポート送信基盤（エディタでは無効・実機のみ） |
| [Window](Modules/Window.md) | ポップアップ開閉ライフサイクルと多重表示管理（利用側で `Modules.Window.Window` 派生 + `PopupManager<T>` 派生を実装） |
| [Resolution](Modules/Resolution.md) | セーフエリア・レターボックス等の画面解像度適応UI部品 |
| [PatternTexture](Modules/PatternTexture.md) | 差分絵をブロック重複排除でパックする独自アトラス+描画UI |
| [MessagePack](Modules/MessagePack.md) | MP共通Resolver（コード生成はSource Generatorで自動化済み） |
| [FileCache](Modules/FileCache.md) | AES暗号化・有効期限付きディスクキャッシュ |
| [AmazonWebService](Modules/AmazonWebService.md) | エディタからS3へ配信データをアップロードするAWSラッパー |
| [StorePage](Modules/StorePage.md) | ストアページを開くだけの極小static（強制アップデート用） |
| [Performance](Modules/Performance.md) | 1フレーム内の処理回数を制限するFunctionFrameLimiter |
| [Particle](Modules/Particle.md) | ParticleSystem一括制御プレイヤー（Play()をawaitで終了待ち可能） |
| [Notifications](Modules/Notifications.md) | ローカルプッシュ通知の登録基盤 |
| [DeviceOrientation](Modules/DeviceOrientation.md) | 画面向き(ScreenOrientation)の適用・監視Singleton基底 |
| [Crypto](Modules/Crypto.md) | AES鍵の暗号化ファイル管理・供給（暗号化処理本体はExtensionsのAES拡張） |
| [BackKey](Modules/BackKey.md) | Android戻るキーのPriority順ハンドリング（Window連携） |
| [DoTween](Modules/DoTween.md) | Tweenerをawait可能・速度一括制御で実行するコントローラ |
| [Rendering](Modules/Rendering.md) | URPカメラスタックのpriority順自動構成 |
| [TouchEffect](Modules/TouchEffect.md) | タップ時パーティクル表示の常駐マネージャー |
| [Prefs](Modules/Prefs.md) | PlayerPrefsのAES暗号化ラッパーSecurePrefs（namespaceは `Extensions`） |
| [Renderer2D](Modules/Renderer2D.md) | SpriteRenderer用エディタ専用ダミー画像 |
| [TimeUtil](Modules/TimeUtil.md) | 時間ユーティリティ（`TimeManager<T>` / `TimeNotice` / `RecoveryValue` / `TimeScale` / `RealTime`） |
| [UniTask](Modules/UniTask.md) | UniTaskのPlayerLoop初期化前倒し（自動実行のみ・手動呼び出し不要） |

### Modules（使用可能だが未使用 — 使う前に一言ユーザーに確認を推奨）

| モジュール | 説明 |
|---|---|
| [Hyphenation](Modules/Hyphenation.md) | 日本語禁則処理+幅計測ベース自動改行 |
| [SpriteAnimation](Modules/SpriteAnimation.md) | SpriteAtlas連番コマアニメ再生 |
| [OffScreenRendering](Modules/OffScreenRendering.md) | RenderTexture経由のUI表示とクリック判定 |
| [StateControl](Modules/StateControl.md) | enumキーの非同期ステートマシン |
| [TagText](Modules/TagText.md) | タグ入りテキストの文字送りビルダー（namespaceは誤記のまま `Modules.TagTect`） |
| [Camera](Modules/Camera.md) | Camera.rect調整によるアスペクト比固定（namespace `Modules.FixedAspectCamera`） |
| [Shader](Modules/Shader.md) | シェーダーを名前指定で差し替え（namespace `Modules.Shaders`） |
| [SortingLayerSetter](Modules/SortingLayerSetter.md) | SortingLayer/Orderのインスペクタ設定 |
| [PathFinding](Modules/PathFinding.md) | 2DグリッドA*経路探索 |
| [BehaviourControl](Modules/BehaviourControl.md) | データ駆動ルールテーブル型AI（namespace は `Modules.BehaviorControl`、綴り違いに注意） |

### Modules（休眠 — コンパイル対象外。使う実装を提案しないこと）

| モジュール | 説明 |
|---|---|
| [CriWare](Modules/CriWare.md) | CRIライブラリ初期化・アセットDL |
| [Movie](Modules/Movie.md) | CRI Sofdecムービー再生 |
| [Live2D](Modules/Live2D.md) | Live2DのUIクリック判定 |
| [Lua](Modules/Lua.md) | xLua連携基盤 |
| [Scenario](Modules/Scenario.md) | Luaカットシーン演出基盤（コマンド約46種） |
| [Utage](Modules/Utage.md) | ADVエンジン「宴」統合拡張 |
| [TimeLine](Modules/TimeLine.md) | Unity Timelineラッパー |
| [Vivox](Modules/Vivox.md) | ボイス/テキストチャットSDKラッパー |
| [WebView](Modules/WebView.md) | アプリ内WebView抽象化 |
| [StandAloneWindows](Modules/StandAloneWindows.md) | Windows専用ネイティブウィンドウ制御 |

## 関連

- 基盤全体像・共通パターン・既知の罠一覧: [Overview.md](Overview.md)
- コーディング規約: ルート `CLAUDE.md`
