# UniModules 基盤リファレンス INDEX

UniModules（プロジェクト非依存のゲーム基盤）のリファレンスドキュメント群。

**このドキュメントの目的**: Claude が実装時に基盤の既存機能を漏れなく活用し、車輪の再発明をしないこと。

## 使い方（Claude向け）

1. 利用側の実装前に、まずこの INDEX の「やりたいこと逆引き」を確認する
2. 該当する機能の説明（各機能フォルダ直下の `Modules/<機能名>/Manual.md`）**だけ** を開いて参照する（全ドキュメントを読み込まない）
3. 汎用処理（文字列・コレクション・GameObject操作等）を書く前に [Extensions/Methods.md](Extensions/Methods.md) を確認（車輪の再発明防止の最重要ファイル）
4. 基盤全体の構造・共通パターン・既知の罠は [Overview.md](Overview.md) を参照
5. **「条件付きコンパイル」のモジュール**（外部SDK依存）は、SDK導入+シンボル定義がある環境でのみ使用可。利用プロジェクトでの有効状態はプロジェクト側のドキュメントで確認してから提案する

## やりたいこと逆引き

### 汎用処理・基盤クラス

| やりたいこと | 参照先 |
|---|---|
| 文字列/コレクション/Dictionary/日時/暗号/圧縮等の汎用操作 | [Extensions/Methods.md](Extensions/Methods.md)（書く前に必ず確認） |
| GameObject生成・破棄・コンポーネント取得・SetActive | [Extensions/Core.md](Extensions/Core.md)（`UnityUtility`） |
| Singleton・マネージャークラスを作る | [Extensions/Core.md](Extensions/Core.md)（`Singleton<T>` / `SingletonMonoBehaviour<T>`） |
| プレハブ参照+インスタンス化 | [Extensions/Core.md](Extensions/Core.md)（`Prefab`） |
| インスペクタ属性（ReadOnly/EnumFlags/Label等） | [Extensions/Core.md](Extensions/Core.md) |
| Rx（Observable/Subject）を書く | [R3Extension](../Modules/R3Extension/Manual.md) — **R3を使う（UniRxは不在）**。冒頭の移行表参照 |
| Observableの購読側処理の完了を発火側で待つ | [R3Extension](../Modules/R3Extension/Manual.md)（`AsyncHandler`） |
| 1フレーム内の処理量を制限（分割実行） | [Performance](../Modules/Performance/Manual.md)（`FunctionFrameLimiter`） |
| 一定間隔の処理を順番・例外の隔離つきで 1 か所から呼ぶ（定期通知） | [TimeUtil](../Modules/TimeUtil/Manual.md)（`PeriodicTickManager<T>`） |

### 画面実装

| やりたいこと | 参照先 |
|---|---|
| 新しいシーン（画面）を追加 | [Scene](../Modules/Scene/Manual.md)（手順あり）+ [View](../Modules/View/Manual.md) |
| 新しいウィンドウ/ポップアップを追加 | [Window](../Modules/Window/Manual.md)（手順あり）+ [View](../Modules/View/Manual.md) |
| 画面の状態管理（ViewModel）・子Viewとの共有 | [View](../Modules/View/Manual.md) |
| ボタン・画像・テキスト等のUI部品 | [UI](../Modules/UI/Manual.md)（`Extension/` の abstract 基底を継承した具象クラスを利用側で実装） |
| 大量アイテムのスクロールリスト | [UI](../Modules/UI/Manual.md)（`VirtualScroll` — 再発明厳禁） |
| テキスト表示（ローカライズ対応） | [TextData](../Modules/TextData/Manual.md)（`TextData.Get/Format` 必須） |
| セーフエリア・レターボックス対応 | [Resolution](../Modules/Resolution/Manual.md) |
| Android戻るキー対応 | [BackKey](../Modules/BackKey/Manual.md) |

### データ・通信

| やりたいこと | 参照先 |
|---|---|
| マスターデータの参照・新規マスター追加 | [Master](../Modules/Master/Manual.md)（追加チェックリストあり） |
| セーブデータ等のローカル永続化 | [LocalData](../Modules/LocalData/Manual.md)（追加手順あり） |
| 軽量なキー値の永続化 | [Prefs](../Modules/Prefs/Manual.md)（`SecurePrefs`） |
| 秘密の値を OS の安全な保存領域（Keychain / Keystore）に保存 | [SecureStorage](../Modules/SecureStorage/Manual.md)（`SecureStorage`。条件付きコンパイル） |
| 通信データに端末の鍵（Secure Enclave / Keystore）で署名 | [DeviceKey](../Modules/DeviceKey/Manual.md)（`DeviceKey`。条件付きコンパイル） |
| メモリキャッシュ / ストレージの使い分け | [Cache](../Modules/Cache/Manual.md)（冒頭に比較表） |
| サーバーAPI呼び出し・CloudScript追加 | [PlayFab](../Modules/PlayFab/Manual.md)（手順あり。入口ファサードは利用側で実装） |
| 配信アセット（画像・プレハブ等）のロード | [ExternalAssets](../Modules/ExternalAssets/Manual.md) |
| ファイルダウンロード・オフライン検知 | [Network](../Modules/Network/Manual.md) |
| MessagePackシリアライズ対応クラスの定義 | [MessagePack](../Modules/MessagePack/Manual.md) |
| 課金処理 | [InAppPurchasing](../Modules/InAppPurchasing/Manual.md) |
| ローカルプッシュ通知 | [Notifications](../Modules/Notifications/Manual.md) |

