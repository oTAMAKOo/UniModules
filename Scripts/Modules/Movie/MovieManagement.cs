
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
    public sealed class MovieManagement : Singleton<MovieManagement>
    {
        //----- params -----

        //----- field -----

        private Texture2D movieTexture = null;

        private List<MovieElement> movieElements = new List<MovieElement>();

        private float audioVolume = 1f;

        private bool initialized = false;

        //----- property -----

        //----- method -----

        private MovieManagement() { }

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

        /// <summary>
        /// 画再生用のインスタンスを生成.
        /// ※ 頭出しなどを行う時はこの関数で生成したPlayerを使って頭出しを実装する.
        /// </summary>
        private MovieElement CreateElement<TMovieMaterial>(string moviePath, Graphic targetGraphic, Player.ShaderDispatchCallback shaderOverrideCallBack = null) 
            where TMovieMaterial : CriManaMovieMaterialBase
        {
            var movieController = UnityUtility.GetOrAddComponent<TMovieMaterial>(targetGraphic.gameObject);

            UnityUtility.SetActive(movieController, true);

            if (movieController is CriMovieForUI criMovieForUI)
            {
                criMovieForUI.target = targetGraphic;
                criMovieForUI.enabled = true;

                if (!criMovieForUI.Initialized)
                {
                    criMovieForUI.ManualInitialize();
                }
            }

            var moviePlayer = movieController.player;

            moviePlayer.SetFile(null, moviePath);
            moviePlayer.SetVolume(audioVolume);

            if (shaderOverrideCallBack != null)
            {
                moviePlayer.SetShaderDispatchCallback(shaderOverrideCallBack);
            }

            var movieElement = new MovieElement(moviePlayer, movieController, moviePath);

            movieElements.Add(movieElement);

            return movieElement;
        }

        #region Prepare

        /// <summary> 動画再生準備. </summary>
        public MovieElement Prepare(Movies.Mana type, Graphic targetGraphic, Player.ShaderDispatchCallback shaderOverrideCallBack = null)
        {
            return Prepare<CriMovieForUI>(type, targetGraphic, shaderOverrideCallBack);
        }

        /// <summary> 動画再生準備. </summary>
        public MovieElement Prepare<TMovieMaterial>(Movies.Mana type, Graphic targetGraphic, Player.ShaderDispatchCallback shaderOverrideCallBack = null) 
            where TMovieMaterial : CriManaMovieMaterialBase
        {
            var movieInfo = Movies.GetManaInfo(type);

            if (movieInfo == null){ return null; }

            return movieInfo != null ? Prepare<TMovieMaterial>(movieInfo, targetGraphic, shaderOverrideCallBack) : null;
        }

        /// <summary> 動画再生準備. </summary>
        public MovieElement Prepare(ManaInfo movieInfo, Graphic targetGraphic, Player.ShaderDispatchCallback shaderOverrideCallBack = null)
        {
            return Prepare<CriMovieForUI>(movieInfo, targetGraphic, shaderOverrideCallBack);
        }

        /// <summary> 動画再生準備. </summary>
        public MovieElement Prepare<TMovieMaterial>(ManaInfo movieInfo, Graphic targetGraphic, Player.ShaderDispatchCallback shaderOverrideCallBack = null)
            where TMovieMaterial : CriManaMovieMaterialBase
        {
            if (movieInfo == null){ return null; }

            var moviePath = Path.ChangeExtension(movieInfo.UsmPath, CriAssetDefinition.UsmExtension);

            return Prepare<TMovieMaterial>(moviePath, targetGraphic, shaderOverrideCallBack);
        }

        /// <summary> 動画再生準備. </summary>
        public MovieElement Prepare(string moviePath, Graphic targetGraphic, Player.ShaderDispatchCallback shaderOverrideCallBack = null)
        {
            return Prepare<CriMovieForUI>(moviePath, targetGraphic, shaderOverrideCallBack);
        }

        /// <summary> 動画再生準備. </summary>
        public MovieElement Prepare<TMovieMaterial>(string moviePath, Graphic targetGraphic, Player.ShaderDispatchCallback shaderOverrideCallBack = null)
            where TMovieMaterial : CriManaMovieMaterialBase
        {
            var element = CreateElement<TMovieMaterial>(moviePath, targetGraphic, shaderOverrideCallBack);

            PrepareElement(element).Forget();

            return element;
        }

        private async UniTask PrepareElement(MovieElement element)
        {
            if (element == null){ return; }

            if (element.Player == null){ return; }

            while (element.Player.status == Player.Status.StopProcessing)
            {
                if (!UnityUtility.IsActiveInHierarchy(element.CriManaMovieMaterial))
                {
                    element.CriManaMovieMaterial.PlayerManualUpdate();
                }

                await UniTask.NextFrame();
            }

            if (element.Player.status != Player.Status.Stop && element.Player.status != Player.Status.PlayEnd)
            {
                Debug.LogWarning($"Movie prepare failed.\nCurrent status is {element.Player.status}.");
            }

            element.Player.Prepare();
        }

        #endregion

        #region Play

        public MovieElement Play(Movies.Mana type, Graphic targetGraphic, bool loop = false, Player.ShaderDispatchCallback shaderOverrideCallBack = null)
        {
            return Play<CriMovieForUI>(type, targetGraphic, loop, shaderOverrideCallBack);
        }

        public MovieElement Play<TMovieMaterial>(Movies.Mana type, Graphic targetGraphic, bool loop = false, Player.ShaderDispatchCallback shaderOverrideCallBack = null)
            where TMovieMaterial : CriManaMovieMaterialBase
        {
            var info = Movies.GetManaInfo(type);

            return info != null ? Play<TMovieMaterial>(info, targetGraphic, loop, shaderOverrideCallBack) : null;
        }

        public MovieElement Play(ManaInfo movieInfo, Graphic targetGraphic, bool loop = false, Player.ShaderDispatchCallback shaderOverrideCallBack = null)
        {
            return Play<CriMovieForUI>(movieInfo, targetGraphic, loop, shaderOverrideCallBack);
        }

        public MovieElement Play<TMovieMaterial>(ManaInfo movieInfo, Graphic targetGraphic, bool loop = false, Player.ShaderDispatchCallback shaderOverrideCallBack = null)
            where TMovieMaterial : CriManaMovieMaterialBase
        {
            if (movieInfo == null){ return null; }

            var moviePath = Path.ChangeExtension(movieInfo.UsmPath, CriAssetDefinition.UsmExtension);

            return Play<TMovieMaterial>(moviePath, targetGraphic, loop, shaderOverrideCallBack);
        }

        public MovieElement Play(string moviePath, Graphic targetGraphic, bool loop = false, Player.ShaderDispatchCallback shaderOverrideCallBack = null)
        {
            var element = CreateElement<CriMovieForUI>(moviePath, targetGraphic, shaderOverrideCallBack);

            Play(element, loop);

            return element;
        }

        public MovieElement Play<TMovieMaterial>(string moviePath, Graphic targetGraphic, bool loop = false, Player.ShaderDispatchCallback shaderOverrideCallBack = null)
            where TMovieMaterial : CriManaMovieMaterialBase
        {
            var element = CreateElement<TMovieMaterial>(moviePath, targetGraphic, shaderOverrideCallBack);

            Play(element, loop);

            return element;
        }

        public void Play(MovieElement element, bool loop = false)
        {
            if (element == null || element.Player == null) { return; }

            element.Player.Loop(loop);

            element.Player.Start();
        }

        #endregion

        public void Pause(MovieElement element, bool pause)
        {
            if (element == null || element.Player == null) { return; }

            element.Player.Pause(pause);
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
        }

        private void UpdateElement()
        {
            var releaseElements = new List<MovieElement>();
            
            for (var i = 0; i < movieElements.Count; ++i)
            {
                var movieElement = movieElements[i];

                movieElement.Update();

                if (!movieElement.IsLoop)
                {
                    if(movieElement.Status.HasValue && movieElement.Status.Value == Player.Status.PlayEnd)
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
                    releaseElement.Player.Dispose();
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
    }
}

#endif
