# Sound

> **namespace**: `Modules.Sound`（Editor専用: `Modules.Sound.Editor` — CRI有効時のみ）
> **場所**: `Client/Assets/UniModules/Scripts/Modules/Sound/`
> **Client側使用**: 約24ファイル（2026-07時点）
> **依存**: UniTask / R3 / DOTween / Extensions（`Singleton<T>`） / Modules.Devkit.Console / 条件付き: CriWare SDK・Modules.CriWare（`ENABLE_CRIWARE_ADX(_LE)`、**本プロジェクトでは無効**）

## 概要

BGM・SE等のサウンド再生を一元管理する基盤。マスター音量 + `SoundType` 別音量、同時再生数制限、同一フレーム重複再生の抑制、フェードを提供する。
実装は2系統あり、`ENABLE_CRIWARE_ADX(_LE)` シンボルで切替わる。**本プロジェクトはシンボル未定義のため UnityAudio 版（`UnityAudio/SoundManagement`、AudioSourceプール方式）が有効**。`CriWare/` 配下（CriAtomExPlayerベース）はコンパイル対象外。
Claude が音を鳴らす時の入口は基盤ではなく **Client側ラッパー `SoundPlayer`（`Dominion.Client`、static）**。アセットロード（`AudioAssetManager`）・BGMクロスフェード・Introloop判別まで面倒を見る。

### 再生の流れ（本プロジェクト）

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

## 主要クラス

### 基盤（`Modules.Sound`）

| クラス | 種別 | 役割 |
|---|---|---|
| `SoundType` | enum | 音量カテゴリ。`Master` / `Bgm` / `Jingle` / `Voice` / `Se` / `Ambience` |
| `ISoundElement` | interface | 管理対象サウンドの共通IF（`Update()` のみ） |
| `SoundManagementCore<TInstance, TSoundParam, TSoundElement>` | abstract / `Singleton<TInstance>` | 共通基底。マスター音量・`SoundType`別 `SoundParam` 登録・再生/停止等の通知Subject・毎フレームの `Element.Update()` 駆動 |
| `SoundManagement` | Singleton（sealed） | **本プロジェクトで有効な実装**（`UnityAudio/`）。AudioSourceプール（DontDestroyOnLoadの `SoundManagement` オブジェクト配下）・同時再生上限・フェード |
| `SoundParam` | class | タイプ別再生設定。`volume`（1f） / `cancelIfPlaying`（false） |
| `SoundElement` | class | 再生1件分のハンドル（AudioSourceラッパー）。Client側で `IntroloopSoundElement` が派生 |

### CriWare版（`Sound/CriWare/` — `ENABLE_CRIWARE_ADX(_LE)` 未定義のため**本プロジェクトではコンパイル対象外**）

| クラス | 種別 | 役割 |
|---|---|---|
| `SoundManagementBase<TInstance, TSound>` | abstract（partial） | CriAtomExPlayerベースの管理。`Play(type, cue)` / `Play3D`（`ENABLE_CRIWARE_POS3D`） / `LipSyncPlay`（`ENABLE_CRIWARE_LIPSYNC`）、ACBの自動ロードと `ReleaseTime`（30秒）経過後の自動解放 |
| `ISoundManagement` | interface | Pause / Resume / Stop / SetVolume |
| `SoundElement`（CriWare版） | class（partial） | `CriAtomExPlayback` ハンドル。`CueInfo` / `SoundSheet` 保持 |
| `SoundSheet` | class | ACB（CueSheet）ラッパー。`AcbPath()` / `AwbPath()` |
| `CueInfo` | class | `CueSheet` / `Cue` / `FilePath` |
| `SoundScriptGenerator` | static class（**Editor専用**） | ACBファイル群から `Sounds.Cue` enum + `GetCueInfo` スクリプトを自動生成 |

### Client側ラッパー（`Dominion.Client` / `Dominion.Core.Sound` — `Client/Assets/Scripts/Client/Core/Sound/`）

基盤モジュールではないが、サウンド実装は必ずここを経由するため併記。

