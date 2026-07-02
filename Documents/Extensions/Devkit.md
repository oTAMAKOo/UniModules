# Extensions Devkit（エディタ拡張ユーティリティ）

> **namespace**: `Extensions.Devkit`（例外: `BackgroundStyle`→`Extensions.Devkit.Style`、`TextureEditorUtility`→`Extensions`、`DebugLog`→`Modules.Devkit.Log`、`Debug`→グローバル）
> **場所**: `Client/Assets/UniModules/Scripts/Extensions/Devkit/`（全27ファイル。`Editor/` 配下はエディタ専用。直下の `Debug.cs` はビルド専用、`Log/DebugLog.cs` はランタイム共用）
> **Client側使用**: 10ファイル（2026-07時点。全て `Client/Assets/Scripts/Editor/` 配下）
> **依存**: R3（SingletonEditorWindow / FastScrollView / SpriteSelectorWindow / DebugLog）/ Extensions（`Scope`・`UnityUtility`・`PathUtility` 等）/ Modules.Devkit.Prefs（`ProjectPrefs`: Header 開閉永続化）/ Modules.Devkit.AssemblyCompilation（`CompileNotification`: SpriteSelectorWindow）

## 概要

エディタ拡張（EditorWindow・カスタム Inspector・MenuItem 配下ツール）を書くための共通 GUI 部品・ユーティリティ層。
見出し（Title/Header）・検索ボックス・ドラッグ&ドロップ・仮想化リスト・using 型の状態スコープ・アセット操作（Undo/検索/保存）を提供する。
UniModules の Devkit ツール群・Client 側 Editor コードは全てこの層の上に書かれている。**素の EditorGUILayout を組み合わせて自作する前に、必ずここから部品を探すこと**。

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

## 主要クラス

### GUI 描画（static・エディタ専用）

| クラス | 種別 | 役割 |
|---|---|---|
| `EditorLayoutTools` | static partial | GUI 部品集。6ファイル分割: 本体（Title/Header/Foldout/ボタン/Outline）+ `.draganddrop` + `.field` + `.sprite` + `.texture` + `.utility`（色定義/ラベル幅） |
| `EditorGUIContentLayout` | static | box 囲みレイアウトの `BeginContents()`/`EndContents()`（`ContentsScope` の実体。直接呼びより Scope 推奨） |
| `EditorSplitterGUILayout` | static | ドラッグリサイズ可能な上下分割。Unity 内部 API（`SplitterGUILayout`）をリフレクション呼び出し |
| `EditorGUISelectableHelpBox` | static | テキスト選択（コピー）可能な HelpBox |
| `BackgroundStyle` | static（`Extensions.Devkit.Style`） | 単色背景 GUIStyle 供給（共有インスタンス・罠あり） |
| `GUILayoutOptions` | sealed class | `GUILayoutOption[]` の入れ物。`ColumnHeader` の引数用 |

### Scope（using パターン・エディタ専用）

| Scope | コンストラクタ | 制御対象（using 終了で復元） | 基底 |
|---|---|---|---|
| `BackgroundColorScope` | `(Color color)` | `GUI.backgroundColor` | GUI.Scope |
| `ColorScope` | `(Color color)` | `GUI.color` | GUI.Scope |
| `ContentColorScope` | `(Color color)` | `GUI.contentColor` | GUI.Scope |
| `DisableScope` | `(bool disabled)` | `EditorGUI.BeginDisabledGroup`（グレーアウト） | GUI.Scope |
| `IndentLevelScope` | `(int indentLevel)` | `EditorGUI.indentLevel`（**絶対値**。Unity 標準の増分指定と異なる） | GUI.Scope |
| `LabelWidthScope` | `(float labelWidth)` | `EditorGUIUtility.labelWidth` | GUI.Scope |
| `ContentsScope` | `()` | box 囲み縦レイアウト開始/終了 | GUI.Scope |
| `AssetEditingScope` | `()` | `AssetDatabase.StartAssetEditing()`/`StopAssetEditing()` | `Extensions.Scope`（OnGUI 外で使用可） |

