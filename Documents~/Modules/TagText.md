# TagText

> **namespace**: `Modules.TagTect`（**実コードのつづりが `TagTect`**。フォルダ名 TagText と不一致なので grep 注意）
> **場所**: `Client/Assets/UniModules/Scripts/Modules/TagText/`
> **Client側使用**: 0ファイル（2026-07時点。基盤内では `Modules.Scenario` の Message コマンドが使用するが `#if ENABLE_XLUA` 内のためコンパイル対象外）
> **依存**: Extensions のみ（MonoBehaviour ではない純粋な C# クラス。RubyTagText のみ `UnityEngine.Debug` 使用）

## 概要

リッチテキストタグ（`<color=...>` 等）を含む文字列の「文字送り（タイプライター表示）」用ビルダー。`SetText` でタグと表示文字を分解し、`Get(n)` で「表示文字 n 文字分＋タグは常に完全な形」の部分文字列を返す。タグの途中で切れた壊れたリッチテキストが画面に出るのを防ぐ。

`RubyTagText` はルビタグ（`<ruby=ふりがな>` / `<r=...>`、ThirdParty の RubyTextMeshPro 形式）対応版で、ルビ対象文字列が全部表示されるまでルビを出さない。

**コンパイル対象**（シンボルゲート無し）で使用可能な状態。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| タグ入りテキストを1文字ずつ表示したい | `TagText.SetText(text)` → ループで `Get(i)` を表示に反映 |
| ルビ（ふりがな）付きテキストを文字送りしたい | `RubyTagText`（使い方は同じ） |
| 全文を取得したい | `Get()`（引数省略 = 元の文字列そのまま） |
| 表示文字数（タグ除く）を知りたい | `Length`（**「空文字の段階」を含むため実文字数+1**） |
| 独自タグの表示加工を挟みたい | `TagText` を継承し `EditTextInfos(Info[])` を override |

## 主要クラス

| クラス | 種別 | 役割 |
|---|---|---|
| `TagText` | class（非MonoBehaviour） | タグ分解（`Regex.Split` で `<...>` を分離）と文字送り部分文字列の構築。`EditTextInfos` が拡張ポイント（virtual、既定は無加工） |
| `RubyTagText` | sealed class（`TagText` 派生） | `ruby` / `r` タグの表示制御。ルビ対象テキストが部分表示の間はタグを `<ruby=>`（空パラメータ）に書き換え、全文表示された時だけふりがなを出す |

## 使い方(実例)

基盤内の実使用（シナリオのメッセージ文字送り。`#if ENABLE_XLUA` 内のため本プロジェクトではコンパイル対象外だが、使い方の参考として有効）。

```csharp
// 引用元: Client/Assets/UniModules/Scripts/Modules/Scenario/Command/Message/Message.cs（簡略化）
using Modules.TagTect;

protected abstract TagText TagText { get; }  // 派生側で TagText or RubyTagText を返す.

TagText.SetText(element);

// Get(0)=空文字 〜 Get(Length-1)=全文、の順で1文字ずつ増やして通知.
for (var i = 0; i < TagText.Length; i++)
{
    var t = TagText.Get(i);

    onRequestTextChange.OnNext(current + t);

    // （1文字ごとのディレイ待ち処理）.
}

current.Append(TagText.Get());  // 引数省略で全文.
```

```csharp
// 想定例: ルビ付きテキストの文字送り（RubyTextMeshPro のタグ形式）.
var tagText = new RubyTagText();

tagText.SetText("<ruby=けんじゃ>賢者</ruby>の石");

// 「賢者」が2文字とも表示されるまで <ruby=>（ルビ無し）として返る.
var text = tagText.Get(1);   // "<ruby=>賢"...
var full = tagText.Get(5);   // "<ruby=けんじゃ>賢者</ruby>の石" 相当.
```

## API(主要公開メンバー)

### TagText / RubyTagText 共通

| メンバー | 説明 |
|---|---|
| `SetText(string origin)`（virtual） | タグ入り文字列を解析して保持。`Get` の前に必須 |
| `Get(int length = -1) : string` | 表示文字 `length` 文字分の文字列（タグ込み・タグは壊れない）。`-1` は元文字列そのまま。範囲外は 0〜`Length` にクランプ |
| `Length : int` | 表示文字数 + 1（`Get(0)`=空文字 の段階を含むループ用。`for (i = 0; i < Length; i++)` で空文字→全文になる） |
| `EditTextInfos(Info[]) : Info[]`（protected virtual） | 構築直前のタグ/文字情報を加工する拡張ポイント（`RubyTagText` はここでルビ制御を実装） |

## 注意点・罠

- **namespace のつづりが `Modules.TagTect`**（Text ではなく Tect、実コード通り）。`using Modules.TagText;` と書くとコンパイルエラー。
- `Length` は実表示文字数より **+1** 大きい（空文字の段階を含む設計）。「最後の1文字が出ない/1周多い」系のバグはここを疑う。
- `SetText` を呼んでから `Get` を使う（状態を持つクラス。使い回す場合も `SetText` で都度リセットされる）。
- タグ対応は `<...>` 形式のみ。シナリオ系の `[w]` / `[p]` のような角括弧制御タグは対象外（Scenario 側が Split で別処理している）。
- 閉じタグの無いタグ（`<br>` 等の自己完結タグ）が含まれると開きタグ管理が空にならず、打ち切り最適化が効かない（結果文字列に切り詰め位置以降のタグが全て含まれる）。表示文字は正しいが、タグが余分に出力される点に注意。
- `RubyTagText` が認識するルビタグは `ruby` / `r` の2種のみ、かつ `=パラメータ` 付きが対象。部分表示中は `<ruby=>` と空パラメータで出力されるため、表示側（RubyTextMeshPro 等）が空ルビを許容する必要がある。
- スレッドセーフではない。1インスタンス=1テキストの逐次利用前提。

## 関連

- [Scenario](Scenario.md) — 基盤内の唯一の使用元（Message コマンドの文字送り。ENABLE_XLUA 未定義のため現状無効）
- [TextData](TextData.md) — 表示するテキスト自体の取得元（ローカライズ基盤）
- ThirdParty `RubyTextMeshPro`（`Client/Assets/ThirdParty/RubyTextMeshPro/`） — `<ruby=...>` タグを実際に描画する TextMeshPro 拡張