### 演出・サウンド

| やりたいこと | 参照先 |
|---|---|
| BGM/SE再生 | [Sound](../Modules/Sound/Manual.md)（利用側でロード付き入口ラッパーを実装するのが定石） |
| Animatorのステート再生を await で待つ | [Animation](../Modules/Animation/Manual.md)（`AnimationPlayer`） |
| Tween（DOTween）を await・速度制御付きで | [DoTweenExtension](../Modules/DoTweenExtension/Manual.md) |
| パーティクル再生を await で待つ | [Particle](../Modules/Particle/Manual.md) |
| エフェクト・リストアイテムの使い回し | [ObjectPool](../Modules/ObjectPool/Manual.md) |

### 制御・その他

| やりたいこと | 参照先 |
|---|---|
| 通信中・遷移中のタップブロック | [InputControl](../Modules/InputControl/Manual.md)（`using (new BlockInput())` スコープ） |
| アプリのポーズ/レジューム/終了検知 | [ApplicationEvent](../Modules/ApplicationEvent/Manual.md)（`OnApplicationPause`直書き不要） |
| 強制アップデート（ストアページ誘導） | [StorePage](../Modules/StorePage/Manual.md) |
| クラッシュ・エラーレポート | [Bugsnag](../Modules/Bugsnag/Manual.md) |
| 開発用ログ出力 | [Devkit](../Modules/Devkit/Manual.md)（`UnityConsole`。本番に残すエラーは `Debug.LogError`） |
| エディタ拡張（EditorWindow/Inspector）を書く | [Extensions/Devkit.md](Extensions/Devkit.md) + [Devkit](../Modules/Devkit/Manual.md) |
| Prefab をオフスクリーン描画して画像で確認・展開して検査したい | [Automation](../Modules/Automation/Manual.md)（`PrefabPreviewRenderer` / `PrefabPreviewScope`） |
| Markdown を Unity 上で整形表示する（機能の説明 `Manual.md` の表示・書き方） | [Markdown](../Modules/Devkit/Markdown/Manual.md) |
| S3への配信データアップロード（エディタ） | [AmazonWebService](../Modules/AmazonWebService/Manual.md) |

## モジュール一覧


### Extensions（拡張メソッド・基盤クラス群）

| ドキュメント | 内容 |
|---|---|
| [Extensions/Methods.md](Extensions/Methods.md) | 汎用拡張メソッド群（string/コレクション/UI/暗号等） |
| [Extensions/Core.md](Extensions/Core.md) | Singleton・LifetimeDisposable・Prefab・インスペクタ属性・Serialize型 |
| [Extensions/Devkit.md](Extensions/Devkit.md) | エディタ拡張の共通GUI部品・アセット操作（EditorWindow/Inspector実装基盤） |

### Modules

各機能の説明は、機能フォルダ直下の `Manual.md` に置く。Unity 上で選択すると整形して表示される（[Markdown](../Modules/Devkit/Markdown/Manual.md)）。