### Window / Inspector 基盤（エディタ専用）

| クラス | 種別 | 役割 |
|---|---|---|
| `SingletonEditorWindow<T>` | EditorWindow 派生（abstract 相当） | シングルトン EditorWindow 基底。`Instance` / `IsExist` / R3 `Disposable` / `OnDestroyAsObservable` を提供。UniModules 内の多数のツールが継承（実例一覧→ [Devkit (Modules)](../Modules/Devkit.md)） |
| `SpriteSelectorWindow` | EditorWindow | 汎用 Sprite グリッド選択ダイアログ。検索・単一/複数選択・R3 で結果通知 |
| `EditorGUIFastScrollView<T>` | abstract class | 可視範囲のみ描画する仮想化スクロールビュー。`Type` と `DrawContent` を実装して使う |
| `ScriptlessEditor` | Editor 派生（abstract） | 「Script」欄を出さないカスタム Inspector 基底 |

### アセット操作（static・エディタ専用）

| クラス | 種別 | 役割 |
|---|---|---|
| `UnityEditorUtility` | static | Undo 登録・アセット検索/保存/選択・GUID 変換・Prefab 判定・Missing 検出・AssetBundle 名取得 |
| `TextureEditorUtility` | static（**namespace は `Extensions`**） | テクスチャのストレージ/ランタイムメモリサイズ取得（`UnityEditor.TextureUtil` リフレクション） |

### ランタイム系（エディタ専用ではない）

| クラス | 種別 | 役割 |
|---|---|---|
| `Debug` | static（グローバル namespace・**`#if !UNITY_EDITOR` ビルド専用**） | `UnityEngine.Debug` の置き換え。非デバッグビルドでログ出力を抑制しつつ `DebugLog` へ常時中継 |
| `DebugLog` | static（`Modules.Devkit.Log`） | ログ文字列の中継ハブ（`Receive*` → `On*ReceivedAsObservable`）。詳細は [Devkit (Modules)](../Modules/Devkit.md) §DebugLog |

## 使い方(実例)

### 1. SingletonEditorWindow（ツールウィンドウの基本形）

```csharp
// Client/Assets/Scripts/Editor/TextMeshPro/TmpSpriteAssetGeneratorWindow.cs
public sealed class TmpSpriteAssetGeneratorWindow : SingletonEditorWindow<TmpSpriteAssetGeneratorWindow>
{
    public static void Open()
    {
        Instance.minSize = WindowSize;

        Instance.titleContent = new GUIContent("TMP Sprite Asset Generator");

        Instance.ShowUtility();
    }

    void OnGUI()
    {
        // 条件を満たすまでボタンをグレーアウト.
        using (new DisableScope(atlas == null))
        {
            if (GUILayout.Button("Generate", EditorStyles.miniButton, GUILayout.Width(80f)))
            {
                Generate();
            }
        }
    }
}
```

```csharp
// Client/Assets/Scripts/Editor/Battle/Trace/BattleTraceWindow.cs
// 初回生成時のタイトル・サイズ設定は OnCreateInstance をオーバーライドする.
protected override void OnCreateInstance()
{
    titleContent = new GUIContent("Battle Trace");

    minSize = new Vector2(800f, 400f);
}
```

### 2. Header + ContentsScope + RegisterUndo（Inspector の定番パターン）

```csharp
// Client/Assets/UniModules/Scripts/Modules/CriWare/Editor/CriAssetConfigInspector.cs（抜粋）
if (EditorLayoutTools.Header("Sound", "CriAssetConfigInspector-Sound"))   // key で開閉状態を永続化.
{
    using (new ContentsScope())
    {
        EditorGUI.BeginChangeCheck();

        var folderName = EditorGUILayout.DelayedTextField(instance.SoundFolderName);

        if (EditorGUI.EndChangeCheck())
        {
            UnityEditorUtility.RegisterUndo(instance);   // 変更を書き込む前に Undo 登録.

            Reflection.SetPrivateField(instance, "soundFolderName", folderName);
        }
    }
}
```

