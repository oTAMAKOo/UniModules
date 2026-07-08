# Extensions Devkit（エディタ拡張ユーティリティ）

> **namespace**: `Extensions.Devkit`（例外: `BackgroundStyle`→`Extensions.Devkit.Style`、`TextureEditorUtility`→`Extensions`、`DebugLog`→`Modules.Devkit.Log`、`Debug`→グローバル）
> **場所**: `Client/Assets/UniModules/Scripts/Extensions/Devkit/`（全27ファイル。`Editor/` 配下はエディタ専用。直下の `Debug.cs` はビルド専用、`Log/DebugLog.cs` はランタイム共用）
> **Client側使用**: 10ファイル（2026-07時点。全て `Client/Assets/Scripts/Editor/` 配下）
> **依存**: R3（SingletonEditorWindow / FastScrollView / SpriteSelectorWindow / DebugLog）/ Extensions（`Scope`・`UnityUtility`・`PathUtility` 等）/ Modules.Devkit.Prefs（`ProjectPrefs`: Header 開閉永続化）/ Modules.Devkit.AssemblyCompilation（`CompileNotification`: SpriteSelectorWindow）

## 概要

エディタ拡張（EditorWindow・カスタム Inspector・MenuItem 配下ツール）を書くための共通 GUI 部品・ユーティリティ層。
見出し（Title/Header）・検索ボックス・ドラッグ&ドロップ・仮想化リスト・using 型の状態スコープ・アセット操作（Undo/検索/保存）を提供する。
UniModules の Devkit ツール群・Client 側 Editor コードは全てこの層の上に書かれている。**素の EditorGUILayout を組み合わせて自作する前に、必ずここから部品を探すこと**。

