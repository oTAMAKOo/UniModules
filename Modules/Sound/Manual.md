# Sound

BGM や SE の再生を一元管理する機能。
種類別の音量、同時再生数の制限、停止・中断・フェードができる。
Unity 標準の音声を使う版と CRI ADX を使う版があり、シンボルで切り替わる。

## 基本情報

> **namespace**: `Modules.Sound`（Editor専用: `Modules.Sound.Editor` — CRI有効時のみ）
> **場所**: `Modules/Sound/`（共通: 直下 / UnityAudio 版: `UnityAudio/` / CRI 版: `CriWare/`）
> **依存**: UniTask / R3 / Extensions（`Singleton<T>`） / UnityAudio 版のみ: DOTween・Modules.Devkit.Console / CRI 版のみ: CriWare SDK・Modules.CriWare（`ENABLE_CRIWARE_ADX` または `ENABLE_CRIWARE_ADX_LE` 定義時）

## 概要

BGM・SE等のサウンド再生を一元管理する基盤。マスター音量 + `SoundType` 別音量、同時再生数制限、同一フレーム重複再生の抑制、フェードを提供する。
実装は2系統あり、`ENABLE_CRIWARE_ADX` / `ENABLE_CRIWARE_ADX_LE` シンボルで切替わる。**どちらも未定義の時は UnityAudio 版（`UnityAudio/SoundManagement`、AudioSourceプール方式）が有効**。`CriWare/` 配下（CriAtomExPlayerベース）はどちらかのシンボル定義時のみコンパイル対象。以下の逆引き・使い方・注意点は、「CRI 版」と明記した箇所を除き UnityAudio 版を前提とする（`SoundManagement` / `FadeIn`・`FadeOut`・`CrossFade` / `SetSoundLimit` / 同時再生数制限は CRI 版には存在しない）。
サウンド再生は基盤の `SoundManagement.Play` を直接叩くのではなく、利用側でアセットロード（配信/同梱）や BGM クロスフェード、Introloop 判別まで面倒を見るラッパーを用意することを推奨。
主要クラス: `SoundType`（音量カテゴリ enum。Master / Bgm / Jingle / Voice / Se / Ambience）/ `SoundManagement`（Singleton・AudioSourceプール・同時再生上限・フェード。共通基底 `SoundManagementCore<TInstance, TSoundParam, TSoundElement>` がマスター音量・タイプ別 `SoundParam`・再生/停止等の通知を持つ）/ `SoundParam`（`volume` / `cancelIfPlaying`）/ `SoundElement`（再生1件分のハンドル）。

