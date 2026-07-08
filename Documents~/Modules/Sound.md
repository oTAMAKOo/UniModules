# Sound

> **namespace**: `Modules.Sound`（Editor専用: `Modules.Sound.Editor` — CRI有効時のみ）
> **場所**: `Client/Assets/UniModules/Scripts/Modules/Sound/`
> **Client側使用**: 約24ファイル（2026-07時点）
> **依存**: UniTask / R3 / DOTween / Extensions（`Singleton<T>`） / Modules.Devkit.Console / 条件付き: CriWare SDK・Modules.CriWare（`ENABLE_CRIWARE_ADX(_LE)`、**本プロジェクトでは無効**）

## 概要

BGM・SE等のサウンド再生を一元管理する基盤。マスター音量 + `SoundType` 別音量、同時再生数制限、同一フレーム重複再生の抑制、フェードを提供する。
実装は2系統あり、`ENABLE_CRIWARE_ADX(_LE)` シンボルで切替わる。**本プロジェクトはシンボル未定義のため UnityAudio 版（`UnityAudio/SoundManagement`、AudioSourceプール方式）が有効**。`CriWare/` 配下（CriAtomExPlayerベース）はコンパイル対象外。
Claude が音を鳴らす時の入口は基盤ではなく **Client側ラッパー `SoundPlayer`（`Dominion.Client`、static）**。アセットロード（`AudioAssetManager`）・BGMクロスフェード・Introloop判別まで面倒を見る。
主要クラス: 基盤 = `SoundType`（音量カテゴリ enum）/ `SoundManagement`（Singleton・AudioSourceプール・同時再生上限・フェード。共通基底 `SoundManagementCore` がマスター音量・タイプ別 `SoundParam`・再生/停止等の通知を持つ）/ `SoundElement`（再生1件分のハンドル）。Client側 = `SoundPlayer` / `SoundInfo`（`ResourcePath` + `External` 指定）/ `AudioAssetManager`（ロード+キャッシュ、未使用60秒で自動解放）/ `BgmManager`（現在BGMの状態保持・5分間隔の変更抑制・BGM ON/OFF設定の永続化）/ `IntroloopSoundElement`（E7.Introloop によるイントロ付きループBGM）/ `BattleSoundManager`（戦闘SE）

再生の流れ（本プロジェクト）:

```
SoundPlayer.Se(Sounds.Se.xxx) / SoundPlayer.Bgm(bgmId)
    → SoundInfo（"Sound/" プレフィックス付き resourcePath + External フラグ）
    → AudioAssetManager.GetAudioAsset(info)
        External=true  : ExternalAsset.LoadAsset<Object>(resourcePath)（配信アセット）
        External=false : Resources.LoadAsync<Object>（アプリ同梱、拡張子除去）
    → AudioClip なら SoundManagement.Play(type, clip) / IntroloopAudio なら IntroloopSoundElement
```

- SE定義（アプリ同梱）: `Constants.Sounds.Se` enum + `SeDefine`（`Client/Assets/Scripts/Constants/Sounds.cs`）
- 配信サウンド: `SeMaster` / `BgmMaster` の `ResourcePath` カラム（`SoundPlayer.Se(uint seId)` / `Bgm(uint bgmId)`）

## 逆引き（〜したい）