| クラス | 種別 | 役割 |
|---|---|---|
| `SoundPlayer` | static class | **音を鳴らす時の入口**。`Bgm` / `Se` / `StopBgm`。アセットロード・BGMクロスフェード（1秒）・Introloop判別込み |
| `SoundInfo` | class | 再生対象の指定。`ResourcePath`（`"Sound/"` 自動付与） + `External`（既定true=配信アセット） |
| `AudioAssetManager` | Singleton | `AudioClip` / `IntroloopAudio` のロードとキャッシュ。未使用60秒で自動解放（再生中は保持） |
| `BgmManager` | Singleton | 現在BGMの状態保持・5分間隔の変更抑制・BGM ON/OFF設定の永続化（`PlayData`） |
| `IntroloopSoundElement` | class（`SoundElement` 派生） | E7.Introloop によるイントロ付きループBGM。`Source` は null |
| `BattleSoundManager` | Singleton | 戦闘SE。`IsEnable`（音量0チェック）・`PlaySe`（複数candidateからランダム）・`DelaySe`（遅延連続再生）。SyncMode中は再生スキップ |

## 使い方(実例)

### 初期化（アプリ起動時に1回。通常は実施済みのため書く必要なし）

```csharp
// 引用元: Client/Assets/Scripts/Client/Core/Initialize/InitializeObject/InitializeObject.manager.cs
private void InitializeSoundManagement()
{
    var soundManagement = SoundManagement.CreateInstance();

    soundManagement.Initialize(new SoundParam() { volume = 1f });

    var playData = LocalDataManager.Get<PlayData>();

    soundManagement.Volume = playData.MasterVolume;

    var bgmVolume = playData.BgmVolume;
    var seVolume = playData.SeVolume;

    soundManagement.RegisterSoundType(SoundType.Bgm, new SoundParam() { volume = bgmVolume });
    soundManagement.RegisterSoundType(SoundType.Se, new SoundParam() { volume = seVolume });

    // アプリ終了時に全サウンド解放.
    ApplicationEventHandler.OnQuitAsObservable().Subscribe(_ => soundManagement.ReleaseAll());
}
```

### SE再生（同梱SE enum 指定・fire-and-forget）

```csharp
// 引用元: Client/Assets/Scripts/Client/Core/UI/UIButton.cs
SoundPlayer.Se(Sounds.Se.button_positive).Forget();
```

```csharp
// 引用元: Client/Assets/Scripts/Client/Core/Popup/WindowBase.cs（OnOpen/OnClose）
SoundPlayer.Se(OpenSe).Forget();    // OpenSe = Sounds.Se.window_open
```

### BGM再生（enum / マスターID）

```csharp
// 引用元: Client/Assets/Scripts/Client/Core/Scene/SceneBase.cs（SetBgm）
if (bgm.HasValue && bgm.Value != Sounds.Bgm.None)
{
    SoundPlayer.Bgm(bgm.Value).Forget();
}
else if (bgmId.HasValue)
{
    SoundPlayer.Bgm(bgmId.Value).Forget();   // BgmMaster の BgmId
}
```

```csharp
// 引用元: Client/Assets/Scripts/Client/Scene/Title/TitleView.cs
SoundPlayer.Bgm(Sounds.Bgm.bgm_title).Forget();
```

### 音量変更（設定画面: マスター/タイプ別）

```csharp
// 引用元: Client/Assets/Scripts/Client/Feature/Window/SoundVolume/SoundVolumeWindow.cs
private void UpdateMasterVolume(float volume)
{
    var soundManagement = SoundManagement.Instance;

    soundManagement.Volume = volume;

    var sounds = soundManagement.GetAllSounds();

    foreach (var sound in sounds)
    {
        if (sound == null){ continue; }

        sound.UpdateVolume();
    }
}

private void UpdateSoundVolume(SoundType soundType, float volume)
{
    var soundManagement = SoundManagement.Instance;

    var sounds = soundManagement.GetAllSounds().Where(x => x.Type == soundType);

    var soundParam = soundManagement.GetSoundParam(soundType);

    soundParam.volume = volume;

    soundManagement.UpdateSoundParam(soundType);

    foreach (var sound in sounds)
    {
        sound.UpdateVolume();
    }
}
```

※ 永続化は `PlayData.MasterVolume / BgmVolume / SeVolume`（LocalData）へ。`SoundManagement` 自体は保存しない。

### 再生可否チェック（音量0なら演出ごとスキップ）

