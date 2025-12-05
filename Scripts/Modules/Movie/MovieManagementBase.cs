
#if ENABLE_CRIWARE_SOFDEC
﻿﻿﻿
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Collections.Generic;
using CriWare;
using CriWare.CriMana;
using UniRx;
using Cysharp.Threading.Tasks;
using Extensions;
using Modules.CriWare;

namespace Modules.Movie
{
    public interface IMovieManagement
    {
        void Play(MovieElement element, bool loop = false);

        void Pause(MovieElement element, bool pause);

        void Stop(MovieElement element);
    }

    public abstract class MovieManagementBase<TInstance, TMovie> : Singleton<TInstance>, IMovieManagement
        where TInstance : MovieManagementBase<TInstance, TMovie>
    {
        //----- params -----

        private static readonly HashSet<Player.Status> PrepareManualUpdateStateTable = new HashSet<Player.Status>
        {
            /**< ヘッダ解析中 */
            Player.Status.Dechead,
            /**< バッファリング開始停止中 */
            Player.Status.WaitPrep,
            /**< 再生準備中 */
            Player.Status.Prep,
            /**< 停止処理中 */
            Player.Status.StopProcessing,
        };

        //----- field -----

        private Texture2D movieTexture = null;

        private List<MovieElement> movieElements = new List<MovieElement>();

        private float audioVolume = 1f;

        private bool initialized = false;

        //----- property -----

        //----- method -----

        protected MovieManagementBase() { }

        public void Initialize()
        {
            if (initialized) { return; }

            // 状態更新.
            Observable.EveryEndOfFrame().Subscribe(_ => UpdateElement()).AddTo(Disposable);

            if (movieTexture == null)
            {
                movieTexture = new Texture2D(64, 64, TextureFormat.Alpha8, false);
                movieTexture.name = "CriMana MovieTexture";
                var colors = new Color[64 * 64];

                for (var i = 0; i < colors.Length; i++)
                {
                    colors[i] = Color.clear;
                }

                movieTexture.SetPixels(colors);
                movieTexture.Apply();
            }

            initialized = true;
        }

        private MovieElement CreateMovieElement(CriManaMovieMaterialBase movieController, string moviePath)
        {
            movieController.enabled = true;

            if (movieController.player == null)
            {
                movieController.PlayerManualInitialize();
            }

            movieController.PlayerManualSetup();

            var moviePlayer = movieController.player;

            UnityUtility.SetActive(movieController, true);

            moviePlayer.SetFile(null, moviePath);
            moviePlayer.SetVolume(audioVolume);

            var movieElement = new MovieElement(this, moviePlayer, movieController, moviePath);

            movieElements.Add(movieElement);

            return movieElement;
        }

        private void SetShaderDispatchCallback(MovieElement movieElement, Player.ShaderDispatchCallback shaderOverrideCallBack)
        {
            if (shaderOverrideCallBack == null){ return; }
            
            movieElement.Player.SetShaderDispatchCallback(shaderOverrideCallBack);
        }

        #region Prepare

        /// <summary> 動画再生準備. </summary>
        public MovieElement Prepare(TMovie mana, CriManaMovieMaterialBase movieController, Player.ShaderDispatchCallback shaderOverrideCallBack = null) 
        {
            var movieInfo = GetManaInfo(mana);

            if (movieInfo == null){ return null; }

            return movieInfo != null ? Prepare(movieInfo, movieController, shaderOverrideCallBack) : null;
        }

        /// <summary> 動画再生準備. </summary>
        public MovieElement Prepare(ManaInfo movieInfo, CriManaMovieMaterialBase movieController, Player.ShaderDispatchCallback shaderOverrideCallBack = null)
        {
            if (movieInfo == null){ return null; }

            var moviePath = Path.ChangeExtension(movieInfo.UsmPath, CriAssetDefinition.UsmExtension);

            return Prepare(moviePath, movieController, shaderOverrideCallBack);
        }

        /// <summary> 動画再生準備. </summary>
        public MovieElement Prepare(string moviePath, CriManaMovieMaterialBase movieController, Player.ShaderDispatchCallback shaderOverrideCallBack = null)
        {
            UpdateElement();

            var element = CreateMovieElement(movieController, moviePath);

            SetShaderDispatchCallback(element, shaderOverrideCallBack);

            PrepareElement(element).Forget();

            return element;
        }

        private async UniTask PrepareElement(MovieElement element)
        {
            if (element == null){ return; }

            if (element.Player == null){ return; }

            // 非アクティブ時にUpdate処理を実行.
            while (true)
            {
                if (element == null){ break; }

                if (element.Player == null){ break; }
            
                if (element.Player.status != Player.Status.StopProcessing){ break; }

                if (!UnityUtility.IsActiveInHierarchy(element.CriManaMovieMaterial))
                {
                    element.CriManaMovieMaterial.PlayerManualUpdate();
                }

                await UniTask.NextFrame();
            }

            // 再生準備開始.
            if (element != null && element.Player != null)
            {
                element.Player.Prepare();
            }

            // 非アクティブ時にUpdate処理を実行.
            while (true)
            {
                if (element == null){ break; }

                if (element.Player == null){ break; }

                if(!PrepareManualUpdateStateTable.Contains(element.Player.status)){ break; }

                if (!UnityUtility.IsActiveInHierarchy(element.CriManaMovieMaterial))
                {
                    element.CriManaMovieMaterial.PlayerManualUpdate();
                }

                await UniTask.NextFrame();
            }
        }

        #endregion

        #region Play

        public MovieElement Play(TMovie mana, CriManaMovieMaterialBase movieController, bool loop = false, Player.ShaderDispatchCallback shaderOverrideCallBack = null)
        {
            var info = GetManaInfo(mana);

            return info != null ? Play(info, movieController, loop, shaderOverrideCallBack) : null;
        }

        public MovieElement Play(ManaInfo movieInfo, CriManaMovieMaterialBase movieController, bool loop = false, Player.ShaderDispatchCallback shaderOverrideCallBack = null)
        {
            if (movieInfo == null){ return null; }

            var moviePath = Path.ChangeExtension(movieInfo.UsmPath, CriAssetDefinition.UsmExtension);

            return Play(moviePath, movieController, loop, shaderOverrideCallBack);
        }

        public MovieElement Play(string moviePath, CriManaMovieMaterialBase movieController, bool loop = false, Player.ShaderDispatchCallback shaderOverrideCallBack = null)
        {
            var element = CreateMovieElement(movieController, moviePath);

            SetShaderDispatchCallback(element, shaderOverrideCallBack);

            Play(element, loop);

            return element;
        }

        public void Play(MovieElement element, bool loop = false)
        {
            if (element == null || element.Player == null) { return; }

            element.Player.Loop(loop);

            element.Player.Start();

            element.SetStatus(MovieElement.Status.Play);
        }

        #endregion

        public void Pause(MovieElement element, bool pause)
        {
            if (element == null || element.Player == null) { return; }

            element.Player.Pause(pause);

            element.SetStatus(MovieElement.Status.Pause);
        }

        public void SetAudioVolume(float volume)
        {
            audioVolume = volume;

            foreach (var movieElement in movieElements)
            {
                if (movieElement == null || movieElement.Player == null){ continue; }

                movieElement.Player.SetVolume(audioVolume);
            }
        }

        public void Stop(MovieElement element)
        {
            if (element == null || element.Player == null) { return; }

            element.Player.Stop();

            element.SetStatus(MovieElement.Status.Stop);
        }

        private void UpdateElement()
        {
            var releaseElements = new List<MovieElement>();
            
            for (var i = 0; i < movieElements.Count; ++i)
            {
                var movieElement = movieElements[i];

                movieElement.Update();

                if (UnityUtility.IsNull(movieElement.CriManaMovieMaterial))
                {
                    releaseElements.Add(movieElement);
                }
                else if (!movieElement.IsLoop)
                {
                    if (movieElement.PlayerStatus is Player.Status.PlayEnd)
                    {
                        releaseElements.Add(movieElement);
                    }
                }
            }

            for (var i = 0; i < releaseElements.Count; i++)
            {
                var releaseElement = releaseElements[i];

                if (releaseElement.Player != null)
                {
                    releaseElement.ReleasePlayer();
                }

                movieElements.Remove(releaseElement);
            }
        }

        public void ReleaseAll()
        {
            foreach (var movieElement in movieElements)
            {
                if (movieElement == null) { continue; }

                if (movieElement.Player != null)
                {
                    movieElement.Player.Dispose();
                }
            }

            movieElements.Clear();
        }

        protected abstract ManaInfo GetManaInfo(TMovie mana);
    }
}

#endif