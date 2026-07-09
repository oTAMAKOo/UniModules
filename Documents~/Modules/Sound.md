# Sound

> **namespace**: `Modules.Sound`（Editor専用: `Modules.Sound.Editor` — CRI有効時のみ）
> **場所**: `Client/Assets/UniModules/Scripts/Modules/Sound/`
> **依存**: UniTask / R3 / DOTween / Extensions（`Singleton<T>`） / Modules.Devkit.Console / 条件付き: CriWare SDK・Modules.CriWare（`ENABLE_CRIWARE_ADX(_LE)` 定義時のみ）

## 概要

BGM・SE等のサウンド再生を一元管理する基盤。マスター音量 + `SoundType` 別音量、同時再生数制限、同一フレーム重複再生の抑制、フェードを提供する。
実装は2系統あり、`ENABLE_CRIWARE_ADX(_LE)` シンボルで切替わる。**シンボル未定義時は UnityAudio 版（`UnityAudio/SoundManagement`、AudioSourceプール方式）が有効**。`CriWare/` 配下（CriAtomExPlayerベース）はシンボル定義時のみコンパイル対象。
サウンド再生は基盤の `SoundManagement.Play` を直接叩くのではなく、利用側でアセットロード（配信/同梱）や BGM クロスフェード、Introloop 判別まで面倒を見るラッパーを用意することを推奨。
主要クラス: `SoundType`（音量カテゴリ enum）/ `SoundManagement`（Singleton・AudioSourceプール・同時再生上限・フェード。共通基底 `SoundManagementCore` がマスター音量・タイプ別 `SoundParam`・再生/停止等の通知を持つ）/ `SoundElement`（再生1件分のハンドル）。

Introloop（イントロ付きループBGM）等の特殊な再生要素は利用側で `SoundElement` を派生させて実装するのが定石。

再生の流れ（推奨ラッパー経由）:

```
利用側ラッパー (SE / BGM の入口 static)
    → 内部でリソース識別情報（同梱/配信の区別 + パス）を構築
    → 利用側のアセットローダーで AudioClip 等を取得
        配信 : ExternalAsset.LoadAsset<Object>(resourcePath)
        同梱 : Resources.LoadAsync<Object>（拡張子除去）
    → AudioClip なら SoundManagement.Play(type, clip)
    → 特殊フォーマット（Introloop 等）は利用側で SoundElement 派生としてラップ
```

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| SE / BGM を鳴らす | 利用側のラッパー（内部で `SoundManagement.Play` を呼ぶ）を経由。基盤単体では `SoundManagement.Instance.Play(SoundType, AudioClip)` |
| マスター音量の変更 | `SoundManagement.Instance.Volume = 0.5f;`（0〜1） |
| BGM/SE別の音量変更 | `GetSoundParam(type).volume = x;` → `UpdateSoundParam(type)` → 各 `element.UpdateVolume()` |
| 現在の実効音量取得 | `SoundManagement.Instance.GetVolume(SoundType.Se)`（マスター×タイプ） |
| 全サウンドの停止/中断/復帰 | `SoundManagement.Instance.StopAll()` / `PauseAll()` / `ResumeAll()` |
| 個別サウンドの停止・音量 | `Play` が返す `SoundElement` を保持して `Stop(element)` / `SetVolume(element, v)` |
| フェードイン/アウト/クロスフェード | `await SoundManagement.Instance.FadeIn(element, duration)` / `FadeOut(...)` / `CrossFade(in, out, duration)` |
| 再生中のサウンド列挙 | `SoundManagement.Instance.GetAllSounds()`（`playing: false` で管理中全件） |
| 再生終了を購読 | `element.OnFinishAsObservable()` |
| 再生/停止/中断/復帰/解放の通知購読 | `OnPlayAsObservable()` / `OnStopAsObservable()` / `OnPauseAsObservable()` / `OnResumeAsObservable()` / `OnReleaseAsObservable()` |
| SoundType毎の同時再生上限変更 | `SoundManagement.Instance.SetSoundLimit(type, limit)`（初期値: Bgm 2 / Ambience 4 / Jingle 4 / Voice 16 / Se 32） |
| 同時再生中の同一SEを弾く | `RegisterSoundType(type, new SoundParam { cancelIfPlaying = true })` |

