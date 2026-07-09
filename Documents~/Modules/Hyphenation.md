# Hyphenation

> **namespace**: `Modules.Hyphenation`
> **場所**: `Client/Assets/UniModules/Scripts/Modules/Hyphenation/`
> **依存**: Extensions / uGUI（`Text`, `UIBehaviour`） / TextMeshPro（`TMPro`）

## 概要

日本語の禁則処理（行頭に「。」「ー」等・行末に「（」等が来ないようにする改行制御）基盤。
(1) 改行済みテキストに禁則を後適用する static `Hyphenation.Format`、(2) `RectTransform` 幅に合わせて禁則込みの自動改行を行うテキスト用コンポーネント（uGUI `Text` 版 / `TextMeshProUGUI` 版）の2段構成。

主要クラス: `Hyphenation`（static。禁則文字テーブルと判定・整形）/ `TextHyphenationBase`（自動改行の本体。実測幅で `\n` 挿入、`LateUpdate` でテキスト変更を監視）/ `TextHyphenation`（uGUI `Text` 版）/ `TextMeshProHyphenation`（TextMeshPro 版）。

シンボルゲート・外部SDK不要でコンパイル対象。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| uGUI `Text` を禁則込みで自動改行したい | `TextHyphenation`（Text と同じ GameObject に付与するだけ） |
| `TextMeshProUGUI` を禁則込みで自動改行したい | `TextMeshProHyphenation`（TMP と同じ GameObject に付与するだけ） |
| 改行位置決定済みの文字列に禁則だけ適用したい | `Hyphenation.Format(text)`（static、コンポーネント不要） |
| ある文字が行頭禁則/行末禁則か判定したい | `Hyphenation.CheckHyphenationFront(c)` / `CheckHyphenationBack(c)` |
| 英単語の途中で改行させたくない | コンポーネント版が対応済み（`Hyphenation.IsLatin` で単語単位に分割） |
| 計測時に無視するリッチテキストタグを変えたい | `TextHyphenationBase.RitchTextReplace`（正規表現、set可） |
| 即時に整形して反映したい | `UpdateText(text)`（`LateUpdate` の自動検知を待たない） |

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