```csharp
// 引用元: Client/Assets/Scripts/Client/Battle/Core/Manager/BattleSoundManager.cs
public bool IsEnable(SoundType type)
{
    var soundManagement = SoundManagement.Instance;

    if (soundManagement.Volume == 0f) { return false; }

    var soundParam = soundManagement.GetSoundParam(type);

    if (soundParam.volume == 0f) { return false; }

    return true;
}
```

## API(主要公開メンバー)

### SoundManagementCore（共通基底 — `SoundManagement` にも継承公開）

| メンバー | 説明 |
|---|---|
| `float Volume { get; set; }` | マスター音量（0〜1にClamp）。setで `OnUpdateMasterVolume` 通知 |
| `bool LogEnable { get; set; }` | 再生ログ出力（UnityConsole "Sound" イベント） |
| `RegisterSoundType(SoundType, TSoundParam)` | タイプ別再生設定を登録（未登録タイプはvolume取得系でNRE。罠参照） |
| `RemoveSoundType(SoundType)` / `UpdateSoundParam(SoundType)` | 設定削除 / 設定変更後の再適用通知 |
| `GetSoundParam(SoundType) : TSoundParam` | 設定取得（未登録は null） |
| `OnUpdateMasterVolume() : Observable<float>` | マスター音量変更通知 |
| `OnUpdateParamAsObservable() : Observable<SoundType>` | タイプ設定変更通知 |
| `OnPlay/OnStop/OnPause/OnResume/OnReleaseAsObservable() : Observable<TSoundElement>` | 再生/停止/中断/復帰/解放通知 |

### SoundManagement（UnityAudio版・本プロジェクト有効）

| メンバー | 説明 |
|---|---|
| `Initialize(SoundParam defaultSoundParam)` | 初期化（必須・1回）。AudioSourceプール生成、DontDestroyOnLoad |
| `Play(SoundType, AudioClip) : SoundElement` | 再生。上限超過時は古い順に停止、同一フレーム同一クリップは既存返却 |
| `Stop(SoundElement)` / `StopAll()` | 停止（管理リストからも除去） |
| `Pause(SoundElement)` / `PauseAll()` / `Resume(SoundElement)` / `ResumeAll()` | 中断 / 復帰 |
| `GetAllSounds(bool playing = true) : IReadOnlyList<SoundElement>` | 再生中（または管理中全件）取得 |
| `AddSoundElement(SoundElement)` | 外部生成した派生Element（Introloop等）を管理下に追加 |
| `SetVolume(SoundElement, float)` / `GetVolume(SoundType) : float` | 個別音量設定 / 実効音量（マスター×タイプ）取得 |
| `SetSoundLimit(SoundType, int)` | タイプ別同時再生上限（=AudioSource数）変更 |
| `ReleaseAll(bool force = false)` | 停止済み（force時は全件）を解放し、AudioSourceからクリップ参照を外す |
| `FadeIn(element, duration, volume = 1f) : UniTask` | DOTweenで音量フェードイン |
| `FadeOut(element, duration, volume = 0f, fadeEndStop = true) : UniTask` | フェードアウト（完了時Stop） |
| `CrossFade(inElement, outElement, duration) : UniTask` | クロスフェード（inをフェードイン+outをフェードアウト。inがnullならoutのFadeOutのみ） |

### SoundElement（UnityAudio版）

| メンバー | 説明 |
|---|---|
| `Number` / `Type` / `Source` / `Clip` | 通し番号（古さ判定） / SoundType / AudioSource / AudioClip |
| `IsPlaying` / `IsPause` / `FinishTime` | 状態（毎フレーム `Update()` で反映） |
| `Volume { get; set; }` | 個別音量（setで即 `UpdateVolume()`） |
| `Play() / Stop() / Pause() / UnPause()` | 直接操作（通常は `SoundManagement` 経由） |
| `UpdateVolume()` | マスター×タイプ×個別音量を `Source.volume` に反映（virtual、Introloopが上書き） |
| `OnFinishAsObservable() : Observable<Unit>` | 再生終了通知 |

### SoundPlayer（Client側入口・static）