### 3. SpriteSelectorWindow（Sprite 選択ダイアログ + R3 購読）

```csharp
// Client/Assets/Scripts/Editor/WorldMap/GenerateWindow.cs（抜粋）
var window = SpriteSelectorWindow.Open(
    "Select Base Terrain",
    items,                          // SpriteSelectorWindow.Item[] (Sprite / Label / UserData).
    new object[] { terrainId },     // 初期選択 (UserData 一致で判定).
    maxSelectCount: 1);             // 1 = クリック即確定, null = 無制限.

window.OnConfirmAsObservable()
    .Subscribe(result =>
        {
            if (result.Length == 0){ return; }

            terrainId = (uint)result[0];

            Repaint();
        })
    .AddTo(Disposable);             // SingletonEditorWindow の Disposable で寿命管理.
```

### 4. AssetEditingScope（アセット一括操作の高速化）

```csharp
// Client/Assets/Scripts/Editor/TextData/GenerateAllLanguage.cs
using (new AssetEditingScope())
{
    GenerateTextDataAsset(TextType.Internal);
    GenerateTextDataAsset(TextType.External);
}
```

### 5. EditorSplitterGUILayout（リサイズ可能な上下分割）

```csharp
// Client/Assets/UniModules/Scripts/Modules/Devkit/ApiMonitor/Editor/ApiMonitorWindow.cs（抜粋）
// 初期化時: 相対サイズ 75:25, 各ペイン最小 32px.
splitterState = EditorSplitterGUILayout.CreateSplitterState(new float[] { 75f, 25f }, new int[] { 32, 32 }, null);

void OnGUI()
{
    EditorSplitterGUILayout.BeginVerticalSplit(splitterState);
    {
        DrawApiHistoryGUI();

        DrawApiDetailGUI();
    }
    EditorSplitterGUILayout.EndVerticalSplit();
}
```

その他の実例（grep で参照可能）:

- 仮想化リスト継承: `Client/Assets/UniModules/Scripts/Modules/Devkit/Build/Editor/BuiltInAsset/BuiltInAssetScrollView.cs`（`EditorGUIFastScrollView<BuiltInAssets.BuiltInAssetInfo>` 継承）
- 検索ボックス: `Client/Assets/UniModules/Scripts/Extensions/Devkit/Editor/SpriteSelectorWindow.cs:157`（`DrawToolbarSearchTextField`）
- Title + ContentsScope の入れ子: `Client/Assets/UniModules/Scripts/Modules/BehaviourControl/Editor/BehaviorControlMonitor.cs`
- 選択可能 HelpBox: `Client/Assets/UniModules/Scripts/Modules/TextData/TextSetter/Editor/TextSetterInspector.cs:175`
- SaveAsset: `Client/Assets/Scripts/Editor/Build/Build.cs:186`

## API(主要公開メンバー)

### EditorLayoutTools（static partial）

見出し・コンテナ系（`EditorLayoutTools.cs`）:

| メンバー | 説明 |
|---|---|
| `Title(string text, params GUILayoutOption[] options)` | 帯付きセクション見出し。オーバーロードで `Color backgroundColor` / `Color labelColor` / `TitleGUIStyle` 指定可 |
| `TitleGUIStyle`（ネストクラス） | Title の書式指定（`backgroundColor` / `labelColor` / `alignment` / `fontStyle` / `width`、全て null 許容） |
| `Header(string text, string key, Color? color = null, bool defaultState = true)` | 開閉ヘッダー。開閉状態を `ProjectPrefs` に key で永続化。戻り値=開いているか |
| `Header(string text, Color? color = null, bool defaultState = true)` | 同上（text がそのまま key になる） |
| `Header(string text, bool state, Color? color = null)` | 開閉状態を呼び出し側フィールドで管理する版 |
| `Foldout(string text, bool display)` | ShurikenModuleTitle 風の折りたたみ。戻り値を display に代入して使う |
| `ContentTitle(string text, Color? color = null)` | ツールバー風の小見出し |
| `ColumnHeader(Tuple<string, GUILayoutOptions>[] contents)` | 表の列ヘッダー行 |
| `ColorButton(string text, bool enabled, Color color, params GUILayoutOption[] options)` | 色付き + 無効化対応ボタン |
| `PrefixButton(string text, params GUILayoutOption[] options)` | DropDown スタイルのボタン（引数なし版は幅76固定） |
| `Outline(Rect rect, Color color)` | 矩形の枠線描画（Repaint 時のみ） |
| `SingleLineHeight` | `EditorGUIUtility.singleLineHeight` の別名 |

入力フィールド系（`EditorLayoutTools.field.cs`）— ラベル幅を文字幅に自動調整する点が素の EditorGUILayout との違い:

| メンバー | 説明 |
|---|---|
| `ObjectField<T>(T obj, bool allowSceneObjects, ...)` / `ObjectField<T>(string label, T obj, bool allowSceneObjects, ...)` | 型指定 ObjectField（キャスト不要） |
| `TextField(string label, string text, float space = 0f, ...)` / `DelayedTextField` | ラベル幅自動調整テキスト入力 |
| `IntField` / `DelayedIntField` / `FloatField` / `DelayedFloatField`（引数は TextField と同型） | 数値入力 |
| `BoolField(string label, bool state, float space = 0f, ...)` | トグル |
| `IntRangeField(string prefix, string leftCaption, string rightCaption, int x, int y, bool editable = true)` / `DelayedIntRangeField` | 2値レンジ入力（戻り値 `Vector2Int`） |
| `DrawSearchTextField(string searchText, Action<string> onChangeSearchText = null, Action onSearchCancel = null, ...)` | 検索ボックス（×ボタン付き）。`DrawToolbarSearchTextField`（ツールバー用）/ `DrawDelayedSearchTextField` / `DrawDelayedToolbarSearchTextField` |
| `GetTextFieldLineCount(string text)` / `GetTextFieldHight(string text, int? maxLine = null)` | 複数行テキストの行数・高さ計算（**Hight は原文ママのタイポ**） |

ドラッグ&ドロップ（`EditorLayoutTools.draganddrop.cs`）:

| メンバー | 説明 |
|---|---|
| `DragAndDrop<T>(string text, float widthMin = 0, float? height = null)` | D&D 受付エリア描画。ドロップされたフレームのみ T を返す（それ以外 null） |
| `MultipleDragAndDrop<T>(string text, float widthMin = 0, float? height = null)` | 複数版。非ドロップ時は空配列 |

Sprite / Texture 描画（`EditorLayoutTools.sprite.cs` / `.texture.cs`）:

| メンバー | 説明 |
|---|---|
| `DrawSprite(Sprite sprite, Rect drawArea, Vector4 border, Color color)` | Sprite 描画（内部で `UnityEditor.UI.SpriteDrawUtility` をリフレクション実行） |
| `DrawSprite(Rect rect, Sprite sprite, Color color, bool hasSizeLabel)` ほか Texture2D 直指定のオーバーロード2種 | 市松背景 + ボーダー線 + サイズラベル付きプレビュー |
| `DrawTexture(Rect rect, float imageSize, Texture texture, Rect? uv = null)` | アスペクト比維持のテクスチャ描画（市松背景付き） |
| `DrawTiledTexture(Rect rect, Texture tex)` | テクスチャのタイル敷き詰め描画 |
| `backdropTexture` / `contrastTexture` | 市松模様テクスチャ（遅延生成・使い回し） |