## 使い方

定型パターン:

- **初期化（アプリ起動時に1回）**: `SoundManagement.CreateInstance()` → `Initialize(SoundParam)` → 使用する `SoundType` を `RegisterSoundType` で登録（例: `Bgm` / `Se`）。マスター音量は永続化した値で復元する
- **音量変更（設定画面のマスター/タイプ別）**: `GetSoundParam(type).volume` を書き換え → `UpdateSoundParam(type)` → 対象 `SoundElement` に対して `UpdateVolume()`
- **再生可否チェック（音量0なら演出ごとスキップ）**: `GetVolume(soundType)` を見て 0 なら音の後続演出（時間待ち等）もスキップする形が定石

## 注意点・罠

- **初期化必須**: `SoundManagement.CreateInstance()` → `Initialize(defaultSoundParam)`
- **R3ベース**（UniRxではない）。通知系の購読は `using R3;` が必要
- **`RegisterSoundType` していないタイプは NRE**: `GetVolume(soundType)` と `SoundElement.UpdateVolume()` は `GetSoundParam` の null チェックをしていないため、未登録タイプの音を鳴らすと NullReferenceException。使う `SoundType` はすべて事前に `RegisterSoundType` すること
- **同時再生上限**: 初期値 Bgm 2 / Ambience 4 / Jingle 4 / Voice 16 / Se 32。上限到達時は `Number` が小さい（古い）ものから停止。`SoundType.Master` は上限テーブルに無く AudioSource が0本のため再生不可（音量カテゴリ用）
- **同一フレームの同一 (SoundType, AudioClip) は再生されない**（既存要素か null が返る）。連打SEの多重防止は基盤側で担保済み
- `SoundParam.cancelIfPlaying = true` のタイプは、同クリップ再生中なら新規再生せず既存 `SoundElement` を返す
- **音を鳴らす実装はラッパー経由を原則とする**。`SoundManagement.Play` 直呼びはアセットロード・キャッシュ・Introloop判別・BGM管理を全てバイパスしてしまう
- **`CrossFade(inElement, outElement, duration)` は equal-power カーブ**で in をフェードイン + out をフェードアウトする（in が null なら out の `FadeOut` のみ）。BGM 切替時は前BGMと自動クロスフェードするラッパーを利用側で組むのが定石
- **SEは `AudioClip` 限定**（ラッパー側で `as AudioClip` する）。特殊なループフォーマット（Introloop 等）を扱う場合は利用側で BGM 専用の `SoundElement` 派生を用意する
- 利用側で `SoundElement` を派生させて実装した要素は、内部で AudioSource を使わない場合 `Source == null` になり得る。`GetAllSounds()` の要素の `Source` を触る時は null / 型チェックすること
- 音量の永続化は利用側（LocalData 等）の責務。`SoundManagement` は保存しない
- **`CriWare/` 配下と `#if ENABLE_CRIWARE_ADX(_LE)` ブロックはシンボル未定義時はコンパイルされない**
- Editor専用: `Sound/CriWare/Editor/SoundScriptGenerator` はCRI有効時のみ（`CriAssetUpdater` から呼ばれ `Sounds.Cue` を自動生成する仕組み）

## 関連

- [CriWare](CriWare.md) — CRI版実装のライブラリ初期化・アセット管理
- [ExternalAsset](ExternalAsset.md) — 配信サウンド（`SoundInfo.External = true`）のロード実体
- [LocalData](LocalData.md) — 音量・BGM ON/OFF設定の永続化に利用
- [Master](Master.md) — `BgmMaster` / `SeMaster` 等（配信サウンドのID→ResourcePath解決を利用側で実装する場合の供給元）
- [Extensions/Core](../Extensions/Core.md) — `Singleton<T>`（`CreateInstance` / `Instance` / `Exists`）
