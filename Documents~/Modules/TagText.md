# TagText

> **namespace**: `Modules.TagTect`（**実コードのつづりが `TagTect`**。フォルダ名 TagText と不一致なので grep 注意）
> **場所**: `Client/Assets/UniModules/Scripts/Modules/TagText/`
> **依存**: Extensions のみ（MonoBehaviour ではない純粋な C# クラス。RubyTagText のみ `UnityEngine.Debug` 使用）

## 概要

リッチテキストタグ（`<color=...>` 等）を含む文字列の「文字送り（タイプライター表示）」用ビルダー。`SetText` でタグと表示文字を分解し、`Get(n)` で「表示文字 n 文字分＋タグは常に完全な形」の部分文字列を返す。タグの途中で切れた壊れたリッチテキストが画面に出るのを防ぐ。

`RubyTagText` はルビタグ（`<ruby=ふりがな>` / `<r=...>`、ThirdParty の RubyTextMeshPro 形式）対応版で、ルビ対象文字列が全部表示されるまでルビを出さない。

主要クラス: `TagText`（非MonoBehaviour。タグ分解と文字送り部分文字列の構築。protected virtual `EditTextInfos(Info[])` が拡張ポイント）/ `RubyTagText`（`TagText` 派生。ルビ表示制御）。

シンボルゲート無しでコンパイル対象。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| タグ入りテキストを1文字ずつ表示したい | `TagText.SetText(text)` → ループで `Get(i)` を表示に反映 |
| ルビ（ふりがな）付きテキストを文字送りしたい | `RubyTagText`（使い方は同じ） |
| 全文を取得したい | `Get()`（引数省略 = 元の文字列そのまま） |
| 表示文字数（タグ除く）を知りたい | `Length`（**「空文字の段階」を含むため実文字数+1**） |
| 独自タグの表示加工を挟みたい | `TagText` を継承し `EditTextInfos(Info[])` を override |

## 使い方

文字送りループ（`Get(0)`=空文字 〜 `Get(Length-1)`=全文、の順で1文字ずつ増やして通知）の実例: `Client/Assets/UniModules/Scripts/Modules/Scenario/Command/Message/Message.cs`（`#if ENABLE_XLUA` 内）。

## 注意点・罠

- **namespace のつづりが `Modules.TagTect`**（Text ではなく Tect、実コード通り）。`using Modules.TagText;` と書くとコンパイルエラー。
- `Length` は実表示文字数より **+1** 大きい（空文字の段階を含む設計）。「最後の1文字が出ない/1周多い」系のバグはここを疑う。
- `SetText` を呼んでから `Get` を使う（状態を持つクラス。使い回す場合も `SetText` で都度リセットされる）。
- タグ対応は `<...>` 形式のみ。シナリオ系の `[w]` / `[p]` のような角括弧制御タグは対象外（Scenario 側が Split で別処理している）。
- 閉じタグの無いタグ（`<br>` 等の自己完結タグ）が含まれると開きタグ管理が空にならず、打ち切り最適化が効かない（結果文字列に切り詰め位置以降のタグが全て含まれる）。表示文字は正しいが、タグが余分に出力される点に注意。
- `RubyTagText` が認識するルビタグは `ruby` / `r` の2種のみ、かつ `=パラメータ` 付きが対象。部分表示中は `<ruby=>` と空パラメータで出力されるため、表示側（RubyTextMeshPro 等）が空ルビを許容する必要がある。
- スレッドセーフではない。1インスタンス=1テキストの逐次利用前提。

## 関連

- [Scenario](Scenario.md) — 基盤内の使用元（Message コマンドの文字送り。`ENABLE_XLUA` 定義時に有効）
- [TextData](TextData.md) — 表示するテキスト自体の取得元（ローカライズ基盤）
- ThirdParty `RubyTextMeshPro` — `<ruby=...>` タグを実際に描画する TextMeshPro 拡張