ユーティリティ（`EditorLayoutTools.utility.cs`）:

| メンバー | 説明 |
|---|---|
| `DefaultHeaderColor` / `DefaultContentColor` / `BackgroundColor` / `LabelColor` | Pro / Light スキン対応の標準色 |
| `SetLabelWidth(float width)` / `SetLabelWidth(string text)` | `EditorGUIUtility.labelWidth` を設定し**変更前の値を返す**（復元用） |
| `ConvertSlashToUnicodeSlash(string)` / `ConvertUnicodeSlashToSlash(string)` | '/' ⇔ '∕'(U+2215) 変換（メニュー・ポップアップ項目名に '/' を含めたい時） |

### SingletonEditorWindow&lt;T&gt;

| メンバー | 説明 |
|---|---|
| `static T Instance` | 既存ウィンドウ検索 or `CreateInstance<T>()`。初回生成時に `OnCreateInstance()` を呼ぶ |
| `static bool IsExist` | 生成せずに存在確認 |
| `CompositeDisposable Disposable` | R3 購読の寿命管理（`LifetimeDisposable` 由来。`.AddTo(Disposable)` で使う） |
| `Observable<Unit> OnDestroyAsObservable()` | ウィンドウ破棄通知 |
| `protected virtual void OnCreateInstance()` | タイトル・minSize 等の初期化フック |

### EditorGUIFastScrollView&lt;T&gt;（abstract）

| メンバー | 説明 |
|---|---|
| `abstract Direction Type` | `Direction.Vertical` / `Horizontal` を返す（実装必須） |
| `protected abstract void DrawContent(int index, T content)` | 1アイテムの描画（実装必須） |
| `T[] Contents` | 表示データ。set で `Refresh()` + `OnContentsUpdate()` が走る |
| `void Draw(bool scrollEnable = true, params GUILayoutOption[] options)` | OnGUI から毎フレーム呼ぶ |
| `Vector2 ScrollPosition` | スクロール位置（保存・復元可） |
| `void Refresh()` | レイアウトキャッシュ破棄 + スクロール位置リセット |
| `Observable<Unit> OnRepaintRequestAsObservable()` / `void RequestRepaint()` | 再描画要求（**購読して `Repaint()` を呼ぶこと**→注意点） |
| `bool IsLayoutUpdating` | レイアウト計算が未完了か |
| `HideHorizontalScrollBar` / `HideVerticalScrollBar` / `AlwaysShowHorizontalScrollBar` / `AlwaysShowVerticalScrollBar` | スクロールバー表示制御 |
| `protected virtual void OnContentsUpdate()` / `virtual GUIStyle GetHorizontalScrollBarStyle()` / `GetVerticalScrollBarStyle()` | 拡張フック |

### SpriteSelectorWindow

| メンバー | 説明 |
|---|---|
| `static SpriteSelectorWindow Open(string title, Item[] items, object[] initialSelection, int? maxSelectCount)` | 選択ウィンドウを開く。`maxSelectCount`: null=無制限 / 1=クリック即確定 |
| `Item`（ネストクラス） | `Sprite Sprite`（null 可）/ `string Label` / `object UserData`（結果として返る値） |
| `Observable<object[]> OnConfirmAsObservable()` | Apply / 即確定時に UserData 配列を通知 |
| `Observable<Unit> OnCancelAsObservable()` | Cancel ボタン時に通知（※×閉じでは発火しない→注意点） |

### ScriptlessEditor（abstract）

| メンバー | 説明 |
|---|---|
| `protected void DrawDefaultScriptlessInspector()` | `m_Script` を除いた既定 Inspector 描画（変更時 ApplyModifiedProperties まで実施） |

### UnityEditorUtility（static）

