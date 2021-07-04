
#if ENABLE_CRIWARE_SOFDEC
﻿﻿﻿
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UniRx;
using CriMana;
using Extensions;
using Modules.CriWare;
using System.IO;

namespace Modules.MovieManagement
{
    public enum MovieAssetType
    {
        InternalResources,
        ExternalResources,
    }

    public sealed class MovieManagement : Singleton<MovieManagement>
    {
        //----- params -----

        //----- field -----

        private Texture2D movieTexture = null;

        private List<MovieElement> movieElements = new List<MovieElement>();

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

        #region InternalResources

        /// <summary>
        /// InternalResources内の動画再生.
        /// </summary>
        public static MovieElement Play(Movies.Mana type, Graphic targetGraphic, Player.ShaderDispatchCallback shaderOverrideCallBack = null)
        {
            var info = Movies.GetManaInfo(type);

            return info != null ? Play(info, targetGraphic, shaderOverrideCallBack) : null;
        }

        #endregion

        #region ExternalResources

        /// <summary>
        /// ExternalResources内や、直接指定での動画再生用のインスタンスを生成.
        /// ※ 頭出しなどを行う時はこの関数で生成したPlayerを使って頭出しを実装する.
        /// </summary>
        public static MovieElement CreateElement(string moviePath, Graphic targetGraphic, Player.ShaderDispatchCallback shaderOverrideCallBack = null)
        {
            var movieController = UnityUtility.GetOrAddComponent<CriManaMovieControllerForUI>(targetGraphic.gameObject);

            movieController.target = targetGraphic;

            var manaPlayer = movieController.player;

            manaPlayer.SetFile(null, moviePath);

            if (shaderOverrideCallBack != null)
            {
                manaPlayer.SetShaderDispatchCallback(shaderOverrideCallBack);
            }

            var movieElement = new MovieElement(manaPlayer, movieController, moviePath);

            Instance.movieElements.Add(movieElement);

            return movieElement;
        }
        
        /// <summary> ExternalResources内や、直接指定での動画再生. </summary>
        public static MovieElement Play(ManaInfo movieInfo, Graphic targetGraphic, Player.ShaderDispatchCallback shaderOverrideCallBack = null)
        {
            var moviePath = Path.ChangeExtension(movieInfo.UsmPath, CriAssetDefinition.UsmExtension);

            return Play(moviePath, targetGraphic, shaderOverrideCallBack);
        }

        /// <summary> 直接指定での動画再生. </summary>
        public static MovieElement Play(string moviePath, Graphic targetGraphic, Player.ShaderDispatchCallback shaderOverrideCallBack = null)
        {
            var movieElement = CreateElement(moviePath, targetGraphic, shaderOverrideCallBack);

            movieElement.Player.Start();

            return movieElement;
        }

        #endregion

        public static void Stop(MovieElement element)
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

                if(movieElement.Status.HasValue && movieElement.Status.Value == Player.Status.PlayEnd)
                {
                    releaseElements.Add(movieElement);                    
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

        public static void ReleaseAll()
        {
            var movieElements = Instance.movieElements;

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