| モジュール | 説明 |
|---|---|
| [TextData](../Modules/TextData/Manual.md) | ローカライズ対応テキスト管理（Excel原本・enum/文字列キー取得） |
| [View](../Modules/View/Manual.md) | View-ViewModel接続基盤（画面状態の共有・自動解決） |
| [Master](../Modules/Master/Manual.md) | マスターデータの配信・暗号化キャッシュ・MessagePackロード・参照基盤 |
| [Devkit](../Modules/Devkit/Manual.md) | エディタ開発支援ツール群+実機デバッグ（UnityConsole/SRDebugger/レポート送信） |
| [Automation](../Modules/Automation/Manual.md) | プログラムから呼ぶエディタ操作の自動化（Prefab のオフスクリーン描画・検査用スコープ） |
| [UI](../Modules/UI/Manual.md) | uGUIラッパー・仮想スクロール・SpriteLoader等（Extension/ の基底は abstract。利用側で具象を実装） |
| [ObjectPool](../Modules/ObjectPool/Manual.md) | GameObjectを使い回す汎用プール（リストアイテム・エフェクト向け） |
| [LocalData](../Modules/LocalData/Manual.md) | 端末ローカル永続データの型ベースLoad/Save基盤（MessagePack+AES） |
| [Cache](../Modules/Cache/Manual.md) | 文字列キーのメモリキャッシュ（冒頭にストレージ使い分け比較表） |
| [ExternalAssets](../Modules/ExternalAssets/Manual.md) | 配信アセットのDL・キャッシュ・ロード基盤（AssetBundle/生ファイル） |
| [Sound](../Modules/Sound/Manual.md) | BGM/SE再生管理（音量・同時再生制限・フェード） |
| [PlayFab](../Modules/PlayFab/Manual.md) | PlayFab CSharpSDK補助（生HTTP・エラー判定）。API入口ファサードは利用側で実装 |
| [InputControl](../Modules/InputControl/Manual.md) | 全画面タップ無効化基盤（`using(new BlockInput())` スコープ・多重管理） |
| [R3Extension](../Modules/R3Extension/Manual.md) | R3⇔UniTask橋渡し（AsyncHandler等）。冒頭にR3移行状況あり |
| [Scene](../Modules/Scene/Manual.md) | シーン遷移基盤（Initialize→Prepare→Enter→Leave、加算遷移・キャッシュ・履歴） |
| [ApplicationEvent](../Modules/ApplicationEvent/Manual.md) | ポーズ/レジューム/終了/低メモリのstatic Observable配信 |
| [Animation](../Modules/Animation/Manual.md) | Animatorステート再生をawaitで終了待ちできるAnimationPlayer基盤 |
| [Localize](../Modules/Localize/Manual.md) | エディタ言語選択と言語別スプライト切替（実行時の言語切替は利用側で実装） |
| [InAppPurchasing](../Modules/InAppPurchasing/Manual.md) | Unity IAPラッパー。サーバー検証前提のPending型課金基盤 |
| [Network](../Modules/Network/Manual.md) | HTTP通信基盤（ファイルDL・到達性監視）。API通信はPlayFab経由が原則 |
| [Bugsnag](../Modules/Bugsnag/Manual.md) | クラッシュ・エラーレポート送信基盤（エディタでは無効・実機のみ） |
| [Window](../Modules/Window/Manual.md) | ポップアップ開閉ライフサイクルと多重表示管理（利用側で `Modules.Window.Window` 派生 + `PopupManager<T>` 派生を実装） |
| [Resolution](../Modules/Resolution/Manual.md) | セーフエリア・レターボックス等の画面解像度適応UI部品 |
| [PatternTexture](../Modules/PatternTexture/Manual.md) | 差分絵をブロック重複排除でパックする独自アトラス+描画UI（エディタ専用の仮表示 `DummyPatternImage` を含む） |
| [MessagePack](../Modules/MessagePack/Manual.md) | MP共通Resolver（コード生成はSource Generatorで自動化済み） |
| [FileCache](../Modules/FileCache/Manual.md) | AES暗号化・有効期限付きディスクキャッシュ |
| [AmazonWebService](../Modules/AmazonWebService/Manual.md) | エディタからS3へ配信データをアップロードするAWSラッパー |
| [StorePage](../Modules/StorePage/Manual.md) | ストアページを開くだけの極小static（強制アップデート用） |
| [Performance](../Modules/Performance/Manual.md) | 1フレーム内の処理回数を制限するFunctionFrameLimiter |
| [Particle](../Modules/Particle/Manual.md) | ParticleSystem一括制御プレイヤー（Play()をawaitで終了待ち可能） |
| [Notifications](../Modules/Notifications/Manual.md) | ローカルプッシュ通知の登録基盤 |
| [DeviceOrientation](../Modules/DeviceOrientation/Manual.md) | 画面向き(ScreenOrientation)の適用・監視Singleton基底 |
| [Crypto](../Modules/Crypto/Manual.md) | AES鍵の暗号化ファイル管理・供給（暗号化処理本体はExtensionsのAES拡張） |
| [BackKey](../Modules/BackKey/Manual.md) | Android戻るキーのPriority順ハンドリング（Window連携） |
| [DoTweenExtension](../Modules/DoTweenExtension/Manual.md) | Tweenerをawait可能・速度一括制御で実行するコントローラ |
| [Rendering](../Modules/Rendering/Manual.md) | URPカメラスタックのpriority順自動構成 |
| [TouchEffect](../Modules/TouchEffect/Manual.md) | タップ時パーティクル表示の常駐マネージャー |
| [Prefs](../Modules/Prefs/Manual.md) | PlayerPrefsのAES暗号化ラッパーSecurePrefs（namespaceは `Extensions`） |
| [Renderer2D](../Modules/Renderer2D/Manual.md) | SpriteRenderer用エディタ専用ダミー画像 |
| [TimeUtil](../Modules/TimeUtil/Manual.md) | 時間ユーティリティ（`TimeManager<T>` / `TimeNotice` / `RecoveryValue` / `TimeScale` / `RealTime` / `PeriodicTickManager<T>`） |
| [UniTaskExtension](../Modules/UniTaskExtension/Manual.md) | UniTaskのPlayerLoop初期化前倒し（自動実行のみ・手動呼び出し不要） |
| [Hyphenation](../Modules/Hyphenation/Manual.md) | 日本語禁則処理+幅計測ベース自動改行 |
| [SpriteAnimation](../Modules/SpriteAnimation/Manual.md) | SpriteAtlas連番コマアニメ再生 |
| [OffScreenRendering](../Modules/OffScreenRendering/Manual.md) | RenderTexture経由のUI表示とクリック判定 |
| [StateControl](../Modules/StateControl/Manual.md) | enumキーの非同期ステートマシン |
| [TagText](../Modules/TagText/Manual.md) | タグ入りテキストの文字送りビルダー |
| [Camera](../Modules/Camera/Manual.md) | Camera.rect調整によるアスペクト比固定（namespace `Modules.FixedAspectCamera`） |
| [Shaders](../Modules/Shaders/Manual.md) | シェーダーを名前指定で差し替え（namespace `Modules.Shaders`） |
| [SortingLayerSetter](../Modules/SortingLayerSetter/Manual.md) | SortingLayer/Orderのインスペクタ設定 |
| [PathFinding](../Modules/PathFinding/Manual.md) | 2DグリッドA*経路探索 |
| [BehaviorControl](../Modules/BehaviorControl/Manual.md) | データ駆動ルールテーブル型AI |