| メンバー | 説明 |
|---|---|
| `const string AssetsFolderName` (= "Assets/") / `MetaFileExtension` (= ".meta") | パス定数 |
| `RegisterUndo<T>(T target)` / `RegisterUndo(string name, params Object[] objects)` | `Undo.RecordObjects` + `SetDirty`。**値を書き換える前に呼ぶ** |
| `FindRootObjectsInHierarchy(bool inactive = true)` / `FindAllObjectsInHierarchy(bool inactive = true)` | 現在シーンのルート / 全 GameObject 取得 |
| `HasMissingReference(GameObject gameObject)` | Missing Script / Missing 参照フィールドの検出 |
| `RequestScriptCompilation()` | コンパイル実行（2021.2+ は CleanBuildCache 付き） |
| `IsPrefab(Object instance)` / `IsPrefabInstance(Object instance)` | Prefab 判定 / Prefab 由来インスタンス判定 |
| `IsExists(string assetPath)` | アセットの実ファイル存在確認 |
| `SelectAsset(string assetPath)` / `SelectAsset(Object instance)` | Project ビューで選択状態にする |
| `SaveAsset(Object asset)` | `SetDirty` + `SaveAssetIfDirty`（プロジェクトアセット以外はエラーログ） |
| `IsFolder(Object assetObject)` / `OpenFolder(string path)` / `GetAssetFullPath(Object assetObject)` | フォルダ判定 / OS でフォルダを開く / フルパス取得 |
| `Task<string[]> GetAllAssetPathInFolder(string folderPath)` | フォルダ内全 AssetPath（.meta 除外）。**戻り値は UniTask ではなく Task** |
| `FindAssetPathsByType(string filter, string[] searchInFolders = null)` / `FindAssetsByType<T>(string filter, string[] searchInFolders = null)` | `AssetDatabase.FindAssets` ラッパー。filter は "t:Prefab" 等 |
| `FindMainAsset(string guid)` / `GetAssetGUID(Object asset)` | GUID ⇔ アセット変換 |
| `GetAssetBundleName(string assetPath)` | AssetBundle 名取得（未設定なら親フォルダを遡って解決） |
| `GetLocalIdentifierInFile(Object unityObject)` | ファイル内 LocalId 取得（失敗時 0） |

### その他

| メンバー | 説明 |
|---|---|
| `EditorGUIContentLayout.BeginContents()` / `EndContents()` | box 囲みレイアウト（通常は `ContentsScope` 経由で使う） |
| `EditorSplitterGUILayout.CreateSplitterState(float[] relativeSizes, int[] minSizes, int[] maxSizes)` | 分割状態を生成（戻り値は Unity 内部型のため `object` 保持） |
| `EditorSplitterGUILayout.BeginVerticalSplit(object splitterState, ...)` / `EndVerticalSplit()` | 分割領域の開始 / 終了 |
| `EditorGUISelectableHelpBox.Draw(string message, MessageType messageType)` | 選択可能テキストの HelpBox（アイコン付き） |
| `BackgroundStyle.Get(Color color)` | 単色背景の GUIStyle（共有インスタンス→注意点） |
| `TextureEditorUtility.GetStorageMemorySizeLong(Texture)` / `GetRuntimeMemorySizeLong(Texture)` | テクスチャのディスク / ランタイムメモリサイズ |
| `DebugLog.ReceiveLog/Warning/Error/Assert/Exception(...)` / `On*ReceivedAsObservable()` | ログ中継ハブ（詳細 → [Devkit (Modules)](../Modules/Devkit.md) §DebugLog） |

## 注意点・罠