CRI 版の主要クラス: `SoundManagementBase<TInstance, TSound>`（abstract Singleton。利用側で継承し、`GetCueInfo(TSound cue)` を実装してキュー enum 等から `CueInfo` を解決する）/ `CueInfo`（`FilePath` / `CueSheet` / `Cue`）/ `SoundSheet`（ロード済み ACB の保持）/ `SoundElement`（sealed。`CriAtomExPlayer` と `CriAtomExPlayback` を持つ）/ `SoundScriptGenerator`（Editor。ACB からキュー一覧のスクリプトを生成）。

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
| BGM/SE別の音量変更 | `GetSoundParam(type).volume = x;` → `UpdateSoundParam(type)`（既存 `SoundElement` への反映は内部購読で自動） |
| 現在の実効音量取得 | `SoundManagement.Instance.GetVolume(SoundType.Se)`（マスター×タイプ） |
| 全サウンドの停止/中断/復帰 | `SoundManagement.Instance.StopAll()` / `PauseAll()` / `ResumeAll()` |
| 個別サウンドの停止・中断・復帰・音量 | `Play` が返す `SoundElement` を保持して `Stop(element)` / `Pause(element)` / `Resume(element)` / `SetVolume(element, v)` |
| フェードイン/アウト/クロスフェード | `await SoundManagement.Instance.FadeIn(element, duration, volume = 1f)` / `FadeOut(element, duration, volume = 0f, fadeEndStop = true)` / `CrossFade(in, out, duration)` |
| 再生中のサウンド列挙 | `SoundManagement.Instance.GetAllSounds()`（`playing: false` で管理中全件） |
| 再生終了を購読 | `element.OnFinishAsObservable()` |
| 再生/停止/中断/復帰/解放の通知購読 | `OnPlayAsObservable()` / `OnStopAsObservable()` / `OnPauseAsObservable()` / `OnResumeAsObservable()` / `OnReleaseAsObservable()` |
| マスター音量・タイプ別設定の変更通知購読 | `OnUpdateMasterVolume()`（`Observable<float>`） / `OnUpdateParamAsObservable()`（`Observable<SoundType>`） |
| SoundType毎の同時再生上限変更 | `SoundManagement.Instance.SetSoundLimit(type, limit)`（初期値: Bgm 2 / Ambience 4 / Jingle 4 / Voice 16 / Se 32） |
| 同時再生中の同一SEを弾く | `RegisterSoundType(type, new SoundParam { cancelIfPlaying = true })` |
| 再生が終わった要素の管理を外す | `ReleaseAll()`（`force: true` で再生中も含め全件。`OnReleaseAsObservable` が流れる） |
| 再生ログをコンソールへ出す | `LogEnable = true`（`UnityConsole.Enable` が有効な時だけ出力） |
| （CRI 版）キューを鳴らす | 派生 Manager の `Play(SoundType, TSound cue, float? volume = null)`（同梱）/ `Play(SoundType, CueInfo info, float? volume = null)`（外部アセット） |
| （CRI 版）停止・中断・復帰・音量 | `element.Stop(ignoresReleaseTime)` / `element.Pause()` / `element.Resume(resumeMode)` / `element.SetVolume(v)`（Manager の同名メソッドへ委譲） |
| （CRI 版）サウンドに埋め込まれたイベントを購読 | `element.OnSoundEventAsObservable()` |
| （CRI 版）3D 定位で鳴らす | `Play3D(type, cue または info, position, volume)`（`ENABLE_CRIWARE_POS3D` 定義時のみ）。位置更新は `element.SetPosition(position)` |
| （CRI 版）リップシンク用に専用プレイヤーで鳴らす | `LipSyncPlay(type, cue または info, volume)`（`ENABLE_CRIWARE_LIPSYNC` 定義時のみ） |

## 使い方

定型パターン:

- **初期化（アプリ起動時に1回）**: `SoundManagement.CreateInstance()` → `Initialize(SoundParam)` → 使用する `SoundType` を `RegisterSoundType` で登録（例: `Bgm` / `Se`）。マスター音量は永続化した値で復元する
- **音量変更（設定画面のマスター/タイプ別）**: `GetSoundParam(type).volume` を書き換え → `UpdateSoundParam(type)`（既存 `SoundElement` への反映は内部購読で自動適用される）
- **再生可否チェック（音量0なら演出ごとスキップ）**: `GetVolume(soundType)` を見て 0 なら音の後続演出（時間待ち等）もスキップする形が定石

## 注意点・罠

