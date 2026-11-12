# Markdown

Markdown を整形して表示するエディタ機能。
対象のファイルを選択するとインスペクタに整形表示し、別ウィンドウでも開ける。
対象のファイル名とスタイルシートは設定アセットで指定する。エディタ専用。

## 基本情報

> **namespace**: `Modules.Devkit.Markdown`
> **場所**: `Modules/Devkit/Markdown/`（コードは `Editor/`、標準設定は `Editor/Config/`、標準のスタイルシートは `Editor/StyleSheet/`）
> **依存**: UI Toolkit（`UnityEngine.UIElements` / `UnityEditor.UIElements`）/ Extensions（`UnityUtility` / `UnityPathUtility` / `PathUtility` / `UnityEditorUtility`）/ Modules.Devkit.ScriptableObjects（`SingletonScriptableObject<T>`）

## 概要

`.md` ファイル（`TextAsset`）を、見出し・表・リスト・コードなどに整形して表示する。機能ごとの説明ファイル `Manual.md` を Unity 上で読むための表示基盤で、対象のファイル名とスタイルシートの組（ルール）を足せば、ほかの `.md` にも同じ表示を当てられる。
表示部品 `MarkdownView` は `VisualElement` なので、インスペクタ以外のエディタウィンドウにも組み込める。
主要クラス: `MarkdownView`（整形表示の `VisualElement`）/ `MarkdownWindow`（別ウィンドウ表示）/ `MarkdownAssetInspector`（`TextAsset` のインスペクタ）/ `MarkdownViewRules`（対象の判定）/ `MarkdownViewRule`（ファイル名とスタイルシートの組）/ `MarkdownViewConfig`（利用側で作る設定アセット）/ `DefaultMarkdownViewConfig`（標準設定のアセット）/ `MarkdownParser`・`MarkdownBlock`（ブロックの解析）/ `MarkdownInline`（インライン記法の変換）。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| 機能の説明を Unity 上で読みたい | 機能フォルダの `Manual.md` を選択（インスペクタに整形表示） |
| 広い画面で読みたい | インスペクタ左上の `Open Window`、または `MarkdownWindow.Open(textAsset)` |
| 原文を見たい | インスペクタ右上の `Source` |
| ほかの `.md` も整形表示したい | `MarkdownViewConfig` のアセットを作り、`Rules` にファイル名を足す |
| 見た目を変えたい | `.uss` を用意し、ルールの `Style Sheet` に指定する |
| 自作のエディタウィンドウに整形表示を組み込みたい | `new MarkdownView()` → `SetStyleSheet` → `SetMarkdown` |
| ファイルが整形表示の対象か調べたい | `MarkdownViewRules.Find(assetPath)`（対象外は `null`） |

## 使い方

### Manual.md の書き方

機能フォルダの直下に `Manual.md` を置く。先頭は次の形に揃える。

```markdown
# 機能名

何の機能かを 1 文で書く。
何ができるかを書く。
使える条件・前提があれば書く。

## 基本情報

> **namespace**: `Modules.Xxx`
> **場所**: `Modules/Xxx/`
> **依存**: ...

## 概要
```

- タイトル（`#`）の直後の段落は、短い説明（リード文）として本文より大きく表示される。
- 短い説明は 1 文 1 行で書く。行の区切りは、そのまま改行として表示される。
- 1 文目は全角 25 文字以内を目安にする（幅の狭いインスペクタでも 1 行に収まる長さ）。
- 短い説明には太字・コード記法を使わない。

### 対象のファイルを増やす

1. `MarkdownViewConfig` のアセットを 1 つ作る。
2. `Rules` に要素を足し、`File Name` に対象のファイル名（拡張子を含む）を入れる。`*` は任意の文字列に一致する（例: `*.md`）。
3. `Style Sheet` に当てるスタイルシートを入れる。空のままなら、標準設定の `Default Style Sheet` が当たる。

判定は `MarkdownViewConfig` の `Rules` を上から順に行い、どれにも当たらなければ `DefaultMarkdownViewConfig` の `Rules` を判定する。`MarkdownViewConfig` のアセットが無くても、標準設定のルールだけで動作する。

### 見た目を変える

