# Hyphenation

> **namespace**: `Modules.Hyphenation`
> **場所**: `Client/Assets/UniModules/Scripts/Modules/Hyphenation/`
> **Client側使用**: 0ファイル（2026-07時点。プレハブ/シーンからの参照も無し）
> **依存**: Extensions / uGUI（`Text`, `UIBehaviour`） / TextMeshPro（`TMPro`）

## 概要

日本語の禁則処理（行頭に「。」「ー」等・行末に「（」等が来ないようにする改行制御）基盤。
(1) 改行済みテキストに禁則を後適用する static `Hyphenation.Format`、(2) `RectTransform` 幅に合わせて禁則込みの自動改行を行うテキスト用コンポーネント（uGUI `Text` 版 / `TextMeshProUGUI` 版）の2段構成。

**コンパイル対象**（シンボルゲート無し・外部SDK不要）で使用可能な状態だが、本プロジェクトでは Client コード・アセットともに未使用。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| uGUI `Text` を禁則込みで自動改行したい | `TextHyphenation`（Text と同じ GameObject に付与するだけ） |
| `TextMeshProUGUI` を禁則込みで自動改行したい | `TextMeshProHyphenation`（同上） |
| 改行位置決定済みの文字列に禁則だけ適用したい | `Hyphenation.Format(text)`（static、コンポーネント不要） |
| ある文字が行頭禁則/行末禁則か判定したい | `Hyphenation.CheckHyphenationFront(c)` / `CheckHyphenationBack(c)` |
| 英単語の途中で改行させたくない | コンポーネント版が対応済み（`Hyphenation.IsLatin` で単語単位に分割） |
| 計測時に無視するリッチテキストタグを変えたい | `TextHyphenationBase.RitchTextReplace`（正規表現、set可） |

## 主要クラス

| クラス | 種別 | 役割 |
|---|---|---|
| `Hyphenation` | static class | 禁則文字テーブル（行頭禁則: 閉じ括弧・小書き仮名・長音・句読点等 / 行末禁則: 開き括弧類）と判定・整形。`Format` は**既に改行を含む**テキストの行間で文字を移動して禁則を解消する |
| `TextHyphenationBase` | abstract UIBehaviour | 自動改行の本体。テキストを単語（英字連続・禁則ぶら下がり考慮）に分割し、実測幅（`preferredWidth`）が `RectTransform` 幅を超える位置に `\n` を挿入。`LateUpdate` でテキスト変更を監視し自動再整形 |
| `TextHyphenation` | sealed MonoBehaviour（`[ExecuteAlways]` `[RequireComponent(typeof(Text))]`） | uGUI `Text` 版。整形時に `horizontalOverflow = Overflow` を強制（改行はこのコンポーネントが担う） |
| `TextMeshProHyphenation` | sealed MonoBehaviour（`[ExecuteAlways]` `[RequireComponent(typeof(TextMeshProUGUI))]`） | TextMeshPro 版。`overflowMode = Overflow` を強制 |

## 使い方(実例)

Client側の使用実績が無いため最小の想定例。

```csharp
// 想定例1: コンポーネント版（推奨）.
// TextMeshProUGUI と同じ GameObject に TextMeshProHyphenation を付与するだけで、
// text を書き換えると次の LateUpdate で禁則込みの改行に自動整形される.
textComponent.text = TextData.Get(TextType.SomeDescription);

// 即時に整形したい場合は明示的に呼ぶ.
using Modules.Hyphenation;

[SerializeField]
private TextMeshProHyphenation hyphenation = null;

hyphenation.UpdateText(TextData.Get(TextType.SomeDescription));
```

```csharp
// 想定例2: static 版（改行済みテキストの禁則後処理のみ）.
using Modules.Hyphenation;

var formatted = Hyphenation.Format(multiLineText);
```

## API(主要公開メンバー)

### Hyphenation（static）

| メンバー | 説明 |
|---|---|
| `Format(string text) : string` | 改行済みテキストに行頭・行末禁則を適用（禁則文字を前後の行へ移動。1行だけの場合は無処理） |
| `CheckHyphenationFront(char) : bool` | 行頭禁則文字か（閉じ括弧類・小書き仮名・ハイフン類・「！？」・句読点等） |
| `CheckHyphenationBack(char) : bool` | 行末禁則文字か（開き括弧類） |
| `IsLatin(char) : bool` | 英数字・記号（単語分割の判定用） |
| `IsLineEndChar(char) : bool` | `\n` か |

### TextHyphenationBase（TextHyphenation / TextMeshProHyphenation 共通）

| メンバー | 説明 |
|---|---|
| `UpdateText(string text)` | 明示的に整形して反映（同一テキスト・同一幅なら何もしない）。`LateUpdate` の自動検知を待たず即時反映したい時に使う |
| `TextWidth : float` | 折り返し幅（get は `RectTransform.rect.width`、set は `SetSizeWithCurrentAnchors` で幅変更） |
| `RitchTextReplace : string` | 幅計測時に除去するリッチテキストタグの正規表現（既定: color / size / link / b / i） |

## 注意点・罠

- **BestFit（自動フォントサイズ）と併用不可**。相互にサイズを変え合い最終サイズが正しく取れない（`TextHyphenation.cs` 冒頭コメント）。
- 改行の主体はこのコンポーネント。整形時に `horizontalOverflow` / `overflowMode` を **Overflow に強制**するため、テキストコンポーネント自身の折り返し設定は効かなくなる。
- テキスト変更は `LateUpdate` で検知して自動再整形されるが、**`RectTransform` のサイズ変更では自動追従しない**（テキストが同じ値のままだと再整形されない）。リサイズ後は `UpdateText(元テキスト)` を呼び直す。
- 幅計測（`preferredWidth`）のため、計測中に**実コンポーネントの text を一時的に書き換える**。`Text.text` の変更イベント等をフックしている場合は計測時の中間値が流れることに注意。
- 計測時のタグ除去正規表現（`RITCH_TEXT_REPLACE`）で完全に除去できるのは `<color=...>` / `<b>` / `<i>` 系のみ。`<size=.n>` / `<link=.n>` のパターンは「任意1文字+n」にしかマッチせず実質機能しない。size/link や独自タグ（`<ruby=...>` 等）を含むテキストでは計測幅がずれるため、必要なら `RitchTextReplace` を差し替える。
- `Hyphenation.Format`（static版）は行の幅を一切測らない。禁則文字を前行末へ**追い出すだけ**なので、結果として行が表示幅を超える可能性がある（幅込みで整形したいならコンポーネント版を使う）。
- `[ExecuteAlways]` のためエディタ編集中も整形が走る（プレハブ上のテキストが整形済みの値で保存され得る）。
- 毎フレーム文字列比較 + 変更時は単語ごとの実測（text 差し替え）が走るため、長文・高頻度更新のテキストではコストに注意。

## 関連

- [TextData](TextData.md) — 表示テキストの取得元（ローカライズ基盤）
- [TagText](TagText.md) — タグ入りテキストの文字送り（こちらは幅・改行は扱わない）
- [UI](UI.md) — uGUI 基盤