| 分類 | 主要クラス |
|---|---|
| GUI 描画（static） | `EditorLayoutTools`（GUI 部品集。static partial・6ファイル分割: 本体（Title/Header/Foldout/ボタン/Outline）+ `.draganddrop` + `.field`（ラベル幅を文字幅に自動調整する入力系・検索ボックス）+ `.sprite` + `.texture` + `.utility`（Pro/Light スキン対応の標準色・ラベル幅）） / `EditorGUIContentLayout`（`ContentsScope` の実体。直接呼びより Scope 推奨） / `EditorSplitterGUILayout`（ドラッグリサイズ可能な上下分割） / `EditorGUISelectableHelpBox`（テキスト選択（コピー）可能な HelpBox） / `BackgroundStyle`（単色背景 GUIStyle 供給） / `GUILayoutOptions`（`ColumnHeader` の引数用） |
| Scope（using） | GUI.Scope 基底: `BackgroundColorScope` / `ColorScope` / `ContentColorScope` / `DisableScope`（グレーアウト） / `IndentLevelScope`（**絶対値**指定） / `LabelWidthScope` / `ContentsScope`（box 囲み）。`Extensions.Scope` 基底: `AssetEditingScope`（`StartAssetEditing`/`StopAssetEditing`。OnGUI 外で使用可） |
| Window / Inspector | `SingletonEditorWindow<T>`（シングルトン EditorWindow 基底。`Instance` / `IsExist` / R3 `Disposable` / `OnDestroyAsObservable()` / 初期化フック `OnCreateInstance()`。継承ツールの実例一覧→ [Devkit (Modules)](../Modules/Devkit.md)） / `SpriteSelectorWindow`（汎用 Sprite グリッド選択ダイアログ。検索・単一/複数選択・R3 で結果通知） / `EditorGUIFastScrollView<T>`（可視範囲のみ描画する仮想化スクロールビュー。`Type` と `DrawContent` を実装して使う） / `ScriptlessEditor`（「Script」欄を出さないカスタム Inspector 基底。`DrawDefaultScriptlessInspector()`） |
| アセット操作（static） | `UnityEditorUtility`（Undo 登録・アセット検索/保存/選択・GUID 変換・Prefab 判定・Missing 検出・AssetBundle 名取得） / `TextureEditorUtility`（テクスチャのストレージ/ランタイムメモリサイズ取得。**namespace は `Extensions`**） |
| ランタイム系 | `Debug`（グローバル namespace・**`#if !UNITY_EDITOR` ビルド専用**の `UnityEngine.Debug` 置き換え） / `DebugLog`（ログ文字列の中継ハブ。詳細は [Devkit (Modules)](../Modules/Devkit.md) §DebugLog） |

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| シングルトンの EditorWindow を作りたい | `SingletonEditorWindow<T>` 継承 + `Instance.Show()` |
| ウィンドウの存在確認だけしたい（生成せず） | `SingletonEditorWindow<T>.IsExist` |
| セクション見出し（帯付きラベル）を描きたい | `EditorLayoutTools.Title(text)` |
| 開閉できるヘッダーを描きたい（状態を自動保存） | `EditorLayoutTools.Header(text, key)` |
| ヘッダー配下を box 枠で囲みたい | `using (new ContentsScope())` |
| 折りたたみ（開閉状態を自前フィールド管理） | `EditorLayoutTools.Foldout(text, display)` |
| 表の列ヘッダーを描きたい | `EditorLayoutTools.ColumnHeader(contents)` |
| GUI を条件付きでグレーアウトしたい | `using (new DisableScope(condition))` |
| 背景色/文字色/GUI色を一時的に変えたい | `BackgroundColorScope` / `ContentColorScope` / `ColorScope` |
| ラベル幅を一時的に変えたい | `using (new LabelWidthScope(width))` or `EditorLayoutTools.SetLabelWidth()` |
| インデントを一時的に変えたい | `using (new IndentLevelScope(level))`（絶対値指定） |
| 型指定の ObjectField を1行で書きたい | `EditorLayoutTools.ObjectField<T>(label, obj, allowSceneObjects)` |
| ラベル幅が文字に合う入力フィールド | `EditorLayoutTools.TextField` / `IntField` / `FloatField` / `BoolField`（各 Delayed 版あり） |
| 検索ボックス（×クリアボタン付き）を描きたい | `EditorLayoutTools.DrawSearchTextField` / `DrawToolbarSearchTextField` |
| ドラッグ&ドロップ受付エリアを作りたい | `EditorLayoutTools.DragAndDrop<T>(text)` / `MultipleDragAndDrop<T>(text)` |
| メニュー・ポップアップ項目名に '/' を含めたい | `EditorLayoutTools.ConvertSlashToUnicodeSlash`（'/' ⇔ '∕'(U+2215)。逆変換は `ConvertUnicodeSlashToSlash`） |
| 大量リストを軽く描画したい（仮想化） | `EditorGUIFastScrollView<T>` 継承 |
| 上下分割（ドラッグでリサイズ可）レイアウト | `EditorSplitterGUILayout.CreateSplitterState` + `BeginVerticalSplit` |
| テキストをコピーできる HelpBox を出したい | `EditorGUISelectableHelpBox.Draw(message, messageType)` |
| Sprite 一覧グリッドから選択させたい | `SpriteSelectorWindow.Open(...)` + `OnConfirmAsObservable()` |
| Sprite / Texture をプレビュー描画したい | `EditorLayoutTools.DrawSprite` / `DrawTexture` / `DrawTiledTexture` |
| Inspector から「Script」欄を消したい | `ScriptlessEditor` 継承 + `DrawDefaultScriptlessInspector()` |
| Undo 対応でオブジェクトを編集したい | `UnityEditorUtility.RegisterUndo(target)`（変更前に呼ぶ） |
| 型でアセットを検索したい | `UnityEditorUtility.FindAssetsByType<T>("t:Prefab", folders)` |
| アセットを保存 / Project ビューで選択したい | `UnityEditorUtility.SaveAsset(asset)` / `SelectAsset(asset)` |
| GUID ⇔ アセットを変換したい | `UnityEditorUtility.FindMainAsset(guid)` / `GetAssetGUID(asset)` |
| 大量のアセット生成・削除をまとめて速くしたい | `using (new AssetEditingScope())` |
| Missing 参照を持つ GameObject を検出したい | `UnityEditorUtility.HasMissingReference(gameObject)` |
| スクリプトコンパイルを明示実行したい | `UnityEditorUtility.RequestScriptCompilation()` |
| テクスチャのメモリ使用量を調べたい | `TextureEditorUtility.GetStorageMemorySizeLong(texture)` |
| 矩形に枠線を描きたい | `EditorLayoutTools.Outline(rect, color)` |
| 単色背景の GUIStyle が欲しい | `BackgroundStyle.Get(color)`（`Extensions.Devkit.Style`） |
| ビルド実行時のログを購読したい（Bugsnag 連携等） | `DebugLog.On*ReceivedAsObservable()`（→ [Devkit (Modules)](../Modules/Devkit.md)） |