- **エディタ専用**: `Editor/` 配下（＝本モジュールのほぼ全部）はエディタアセンブリ。ランタイムコードから参照するとビルドが壊れる。例外は `Debug.cs`（ビルド専用）と `Log/DebugLog.cs`（共用）のみ。
- **namespace がフォルダと不一致のクラスがある**: `TextureEditorUtility` は `Extensions`（`Extensions.Devkit` ではない）、`BackgroundStyle` は `Extensions.Devkit.Style`、`DebugLog` は `Modules.Devkit.Log`。using を書く際に注意。
- `ColorScope` にはかつて復元不整合バグ（`GUI.backgroundColor` を保存して `GUI.color` へ書き戻す）があったが、2026-07 に修正済み。現在は `GUI.color` を正しく退避・復元する。
- **`IndentLevelScope` は絶対値指定**: Unity 標準の `EditorGUI.IndentLevelScope`（増分）と挙動が違う。`new IndentLevelScope(EditorGUI.indentLevel + 1)` のように書く。
- **`Header(text, key)` の開閉状態は ProjectPrefs 永続化**: key はプロジェクト内で一意にする（実例: `"CriAssetConfigInspector-Sound"`）。key 省略版は text がそのまま key になるため、同名見出しが複数箇所にあると開閉状態が連動する。
- **`EditorGUIFastScrollView` は初回フレームを必ずスキップ**して `RequestRepaint` を発行する。`OnRepaintRequestAsObservable().Subscribe(_ => Repaint()).AddTo(Disposable)` を購読しないと、初期表示やレイアウト更新が1操作分遅れる。
- **`SingletonEditorWindow<T>.Instance` はアクセスしただけで生成される**（`CreateInstance<T>()`）。存在チェックだけなら `IsExist` を使う。
- **`SpriteSelectorWindow` は「×」で閉じると OnConfirm / OnCancel とも OnNext なしで完了する**（Cancel ボタン経由のみ OnCancel 発火）。またコンパイル開始時に自動 Close する。
- **Unity 内部 API へのリフレクション依存**: `EditorLayoutTools.DrawSprite`（`UnityEditor.UI.SpriteDrawUtility`）、`EditorSplitterGUILayout`（`UnityEditor.SplitterState` / `SplitterGUILayout`）、`TextureEditorUtility`（`UnityEditor.TextureUtil`）、`UnityEditorUtility.GetLocalIdentifierInFile`（`inspectorMode`）。Unity バージョンアップで壊れる可能性がある箇所。
- **`BackgroundStyle.Get` は共有インスタンスを返す**: 呼ぶたびに同じ static GUIStyle / Texture2D を上書きするため、戻り値のキャッシュや同一フレームでの複数色併用は不可。描画直前に都度 `Get` する。
- **`AssetEditingScope` は `GUI.Scope` ではなく `Extensions.Scope`**（ファイナライザ付き IDisposable）。OnGUI 外・バッチ処理で使える。`StartAssetEditing` の対応漏れは AssetDatabase を壊すので必ず using で使う。
- **`GetTextFieldHight` はタイポのまま**（Height ではない）。grep・呼び出し時は実名に合わせる。
- **`Debug.cs` はビルド専用**（`#if !UNITY_EDITOR`・グローバル namespace で `UnityEngine.Debug` を置換）。非デバッグビルドではログ出力を抑制する（`ENABLE_DEVKIT` 定義時は常時出力）が、`DebugLog` への中継は抑制中も常に行われる。

## 関連

- [Core](Core.md) — `Scope` 基底 / `Singleton<T>` / `UnityUtility` / `LifetimeDisposable` / `UnityPathUtility`・`PathUtility`
- [Methods](Methods.md) — 本モジュール内部でも使用する汎用拡張メソッド（`FixLineEnd` / `IsMatch` / `IsEmpty` 等）
- [Devkit (Modules)](../Modules/Devkit.md) — `Modules.Devkit.*`（ProjectPrefs / UnityConsole / CompileNotification / DebugLog 詳説）。`SingletonEditorWindow<T>` 継承ツールの実例多数
- [R3Extension](../Modules/R3Extension.md) — Observable 購読パターン（`.AddTo(Disposable)` 等）