- **初期化必須**: `SoundManagement.CreateInstance()` → `Initialize(defaultSoundParam)`
- **R3ベース**（UniRxではない）。通知系の購読は `using R3;` が必要
- **`RegisterSoundType` していないタイプは NRE**: `GetVolume(soundType)` と `SoundElement.UpdateVolume()`（`Volume` の設定・`SetVolume`・フェードで呼ばれる）は `GetSoundParam` の null チェックをしていないため、未登録タイプで呼ぶと NullReferenceException（`Play` 自体は未登録でも通り、タイプ別音量が適用されない）。使う `SoundType` はすべて事前に `RegisterSoundType` すること
- **マスター音量（`Volume`）を変えても、再生中の要素には自動で反映されない**: 変更は `OnUpdateMasterVolume()` で通知されるだけで、基盤内に購読は無い。再生中の音へ反映するには `UpdateSoundParam(type)` を呼ぶ（そのタイプの要素の音量が計算し直される）
- **同時再生上限**: 初期値 Bgm 2 / Ambience 4 / Jingle 4 / Voice 16 / Se 32。AudioSource は上限と同じ本数だけ用意され、**空きが無い時（上限数が再生中）は `Play` が null を返す**（古い音は自動では止まらない）。管理中の要素数が上限を超えている時（上限を下げた直後など）だけ、超過分を `Number` が小さい（古い）ものから停止する。`SoundType.Master` は上限テーブルに無く AudioSource が0本のため再生不可（音量カテゴリ用）
- **同一フレームの同一 (SoundType, AudioClip) は、その音が再生中なら新規再生せず既存要素を返す**（その音が既に停止している場合は同一フレームでも新規再生される）。完全な連打防止ではない
- `SoundParam.cancelIfPlaying = true` のタイプは、同クリップ再生中なら新規再生せず既存 `SoundElement` を返す
- **音を鳴らす実装はラッパー経由を原則とする**。`SoundManagement.Play` 直呼びはアセットロード・キャッシュ・Introloop判別・BGM管理を全てバイパスしてしまう
- **`CrossFade(inElement, outElement, duration)` は equal-power カーブ**で in をフェードイン + out をフェードアウトする（in が null なら out の `FadeOut` のみ）。in の目標音量は、`Play` が要素に設定する値と同じ種類別の音量（未登録の種類は呼んだ時点の `inElement.Volume`）で、呼んだ時点の `inElement.Volume` は 0 から始め直す。BGM 切替時は前BGMと自動クロスフェードするラッパーを利用側で組むのが定石
- **SEは `AudioClip` 限定**（ラッパー側で `as AudioClip` する）。特殊なループフォーマット（Introloop 等）を扱う場合は利用側で BGM 専用の `SoundElement` 派生を用意する
- 利用側で `SoundElement` を派生させて実装した要素は、内部で AudioSource を使わない場合 `Source == null` になり得る。`GetAllSounds()` の要素の `Source` を触る時は null / 型チェックすること。基底の `Update()` と `SoundManagement.Pause(element)` は `Source` を null チェックせずに使うため、`Source` を持たない派生は `Update()` を override し、中断は `Pause(element)` を通さず自前で行う（派生要素は `AddSoundElement(element)` で管理対象へ登録する）
- **CRI 版の違い**:
  - 派生 Manager を利用側で用意する（`SoundManagementBase<TInstance, TSound>` を継承し `GetCueInfo` を実装）。初期化は同じく `CreateInstance()` → `Initialize(SoundParam)` → `RegisterSoundType`。未登録タイプの `Play` は NullReferenceException になる
  - 同一フレームに同じ (CueSheet, Cue) を再生すると null を返す（既存要素は返さない）
  - `cancelIfPlaying` は同じキューシート（`CueInfo.FilePath`）の音が再生中かで判定する（キュー単位ではない）
  - `StopAll()` / `Stop(element)` の引数 `ignoresReleaseTime` は ADX のリリース時間を無視して即停止するか。`ResumeAll` / `Resume` は `CriAtomEx.ResumeMode` を取る。全体の中断・復帰は `OnPauseAllAsObservable()` / `OnResumeAllAsObservable()` でも通知される
  - 再生終了した要素は終了から `ReleaseTime`（既定 30 秒）経過後に管理から外れ、どの要素からも使われなくなった ACB は 5 秒周期のチェックで解放される。即時に外すなら `ReleaseAll()`
  - `SoundElement` の中断状態は `IsPaused`（UnityAudio 版は `IsPause`）。`Volume` は読み取り専用で、変更は `SetVolume(v)`（0〜1 にクランプされ、マスター×タイプ別音量が掛かる）
- 音量の永続化は利用側（LocalData 等）の責務。`SoundManagement` は保存しない
- **`CriWare/` 配下（`#if ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_ADX_LE`）はシンボル未定義時はコンパイルされない**。逆に `UnityAudio/` 配下はどちらかを定義するとコンパイルされない。3D 定位（`SoundManagementBase.3d.cs` / `SoundElement.3d.cs`）は追加で `ENABLE_CRIWARE_POS3D`、リップシンク（`SoundManagementBase.lipsync.cs`）は `ENABLE_CRIWARE_LIPSYNC` が必要
- Editor専用: `Sound/CriWare/Editor/SoundScriptGenerator` はCRI有効時のみ（`CriAssetUpdater` から `Generate(scriptPath, scriptNamespace, assetFolderPath, rootFolderName)` が呼ばれ、`Sounds.cs`（`Sounds.Cue` enum と `Sounds.GetCueInfo(Cue)`）を自動生成する仕組み）

## 関連

- [CriWare](../CriWare/Manual.md) — CRI版実装のライブラリ初期化・アセット管理
- [ExternalAssets](../ExternalAssets/Manual.md) — 配信サウンドのロード実体
- [LocalData](../LocalData/Manual.md) — 音量・BGM ON/OFF設定の永続化に利用
- [Master](../Master/Manual.md) — 配信サウンドのID→ResourcePath解決をマスターで実装する場合の供給元（マスター定義は利用側）
- [Extensions/Core](../../Documents~/Extensions/Core.md) — `Singleton<T>`（`CreateInstance` / `Instance` / `Exists`）