`Editor/StyleSheet/MarkdownView.uss` を複製して編集し、ルールの `Style Sheet` に指定する。各要素には次のクラス名が付く。

| 要素 | クラス名 |
|---|---|
| 全体 | `md-root`（ダークテーマは `md-dark`、ライトテーマは `md-light` が併せて付く） |
| 見出し | `md-heading` と `md-h1` 〜 `md-h6` |
| 段落 / 短い説明 | `md-paragraph` / `md-lead` |
| 引用 | `md-quote` |
| コードブロック | `md-code-block`（言語名は `md-code-lang`、本文は `md-code`） |
| リスト | `md-list` / `md-list-item` / `md-list-marker` / `md-list-content` |
| 表 | `md-table` / `md-table-row` / `md-table-header` / `md-table-row-alt` / `md-table-cell` |
| 区切り線 / 画像 | `md-hr` / `md-image` |
| 親の中で先頭の要素 | `md-first` |

文中の色と行間は、`md-root` に指定するカスタムプロパティで変える。

| プロパティ | 内容 |
|---|---|
| `--md-link-color` | リンクの文字色 |
| `--md-inline-code-color` | 文中のコードの文字色 |
| `--md-inline-code-background-color` | 文中のコードの下地の色 |
| `--md-line-height` | 段落の行間（%。0 は指定なし） |

### 自作のウィンドウに組み込む

```csharp
var markdownView = new MarkdownView();

markdownView.SetStyleSheet(MarkdownViewRules.LoadStyleSheet(MarkdownViewRules.Find(assetPath)));
markdownView.SetMarkdown(textAsset.text, assetPath);

rootVisualElement.Add(markdownView);
```

`SetMarkdown` の第 2 引数は、相対パスのリンクと画像の基準になるアセットパス。

## 対応している記法

| 記法 | 内容 |
|---|---|
| 見出し | `#` 〜 `######` |
| 段落 | 段落内の改行は、そのまま改行として表示する |
| 強調 | `**太字**` / `*斜体*` / `~~打ち消し~~` |
| コード | `` `文中のコード` `` / バッククォート 3 つで囲んだコードブロック（言語名を表示。色分けはしない） |
| リンク | `[名前](パス)` / URL の直書き。相対パスは、アセットなら選択状態にし、それ以外は既定のアプリで開く |
| 画像 | `![代替テキスト](パス)` を単独の行に書く。アセットとして読み込める画像だけ表示する |
| リスト | `-` / `*` / `+` と番号付き。入れ子、`[ ]` / `[x]` のチェックボックス |
| 表 | `\|` 区切り。区切り行の `:` で左・中央・右揃え |
| 引用 | `>` |
| 区切り線 | `---` |

## 注意点・罠

- **HTML は表示しない**: 改行の `<br>` 以外の HTML タグは、文字としてそのまま表示する。
- **段落内の改行は改行になる**: 1 つの段落を複数行に分けて書くと、その位置で改行して表示する。
- **`TextAsset` のインスペクタを差し替えている**: `MarkdownAssetInspector` は `TextAsset` 全体のインスペクタで、対象外のファイルと複数選択時は Unity 標準のインスペクタを表示する。`TextAsset` に別のカスタムインスペクタを定義すると競合する。
- **対象のファイルはアセットのヘッダーを隠す**: 整形表示の対象を選択している間は、インポート設定のヘッダーの下に、ツールバーと本文だけを表示する。
- **標準設定のアセットが必要**: `DefaultMarkdownViewConfig` のアセット（`Editor/Config/`）が無いと、標準のルールとスタイルシートが使えない。アセットは型で検索するので、フォルダの移動や名前の変更では壊れない。
- **`MarkdownViewConfig` は 1 つだけ**: 同じ型のアセットを複数作ると、後から作った方が削除される（`SingletonScriptableObject<T>` の仕様）。
- **ウィンドウはアセット毎に 1 つ**: `MarkdownWindow.Open` は、同じアセットを表示中のウィンドウがあればそれを前面に出す。アセットの内容が更新されると、表示も更新する。

## 関連

- [Devkit](../Manual.md) — エディタ開発支援ツール群
- [Extensions/Devkit.md](../../../Documents~/Extensions/Devkit.md) — エディタ拡張の共通部品
