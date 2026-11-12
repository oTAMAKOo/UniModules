# Shaders

シェーダーを名前で差し替える機能。
Renderer や uGUI の Image に付けて、マテリアルのシェーダーを切り替えたり元に戻したりできる。

## 基本情報

> **namespace**: `Modules.Shaders`
> **場所**: `Modules/Shaders/ShaderSetter/`（`ShaderSetter.cs` と `Editor/ShaderSetterInspector.cs`）
> **依存**: UnityEngine.UI / Extensions（`UnityUtility`）/ Extensions.Devkit（エディタ。`UnityEditorUtility`）

## 概要

`Renderer` または uGUI `Image` のマテリアルの**シェーダーをシェーダー名指定で差し替える**コンポーネント。差し替え時は新規 `Material` を生成し、元マテリアルの `mainTexture` のみ引き継ぐ。
生成マテリアルは `HideFlags.DontSaveInBuild | DontSaveInEditor` のためアセットを汚さない。名前を空にすると `defaultShader`（初回 Setup 時に自動記憶した元シェーダー）へ戻る。

主要クラス: `ShaderSetter`（sealed MonoBehaviour、`[ExecuteAlways]`。対象は同一 GameObject の Renderer か Image を自動判別。公開メソッドは `Set(string)` / `Set(Shader)` / `Apply()`）/ `ShaderSetterInspector`（エディタ専用。`ShaderName` の入力欄だけを表示し、確定すると `Set(shaderName)` を呼ぶ）。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| Renderer / Image のシェーダーを名前で切り替えたい | `ShaderSetter.Set(string shaderName)` |
| Shader 参照を直接渡して切り替えたい | `ShaderSetter.Set(Shader shader)` |
| 元のシェーダーに戻したい | `Set((Shader)null)` または `Set("")`（`defaultShader` にフォールバック） |
| インスペクタで初期シェーダーを指定したい | `shaderName` をシリアライズ設定（空でなければ Awake で自動適用） |
| 今のシェーダーでマテリアルを作り直したい | `Apply()` |

## 注意点・罠

- **namespace は `Modules.Shaders`（複数形）**。`using Modules.Shader` はコンパイルエラー
- 対象判別は Renderer → Image の順に GetComponent し**後勝ち**。両方付いている場合は Image が対象になる
- `Apply()` のたびに `new Material(shader)` を生成する。**引き継ぐのは `mainTexture` のみ**で、色・その他プロパティは初期値に戻る。頻繁に切り替えるとマテリアルが増える（明示破棄はしない）
- `shader` と `defaultShader` が両方 null の状態、または Renderer / Image がどちらも無い状態で `Apply()` を呼ぶと、何もしない
- `Shader.Find` は**ビルドに含まれるシェーダーのみ**検索可能（Always Included Shaders / Resources / 参照済みマテリアル）。実機で null になる典型原因
- **初回のセットアップ時に `shaderName` のシェーダーが `Shader.Find` で見つからないと、エラーログを出してシェーダーを適用しない**（シリアライズ済みの `shaderName` が空でなく、そのシェーダーがビルドに含まれていないとき）。この状態で `Apply()` を呼ぶと、元のシェーダーが適用され `shaderName` も元のシェーダー名に書き換わる
- `Apply()` は適用したシェーダーの名前を `shaderName` へ書き戻す。`defaultShader` もシリアライズされるフィールドで、空のときだけ初回セットアップ時の元シェーダーが入る
- 再生中の Renderer は `material`（インスタンス）、非再生時は `sharedMaterial` を操作する。`[ExecuteAlways]` のためエディタ編集時も動作する

## 関連

- [Rendering](../Rendering/Manual.md) — 描画・マテリアル関連の基盤
- [UI](../UI/Manual.md) — uGUI コンポーネント拡張（Image 系）