| やりたいこと | 使うもの |
|---|---|
| **SEを鳴らす（最頻出）** | `SoundPlayer.Se(Sounds.Se.xxx).Forget()`（同梱SE） |
| SEをマスターIDで鳴らす | `await SoundPlayer.Se(seId)`（`SeMaster` 参照） |
| **BGMを鳴らす** | `SoundPlayer.Bgm(Sounds.Bgm.xxx)` / `Bgm(bgmId)` / `Bgm(soundInfo)`（自動クロスフェード） |
| BGMを止める | `SoundPlayer.StopBgm()` |
| 戦闘中のSE再生 | `BattleSoundManager.Instance.PlaySe(seId)` / `DelaySe(...)`（SyncMode中は再生しない） |
| マスター音量の変更 | `SoundManagement.Instance.Volume = 0.5f;`（0〜1） |
| BGM/SE別の音量変更 | `GetSoundParam(type).volume = x;` → `UpdateSoundParam(type)` → 各 `element.UpdateVolume()` |
| 現在の実効音量取得 | `SoundManagement.Instance.GetVolume(SoundType.Se)`（マスター×タイプ） |
| 音が鳴る状態か判定（音量0チェック） | `BattleSoundManager.Instance.IsEnable(SoundType.Se)` |
| 全サウンドの停止/中断/復帰 | `SoundManagement.Instance.StopAll()` / `PauseAll()` / `ResumeAll()` |
| 個別サウンドの停止・音量 | `Play` が返す `SoundElement` を保持して `Stop(element)` / `SetVolume(element, v)` |
| フェードイン/アウト/クロスフェード | `await SoundManagement.Instance.FadeIn(element, duration)` / `FadeOut(...)` / `CrossFade(in, out, duration)` |
| 再生中のサウンド列挙 | `SoundManagement.Instance.GetAllSounds()`（`playing: false` で管理中全件） |
| 再生終了を購読 | `element.OnFinishAsObservable()` |
| 再生/停止/中断/復帰/解放の通知購読 | `OnPlayAsObservable()` / `OnStopAsObservable()` / `OnPauseAsObservable()` / `OnResumeAsObservable()` / `OnReleaseAsObservable()` |
| SoundType毎の同時再生上限変更 | `SoundManagement.Instance.SetSoundLimit(type, limit)`（初期値: Bgm 2 / Ambience 4 / Jingle 4 / Voice 16 / Se 32） |
| 同時再生中の同一SEを弾く | `RegisterSoundType(type, new SoundParam { cancelIfPlaying = true })` |
| 現在のBGM情報・BGM変更通知 | `BgmManager.Instance.CurrentInfo` / `CurrentElement` / `OnBgmChangedAsObservable()` |
| ホームBGMの選曲（除外設定考慮） | `BgmManager.Instance.GetPlayRequestBgmId()` / `SetDisableBgms(bgmIds)` |
| 同梱SEを新規追加 | `Constants.Sounds.Se` enum + `SeDefine` に追記（アセットは `Resources/Sound/Se/`） |
| 未使用キャッシュの即時解放 | `AudioAssetManager.Instance.Clear()`（通常は60秒で自動解放） |

## 使い方

定型パターン（コードは引用元を読む）:

- 初期化（アプリ起動時に1回。通常は実施済みのため書く必要なし）: `Client/Assets/Scripts/Client/Core/Initialize/InitializeObject/InitializeObject.manager.cs`（`CreateInstance` → `Initialize(SoundParam)` → `PlayData` の保存音量で `Volume` 設定 + `RegisterSoundType(Bgm / Se)`）
- SE再生（同梱SE enum 指定・fire-and-forget）: `Client/Assets/Scripts/Client/Core/UI/UIButton.cs` / `Client/Assets/Scripts/Client/Core/Popup/WindowBase.cs`（`SoundPlayer.Se(Sounds.Se.xxx).Forget()`）
- BGM再生（enum / マスターID）: `Client/Assets/Scripts/Client/Core/Scene/SceneBase.cs` の `SetBgm` / `Client/Assets/Scripts/Client/Scene/Title/TitleView.cs`
- 音量変更（設定画面: マスター/タイプ別）: `Client/Assets/Scripts/Client/Feature/Window/SoundVolume/SoundVolumeWindow.cs`（`GetSoundParam(type).volume` 変更 → `UpdateSoundParam(type)` → 対象 `SoundElement` の `UpdateVolume()`）
- 再生可否チェック（音量0なら演出ごとスキップ）: `Client/Assets/Scripts/Client/Battle/Core/Manager/BattleSoundManager.cs` の `IsEnable`

## 注意点・罠