### Modules（条件付きコンパイル）

外部SDK・外部機能連携のモジュール群。コードが `#if` シンボルで囲まれており、SDK導入 + シンボル定義がない環境ではコンパイル対象外になる。利用プロジェクトで有効かどうかはプロジェクト側のドキュメント（シンボル定義状態）を参照。

| モジュール | 有効化シンボル | 説明 |
|---|---|---|
| [CriWare](../Modules/CriWare/Manual.md) | `ENABLE_CRIWARE_ADX` / `ENABLE_CRIWARE_ADX_LE` / `ENABLE_CRIWARE_SOFDEC` | CRIライブラリ初期化・アセットDL |
| [Movie](../Modules/Movie/Manual.md) | `ENABLE_CRIWARE_SOFDEC` | CRI Sofdecムービー再生 |
| [Live2D](../Modules/Live2D/Manual.md) | `ENABLE_LIVE2D` | Live2DのUIクリック判定 |
| [Lua](../Modules/Lua/Manual.md) | `ENABLE_XLUA` | xLua連携基盤 |
| [Scenario](../Modules/Scenario/Manual.md) | `ENABLE_XLUA` | Luaカットシーン演出基盤（コマンド約46種） |
| [UtageExtension](../Modules/UtageExtension/Manual.md) | `ENABLE_UTAGE` | ADVエンジン「宴」統合拡張 |
| [TimeLine](../Modules/TimeLine/Manual.md) | `ENABLE_UNITY_TIMELINE` | Unity Timelineラッパー（データ定義等の一部は常時コンパイル） |
| [Vivox](../Modules/Vivox/Manual.md) | `ENABLE_VIVOX` | ボイス/テキストチャットSDKラッパー |
| [WebView](../Modules/WebView/Manual.md) | `ENABLE_UNIWEBVIEW` / `ENABLE_EMBEDDEDBROWSER` | アプリ内WebView抽象化（基底2クラスは常時コンパイル・実装Contentのみシンボル依存） |
| [StandAloneWindows](../Modules/StandAloneWindows/Manual.md) | `UNITY_STANDALONE_WIN`（Unity組込） | Windows専用ネイティブウィンドウ制御 |
| [SecureStorage](../Modules/SecureStorage/Manual.md) | `ENABLE_SECURE_STORAGE`（ネイティブの .mm / .java もこのシンボルでビルドに含める） | iOS Keychain / Android Keystore へのキー値保存 |
| [DeviceKey](../Modules/DeviceKey/Manual.md) | `ENABLE_DEVICE_KEY`（ネイティブの .mm / .java もこのシンボルでビルドに含める） | Secure Enclave / Keystore の EC 鍵による ECDSA 署名 |

## 関連

- 基盤全体像・共通パターン・既知の罠一覧: [Overview.md](Overview.md)
