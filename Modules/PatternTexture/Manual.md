# PatternTexture

差分の多い画像をまとめて持つ機能。
似た画像を同じ部分を共有する形で 1 枚にパックし、名前で絵柄を切り替えて uGUI に表示できる。
アセットの作成と更新はエディタ専用。

## 基本情報

> **namespace**: `Modules.PatternTexture`
> **場所**: `Modules/PatternTexture/`
> **依存**: UniTask / R3 / Modules.R3Extension（`ObservableEx`） / Extensions / uGUI（`MaskableGraphic`） / Extensions.Devkit（エディタ）

## 概要

複数の類似テクスチャ（キャラ立ち絵・表情差分・連番アニメ等）を**ブロック単位で重複排除**してパックした独自アトラスアセット（`PatternTexture` = ScriptableObject）と、それを uGUI 描画する `PatternImage` のセット。
同一ピクセルのブロックを1つに共有するため、差分の少ない画像群では SpriteAtlas よりメモリ・容量効率が良い。パターン名（元テクスチャ名）で表示を切り替え、クロスフェードやアルファマップによるピクセル単位ヒットテストにも対応。
主要クラス: `PatternTexture`（パック済み Texture2D + パターン/ブロック情報を保持するアセット本体）/ `PatternImage`（`MaskableGraphic`。パターン切替・クロスフェード・アルファヒットテスト）/ `PatternImageAnimation`（`patternIndex` を AnimationClip で駆動するアニメーション基底）/ `DummyPatternImage`（エディタの非実行時だけ `PatternImage` に仮のパターンを表示する）/ `PatternTexturePacker`・`PatternTextureGenerator`（**エディタ専用**。パック作成/更新 GUI と生成処理）。

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| PatternTexture アセットをロードしたい | `ExternalAsset.LoadAsset<PatternTexture>(path)`（+ `Cache<PatternTexture>` 併用が定番） |
| 表示する絵柄（パターン）を切り替えたい | `PatternImage.PatternName = "テクスチャ名"` |
| 切替時にクロスフェードさせたい | `PatternImage.CrossFade = true` + `CrossFadeTime`（`UseUnscaledTime` で `Time.timeScale` の影響を外せる） |
| クロスフェードを途中で止めたい | `PatternImage.StopCrossFade()` |
| 表示中のパターンの情報（幅・高さ等）を取りたい | `PatternImage.Current`（`PatternData`） / `PatternImage.GetPatternData(patternName)` |
| RectTransform を元画像サイズにしたい | `PatternImage.SetNativeSize()` |
| 収録パターン名を列挙したい | `PatternImage.GetAllPatternName()` / `PatternTexture.GetAllPatternData()` |
| 透明部分をクリック透過したい | `PatternImage.RaycastTarget = true`（**hasAlphaMap 付きでパックされている場合のみ**） |
| アニメーションでパターン送りしたい | `PatternImageAnimation` を継承（`patternIndex` を AnimationClip で駆動） |
| 実行時に読み込むパターンを、エディタでだけ仮に表示したい（参照をシーン・プレハブ・ビルドに含めない） | `DummyPatternImage` を `PatternImage` と同じ GameObject に付け、インスペクタで PatternTexture とパターンを選ぶ |
| PatternTexture アセットを作成/更新したい | エディタメニュー **Extension > Tools > Open PatternTexturePacker** |

## 使い方

- アセットのロードとキャッシュ: `Cache<PatternTexture>` に無ければ `ExternalAsset.LoadAsset<PatternTexture>(loadPath)` でロードして Add
- 表示: `PatternImage` はプレハブに配置して SerializeField 参照。切替は `PatternTexture` プロパティにアセットを差す → `PatternName` に元テクスチャ名を設定 → 必要なら `SetNativeSize()`

## 注意点・罠

- **ヒットテストはアルファマップ前提**: `PatternBlockData.HasAlpha` はアルファマップ未収録だと常に false。**hasAlphaMap を付けずにパックしたアセットでは RaycastTarget を有効にしてもクリックが一切当たらない**（ボタン化する画像はパック時に `AlphaMap` を ON にする）。全ピクセルが透明なブロックは描画も当たり判定もしない
- クロスフェードは「**新旧パターンが同サイズ**」かつ再生中（`Application.isPlaying`）のみ発動。サイズ違いは即時切替
- `SetNativeSizeOnEnable`（SerializeField: setNativeSize）既定 true。レイアウトでサイズ制御したい場合は OFF にしないと OnEnable でサイズが上書きされる
- パターン名は**元テクスチャのファイル名（拡張子なし）**（`PatternName` は `Path.GetFileNameWithoutExtension` されるため拡張子付き指定も可。未登録名は `Debug.LogError`）。パック元のリネームは参照切れになる（Packer 上では GUID で Update/Missing 判定される）
- `PatternImage.PatternTexture` が未設定のあいだ、`PatternName` の設定は何もせずに無視される（先にアセットを差す）。`PatternTexture` を別のアセットへ差し替えたあとは `PatternName` を設定し直す
- インスペクタで PatternTexture を外す（None にする）と、選択中のパターン名（`selectionPatternName`）も空になる。別のアセットへ差し替えたときは残る。コードから `PatternTexture` に null を入れたときは残る
- `PatternName` に null・空文字を設定すると選択が外れる（`PatternName` は null を返す）
- `PatternImageAnimation` は abstract。継承して `GetPatternNames()` を実装する。`patternIndex` が変わった `Update` でパターンを切り替え、`setNativeSize` が ON なら `SetNativeSize()` も呼ぶ
- アセットの作成・更新はエディタ専用（`Extension > Tools > Open PatternTexturePacker`）。ランタイム生成は不可（`PatternTexture.Set(...)` は PatternTextureGenerator 専用。ランタイムで呼ばない）
- `PatternImage` は `Image` ではないため Sprite / SpriteAtlas とは無関係。素材の使い分け: 差分絵の多い一枚絵 → PatternTexture、通常アイコン類 → SpriteAtlas（[Cache](../Cache/Manual.md) 参照）
- ILayoutElement 実装のため LayoutGroup 配下では preferredWidth/Height（= 元画像サイズ）が効く

### DummyPatternImage（エディタ専用の仮表示）

- 保存するのは PatternTexture アセットの guid と、パターン名の SHA256 ハッシュ（`StringExtensions.GetHash`）だけ。どちらも `UNITY_EDITOR` の中のフィールドで、ビルドには書き出されない。アセットへの参照を持たないため、ビルドの依存にも入らない
- 仮表示はエディタの非実行時、かつ `PatternImage.PatternTexture` が未設定のときだけ描画される。PatternTexture を設定すると、そちらが優先される
- 仮表示は `PatternImage` の保存されるフィールド（`patternTexture` / `selectionPatternName`）と RectTransform を書き換えない。`PatternTexture` / `PatternName` / `Current` / `preferredWidth` / `preferredHeight` は仮表示を返さない
- 仮表示のサイズに合わせるときは、`PatternImage` のインスペクタの SetNativeSize を押す（`SetNativeSize()` は仮表示中なら仮表示のパターンのサイズを使う）。自動では合わせない
- パターンはハッシュで照合するため、パック元のテクスチャ名を変えると仮表示されなくなる（インスペクタに警告が出る）
- 実行時は何もしない。`PatternImage` はパターン未設定なら何も描画しない

## 関連

- [ExternalAssets](../ExternalAssets/Manual.md) — PatternTexture アセットのロード元
- [Cache](../Cache/Manual.md) — `Cache<PatternTexture>` によるロード結果のキャッシュ
- [UI](../UI/Manual.md) — uGUI 拡張コンポーネント群
