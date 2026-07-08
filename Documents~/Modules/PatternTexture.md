# PatternTexture

> **namespace**: `Modules.PatternTexture`
> **場所**: `Client/Assets/UniModules/Scripts/Modules/PatternTexture/`
> **Client側使用**: 2ファイル（2026-07時点: `CommonAssetManager.cs` / `StorageReceiveWindow.cs`）
> **依存**: UniTask / R3 / Modules.R3Extension（`ObservableEx`） / Extensions / uGUI（`MaskableGraphic`）

## 概要

複数の類似テクスチャ（キャラ立ち絵・表情差分・連番アニメ等）を**ブロック単位で重複排除**してパックした独自アトラスアセット（`PatternTexture` = ScriptableObject）と、それを uGUI 描画する `PatternImage` のセット。
同一ピクセルのブロックを1つに共有するため、差分の少ない画像群では SpriteAtlas よりメモリ・容量効率が良い。パターン名（元テクスチャ名）で表示を切り替え、クロスフェードやアルファマップによるピクセル単位ヒットテストにも対応。
本プロジェクトでは**ナビキャラ（アシスタント）全身像**が PatternTexture アセットとして配信されている。
主要クラス: `PatternTexture`（パック済み Texture2D + パターン/ブロック情報を保持するアセット本体）/ `PatternImage`（`MaskableGraphic`。パターン切替・クロスフェード・アルファヒットテスト）/ `PatternImageAnimation`（`patternIndex` を AnimationClip で駆動するアニメーション基底）/ `PatternTexturePacker`・`PatternTextureGenerator`（**エディタ専用**。パック作成/更新 GUI と生成処理）。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| PatternTexture アセットをロードしたい | `ExternalAsset.LoadAsset<PatternTexture>(path)`（+ `Cache<PatternTexture>` 併用が定番） |
| 表示する絵柄（パターン）を切り替えたい | `PatternImage.PatternName = "テクスチャ名"` |
| 切替時にクロスフェードさせたい | `PatternImage.CrossFade = true` + `CrossFadeTime` |
| RectTransform を元画像サイズにしたい | `PatternImage.SetNativeSize()` |
| 収録パターン名を列挙したい | `PatternImage.GetAllPatternName()` / `PatternTexture.GetAllPatternData()` |
| 透明部分をクリック透過したい | `PatternImage.RaycastTarget = true`（**hasAlphaMap 付きでパックされている場合のみ**） |
| アニメーションでパターン送りしたい | `PatternImageAnimation` を継承（`patternIndex` を AnimationClip で駆動） |
| PatternTexture アセットを作成/更新したい | エディタメニュー **Extension > Tools > Open PatternTexturePacker** |

## 使い方

- アセットのロードとキャッシュ（ナビキャラ全身像）: `Cache<PatternTexture>` に無ければ `ExternalAsset.LoadAsset<PatternTexture>(loadPath)` でロードして Add（パスは `Contents/Assistant/FullBody/{assistantId}/{assistantId}.asset`）。実例: `Client/Assets/Scripts/Client/Manager/CommonAssetManager.cs` の `GetAssistantAsset()`
- 表示: `PatternImage` はプレハブに配置して SerializeField 参照（実例: `Client/Assets/Scripts/Client/Scene/Citadel/Window/StorageReceiveWindow/StorageReceiveWindow.cs`）。切替は `PatternTexture` プロパティにアセットを差す → `PatternName` に元テクスチャ名を設定 → 必要なら `SetNativeSize()`

## 注意点・罠

- **ヒットテストはアルファマップ前提**: `PatternBlockData.HasAlpha` はアルファマップ未収録だと常に false。**hasAlphaMap を付けずにパックしたアセットでは RaycastTarget を有効にしてもクリックが一切当たらない**（ボタン化する画像はパック時に「アルファマップ生成」を ON にする）
- クロスフェードは「**新旧パターンが同サイズ**」かつ再生中（`Application.isPlaying`）のみ発動。サイズ違いは即時切替
- `SetNativeSizeOnEnable`（SerializeField: setNativeSize）既定 true。レイアウトでサイズ制御したい場合は OFF にしないと OnEnable でサイズが上書きされる
- パターン名は**元テクスチャのファイル名（拡張子なし）**（`PatternName` は `Path.GetFileNameWithoutExtension` されるため拡張子付き指定も可。未登録名は `Debug.LogError`）。パック元のリネームは参照切れになる（Packer 上では GUID で Update/Missing 判定される）
- アセットの作成・更新はエディタ専用（`Extension > Tools > Open PatternTexturePacker`）。ランタイム生成は不可（`PatternTexture.Set(...)` は PatternTextureGenerator 専用。ランタイムで呼ばない）
- `PatternImage` は `Image` ではないため Sprite / SpriteAtlas とは無関係。素材の使い分け: 差分絵の多い一枚絵 → PatternTexture、通常アイコン類 → SpriteAtlas（[Cache](Cache.md) 参照）
- ILayoutElement 実装のため LayoutGroup 配下では preferredWidth/Height（= 元画像サイズ）が効く

## 関連

- [ExternalAsset](ExternalAsset.md) — PatternTexture アセットのロード元
- [Cache](Cache.md) — `Cache<PatternTexture>` によるロード結果のキャッシュ（実例あり）
- [UI](UI.md) — uGUI 拡張コンポーネント群