## 使い方

定型パターンと実在の引用元（コードは引用元を参照）:

- SingletonEditorWindow の基本形（static `Open()` で `Instance` の titleContent / minSize を設定して `ShowUtility()`、OnGUI 内 `DisableScope` で条件付きグレーアウト）: `Client/Assets/Scripts/Editor/TextMeshPro/TmpSpriteAssetGeneratorWindow.cs`
- 初回生成時のタイトル・minSize 設定を `OnCreateInstance()` オーバーライドで行う形: `Client/Assets/Scripts/Editor/Battle/Trace/BattleTraceWindow.cs`
- Header + ContentsScope + RegisterUndo（Inspector の定番。`Header(text, key)` の key で開閉状態を永続化し、値を書き込む前に `UnityEditorUtility.RegisterUndo`）: `Client/Assets/UniModules/Scripts/Modules/CriWare/Editor/CriAssetConfigInspector.cs`
- SpriteSelectorWindow（`Open(title, items, initialSelection, maxSelectCount)`。maxSelectCount: 1 = クリック即確定 / null = 無制限。初期選択は UserData 一致で判定。`OnConfirmAsObservable()` を `.AddTo(Disposable)` で購読）: `Client/Assets/Scripts/Editor/WorldMap/GenerateWindow.cs`
- AssetEditingScope でアセット一括操作を高速化: `Client/Assets/Scripts/Editor/TextData/GenerateAllLanguage.cs`
- EditorSplitterGUILayout（初期化時に `CreateSplitterState(相対サイズ, 最小px, null)` → OnGUI で `BeginVerticalSplit` / `EndVerticalSplit`）: `Client/Assets/UniModules/Scripts/Modules/Devkit/ApiMonitor/Editor/ApiMonitorWindow.cs`
- 仮想化リスト継承: `Client/Assets/UniModules/Scripts/Modules/Devkit/Build/Editor/BuiltInAsset/BuiltInAssetScrollView.cs`（`EditorGUIFastScrollView<BuiltInAssets.BuiltInAssetInfo>` 継承）
- 検索ボックス: `Client/Assets/UniModules/Scripts/Extensions/Devkit/Editor/SpriteSelectorWindow.cs:157`（`DrawToolbarSearchTextField`）
- Title + ContentsScope の入れ子: `Client/Assets/UniModules/Scripts/Modules/BehaviourControl/Editor/BehaviorControlMonitor.cs`
- 選択可能 HelpBox: `Client/Assets/UniModules/Scripts/Modules/TextData/TextSetter/Editor/TextSetterInspector.cs:175`
- SaveAsset: `Client/Assets/Scripts/Editor/Build/Build.cs:186`

## 注意点・罠

