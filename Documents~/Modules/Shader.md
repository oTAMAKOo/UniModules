# Shader

> **namespace**: `Modules.Shaders`（**フォルダ名 `Shader/` と不一致・複数形**）
> **場所**: `Client/Assets/UniModules/Scripts/Modules/Shader/ShaderSetter/`
> **Client側使用**: 0ファイル（2026-07時点・未使用。prefab / scene からの参照もなし）
> **依存**: UnityEngine.UI / Extensions / Extensions.Devkit（エディタ）

## 概要

`Renderer` または uGUI `Image` のマテリアルの**シェーダーをシェーダー名指定で差し替える**コンポーネント。差し替え時は新規 `Material` を生成し、元マテリアルの `mainTexture` のみ引き継ぐ。
生成マテリアルは `HideFlags.DontSaveInBuild | DontSaveInEditor` のためアセットを汚さない。名前を空にすると `defaultShader`（初回 Setup 時に自動記憶した元シェーダー）へ戻る。

主要クラス: `ShaderSetter`（sealed MonoBehaviour、`[ExecuteAlways]`。対象は同一 GameObject の Renderer か Image を自動判別）/ `ShaderSetterInspector`（エディタ専用）。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| Renderer / Image のシェーダーを名前で切り替えたい | `ShaderSetter.Set(string shaderName)` |
| Shader 参照を直接渡して切り替えたい | `ShaderSetter.Set(Shader shader)` |
| 元のシェーダーに戻したい | `Set((Shader)null)`（`defaultShader` にフォールバック） |
| インスペクタで初期シェーダーを指定したい | `shaderName` をシリアライズ設定（Awake で自動適用） |

## 注意点・罠

- **namespace は `Modules.Shaders`（複数形）**。`using Modules.Shader` はコンパイルエラー
- 対象判別は Renderer → Image の順に GetComponent し**後勝ち**。両方付いている場合は Image が対象になる
- `Apply()` のたびに `new Material(shader)` を生成する。**引き継ぐのは `mainTexture` のみ**で、色・その他プロパティは初期値に戻る。頻繁に切り替えるとマテリアルが増える（明示破棄はしない）
- `shader` と `defaultShader` が両方 null の状態で `Apply()` すると `shader.name` で NullReferenceException（Renderer/Image どちらも無い場合も `GetMaterial()` が null で同様）
- `Shader.Find` は**ビルドに含まれるシェーダーのみ**検索可能（Always Included Shaders / Resources / 参照済みマテリアル）。実機で null になる典型原因
- 再生中の Renderer は `material`（インスタンス）、非再生時は `sharedMaterial` を操作する。`[ExecuteAlways]` のためエディタ編集時も動作する

## 関連

- [Rendering](Rendering.md) — 描画・マテリアル関連の基盤
- [UI](UI.md) — uGUI コンポーネント拡張（Image 系）