| メンバー | 説明 |
|---|---|
| `Bgm(uint bgmId) : UniTask<SoundElement>` | `BgmMaster.GetRecordByBgmId` → ResourcePath 再生（配信アセット） |
| `Bgm(Sounds.Bgm) : UniTask<SoundElement>` | `BgmDefine`（同梱定義）から再生 |
| `Bgm(SoundInfo) : UniTask<SoundElement>` | 同一BGM再生中なら既存返却。前BGMと自動クロスフェード（1秒）、`BgmManager` へ登録 |
| `StopBgm()` | 現在のBGM停止 + `BgmManager` クリア |
| `Se(uint seId) : UniTask<SoundElement>` | `SeMaster.FindRecordBySeId` → 再生（レコード無しは null） |
| `Se(Sounds.Se) : UniTask<SoundElement>` | `SeDefine`（同梱定義）から再生 |
| `Se(SoundInfo) : UniTask<SoundElement>` | AudioClipロード→ `SoundManagement.Play(SoundType.Se, clip)` |

### AudioAssetManager / BgmManager（Client側）

| メンバー | 説明 |
|---|---|
| `AudioAssetManager.GetAudioAsset(SoundInfo) : UniTask<Object>` | AudioClip/IntroloopAudio をロード（External: ExternalAsset / 同梱: Resources）+キャッシュ |
| `AudioAssetManager.Clear()` | キャッシュ全解放（AssetBundle解放・Resources.UnloadAsset含む） |
| `BgmManager.CurrentInfo` / `CurrentElement` | 再生中BGMの `SoundInfo` / `SoundElement` |
| `BgmManager.GetPlayRequestBgmId(bool force = false) : uint?` | 選曲（チュートリアル固定・5分抑制・OFF設定除外・`SampleOne()` 抽選） |
| `BgmManager.Set(SoundInfo, SoundElement)` | 再生BGMの登録（`SoundPlayer.Bgm` が呼ぶ） |
| `BgmManager.SetDisableBgms(IEnumerable<uint>)` / `DisableBgms` | BGM OFF設定の保存（`PlayData.BgmStatus`）/ 取得 |
| `BgmManager.IsPlaying(uint bgmId)` / `FindBgmId(SoundInfo)` | 指定BGM再生中判定 / 逆引き |
| `BgmManager.ClearChangeTime()` / `OnBgmChangedAsObservable()` | 5分抑制のリセット / BGM変更通知 |

## 注意点・罠

- **初期化必須**: `SoundManagement.CreateInstance()` → `Initialize(defaultSoundParam)`。Clientでは `InitializeObject.manager.cs` が起動時に実施済みなので通常意識不要。
- **R3ベース**（UniRxではない）。通知系の購読は `using R3;` が必要。
- **`RegisterSoundType` 済は `Bgm` と `Se` のみ**（Client初期化時点）。`GetVolume(soundType)` と `SoundElement.UpdateVolume()` は `GetSoundParam` の null チェックをしていないため、**未登録タイプ（Voice/Ambience/Jingle等）の音を鳴らすと NRE**。新タイプを使うなら先に `RegisterSoundType` すること。
- **同時再生上限**: 初期値 Bgm 2 / Ambience 4 / Jingle 4 / Voice 16 / Se 32。上限到達時は `Number` が小さい（古い）ものから停止。`SoundType.Master` は上限テーブルに無く AudioSource が0本のため再生不可（音量カテゴリ用）。
- **同一フレームの同一 (SoundType, AudioClip) は再生されない**（既存要素か null が返る）。連打SEの多重防止は基盤側で担保済み。
- `SoundParam.cancelIfPlaying = true` のタイプは、同クリップ再生中なら新規再生せず既存 `SoundElement` を返す。
- `CrossFade` はかつて null 判定の逆転により「旧BGMフェードアウト+新BGM即時再生」として動作していたが、**2026-07 に判定を修正し真のクロスフェード（新BGMフェードイン+旧BGMフェードアウト）が有効化された**。BGM切替（`SoundPlayer.Bgm`）の聞こえ方がこの時点で変わっている点に注意。inElement に null を渡した場合は outElement の FadeOut のみ（NREも解消済み）。
- **音を鳴らす実装は `SoundPlayer` 経由を原則とする**。`SoundManagement.Play` 直呼びはアセットロード・キャッシュ・Introloop判別・BgmManager登録を全てバイパスしてしまう。
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