- **エディタ専用**: `Editor/` 配下（＝本モジュールのほぼ全部）はエディタアセンブリ。ランタイムコードから参照するとビルドが壊れる。例外は `Debug.cs`（ビルド専用）と `Log/DebugLog.cs`（共用）のみ。
- **namespace がフォルダと不一致のクラスがある**: `TextureEditorUtility` は `Extensions`（`Extensions.Devkit` ではない）、`BackgroundStyle` は `Extensions.Devkit.Style`、`DebugLog` は `Modules.Devkit.Log`。using を書く際に注意。
- **`IndentLevelScope` は絶対値指定**: Unity 標準の `EditorGUI.IndentLevelScope`（増分）と挙動が違う。`new IndentLevelScope(EditorGUI.indentLevel + 1)` のように書く。
- **`Header(text, key)` の開閉状態は ProjectPrefs 永続化**: key はプロジェクト内で一意にする（実例: `"CriAssetConfigInspector-Sound"`）。key 省略版は text がそのまま key になるため、同名見出しが複数箇所にあると開閉状態が連動する。
- **`EditorGUIFastScrollView` は初回フレームを必ずスキップ**して `RequestRepaint` を発行する。`OnRepaintRequestAsObservable().Subscribe(_ => Repaint()).AddTo(Disposable)` を購読しないと、初期表示やレイアウト更新が1操作分遅れる。
- **`SingletonEditorWindow<T>.Instance` はアクセスしただけで生成される**（`CreateInstance<T>()`）。存在チェックだけなら `IsExist` を使う。
- **`SpriteSelectorWindow` は「×」で閉じると OnConfirm / OnCancel とも OnNext なしで完了する**（Cancel ボタン経由のみ OnCancel 発火）。またコンパイル開始時に自動 Close する。
- **Unity 内部 API へのリフレクション依存**: `EditorLayoutTools.DrawSprite`（`UnityEditor.UI.SpriteDrawUtility`）、`EditorSplitterGUILayout`（`UnityEditor.SplitterState` / `SplitterGUILayout`）、`TextureEditorUtility`（`UnityEditor.TextureUtil`）、`UnityEditorUtility.GetLocalIdentifierInFile`（`inspectorMode`）。Unity バージョンアップで壊れる可能性がある箇所。
- **`BackgroundStyle.Get` は共有インスタンスを返す**: 呼ぶたびに同じ static GUIStyle / Texture2D を上書きするため、戻り値のキャッシュや同一フレームでの複数色併用は不可。描画直前に都度 `Get` する。
- **`AssetEditingScope` は `GUI.Scope` ではなく `Extensions.Scope`**（ファイナライザ付き IDisposable）。OnGUI 外・バッチ処理で使える。`StartAssetEditing` の対応漏れは AssetDatabase を壊すので必ず using で使う。
- **`UnityEditorUtility.GetAllAssetPathInFolder(folderPath)` の戻り値は UniTask ではなく `Task<string[]>`**（フォルダ内全 AssetPath・.meta 除外）。
- **`GetTextFieldHight` はタイポのまま**（Height ではない）。grep・呼び出し時は実名に合わせる。
- **`Debug.cs` はビルド専用**（`#if !UNITY_EDITOR`・グローバル namespace で `UnityEngine.Debug` を置換）。非デバッグビルドではログ出力を抑制する（`ENABLE_DEVKIT` 定義時は常時出力）が、`DebugLog` への中継は抑制中も常に行われる。

## 関連

- [Core](Core.md) — `Scope` 基底 / `Singleton<T>` / `UnityUtility` / `LifetimeDisposable` / `UnityPathUtility`・`PathUtility`
- [Methods](Methods.md) — 本モジュール内部でも使用する汎用拡張メソッド（`FixLineEnd` / `IsMatch` / `IsEmpty` 等）
- [Devkit (Modules)](../Modules/Devkit.md) — `Modules.Devkit.*`（ProjectPrefs / UnityConsole / CompileNotification / DebugLog 詳説）。`SingletonEditorWindow<T>` 継承ツールの実例多数
- [R3Extension](../Modules/R3Extension.md) — Observable 購読パターン（`.AddTo(Disposable)` 等）