- **初期化必須**: `SoundManagement.CreateInstance()` → `Initialize(defaultSoundParam)`。Clientでは `InitializeObject.manager.cs` が起動時に実施済みなので通常意識不要。
- **R3ベース**（UniRxではない）。通知系の購読は `using R3;` が必要。
- **`RegisterSoundType` 済は `Bgm` と `Se` のみ**（Client初期化時点）。`GetVolume(soundType)` と `SoundElement.UpdateVolume()` は `GetSoundParam` の null チェックをしていないため、**未登録タイプ（Voice/Ambience/Jingle等）の音を鳴らすと NRE**。新タイプを使うなら先に `RegisterSoundType` すること。
- **同時再生上限**: 初期値 Bgm 2 / Ambience 4 / Jingle 4 / Voice 16 / Se 32。上限到達時は `Number` が小さい（古い）ものから停止。`SoundType.Master` は上限テーブルに無く AudioSource が0本のため再生不可（音量カテゴリ用）。
- **同一フレームの同一 (SoundType, AudioClip) は再生されない**（既存要素か null が返る）。連打SEの多重防止は基盤側で担保済み。
- `SoundParam.cancelIfPlaying = true` のタイプは、同クリップ再生中なら新規再生せず既存 `SoundElement` を返す。
- **音を鳴らす実装は `SoundPlayer` 経由を原則とする**。`SoundManagement.Play` 直呼びはアセットロード・キャッシュ・Introloop判別・BgmManager登録を全てバイパスしてしまう。
- **`CrossFade(inElement, outElement, duration)` は equal-power カーブ**で in をフェードイン + out をフェードアウトする（in が null なら out の `FadeOut` のみ）。`SoundPlayer.Bgm` は同一BGM再生中なら既存要素を返し、BGM切替時は前BGMと自動クロスフェード（1秒）する。
- **SEは `AudioClip` 限定**（`SoundPlayer.Se` 内で `as AudioClip`）。`IntroloopAudio`（イントロ付きループ）はBGM専用。
- `IntroloopSoundElement` は `Source` / `GetPlayback` 相当を持たない（`Source == null`）。`GetAllSounds()` の要素の `Source` を触る時は null / 型チェックすること（例: `SoundPlayer.PlayBgmCore` の `is IntroloopSoundElement` 分岐）。
- 音量の永続化はClient側 `PlayData`（LocalData）の責務。`SoundManagement` は保存しない（設定画面 `SoundVolumeWindow` が Close 時に Save）。
- 戦闘SEは `BattleSoundManager` 経由（SyncMode中スキップ等の戦闘都合を吸収）。※2026-07時点、内部の `SoundPlayer.Se(seId)` 呼び出しは「サウンド準備完了まで」コメントアウト中。
- **`CriWare/` 配下と `#if ENABLE_CRIWARE_ADX(_LE)` ブロックは本プロジェクトではコンパイルされない**。Client側にも同シンボルの残存コードがあるが（例: `SceneManager.PlayBgm` の `SoundPlayer.Bgm(cueSheet, cue)` / `Sounds.GetCueInfo`）、現行 `SoundPlayer` / `Sounds` に対応メンバーが無いため、シンボルを定義するだけでは復活しない（CRI移行時は要改修）。
- Editor専用: `Sound/CriWare/Editor/SoundScriptGenerator` はCRI有効時のみ（`CriAssetUpdater` から呼ばれ `Sounds.Cue` を自動生成する仕組み。現在は未使用）。

## 関連

- [CriWare](CriWare.md) — CRI版実装のライブラリ初期化・アセット管理（本プロジェクトでは未使用）
- [ExternalAsset](ExternalAsset.md) — 配信サウンド（`SoundInfo.External = true`）のロード実体
- [LocalData](LocalData.md) — 音量・BGM ON/OFF設定の永続化（`PlayData`）
- [Master](Master.md) — `BgmMaster` / `SeMaster`（配信サウンドのID→ResourcePath解決）
- [Extensions/Core](../Extensions/Core.md) — `Singleton<T>`（`CreateInstance` / `Instance` / `Exists`）
