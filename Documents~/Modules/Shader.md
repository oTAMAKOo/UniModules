# Shader

> **namespace**: `Modules.Shaders`（**フォルダ名 `Shader/` と不一致・複数形**）
> **場所**: `Client/Assets/UniModules/Scripts/Modules/Shader/ShaderSetter/`
> **Client側使用**: 0ファイル（2026-07時点・未使用。prefab / scene からの参照もなし）
> **依存**: UnityEngine.UI / Extensions / Extensions.Devkit（エディタ）

## 概要

`Renderer` または uGUI `Image` のマテリアルの**シェーダーをシェーダー名指定で差し替える**コンポーネント。差し替え時は新規 `Material` を生成し、元マテリアルの `mainTexture` のみ引き継ぐ。
生成マテリアルは `HideFlags.DontSaveInBuild | DontSaveInEditor` のためアセットを汚さない。名前を空にすると `defaultShader`（初回 Setup 時に自動記憶した元シェーダー）へ戻る。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| Renderer / Image のシェーダーを名前で切り替えたい | `ShaderSetter.Set(string shaderName)` |
| Shader 参照を直接渡して切り替えたい | `ShaderSetter.Set(Shader shader)` |
| 元のシェーダーに戻したい | `Set((Shader)null)`（`defaultShader` にフォールバック） |
| インスペクタで初期シェーダーを指定したい | `shaderName` をシリアライズ設定（Awake で自動適用） |

## 主要クラス

| クラス | 種別 | 役割 |
|---|---|---|
| `ShaderSetter` | sealed MonoBehaviour（`[ExecuteAlways]`） | シェーダー差し替え本体。対象は同一 GameObject の Renderer か Image を自動判別 |
| `ShaderSetterInspector` | エディタ専用（`Editor/`） | `shaderName` の DelayedTextField 編集（確定時に即 `Set`） |

## 使い方(最小の想定例)

Client側に使用実績がないため想定例。

```csharp
// 想定例（本プロジェクトに実使用コードなし）.
var shaderSetter = UnityUtility.GetComponent<ShaderSetter>(gameObject);

// シェーダー名で差し替え（Shader.Find で検索）.
shaderSetter.Set("UI/Grayscale");

// 元に戻す（defaultShader にフォールバック）.
shaderSetter.Set((Shader)null);
```

## API(主要公開メンバー)

### ShaderSetter

| メンバー | 説明 |
|---|---|
| `void Set(string shaderName)` | `Shader.Find` で検索して適用。null/空なら defaultShader へ |
| `void Set(Shader shader)` | Shader 参照を直接適用（内部で `Apply()`） |
| `void Apply()` | 現在の shader（null なら defaultShader）で新規 Material を生成し差し替え |

シリアライズフィールド: `shaderName`（初期適用シェーダー名）/ `defaultShader`（フォールバック。未設定なら初回 Setup 時の現行シェーダーを自動記憶）。

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
