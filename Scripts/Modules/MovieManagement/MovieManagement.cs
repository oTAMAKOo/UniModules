
#if ENABLE_CRIWARE_SOFDEC
﻿﻿﻿
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Modules.CriWare;

namespace Modules.MovieManagement
{
    public enum MovieAssetType
    {
        InternalResources,
        ExternalResources,
    }

    public class MovieManagement : Singleton<MovieManagement>
    {
        //----- params -----

        //----- field -----

        private Texture2D movieTexture = null;

        private List<MovieElement> movieElements = new List<MovieElement>();

        private bool initialized = false;

        //----- property -----

        //----- method -----

        public void Initialize()
        {
            if (initialized) { return; }

            // サウンドの状態更新.
            Observable.EveryEndOfFrame().Subscribe(_ => UpdateElement());

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
        /// InternalResources内の動画再生.
        /// </summary>
        public MovieElement Play(Movies.Mana type, Graphic targetGraphic)
        {
            var info = Movies.GetManaInfo(type);

            return info != null ? Play(info, targetGraphic) : null;
        }

        /// <summary>
        /// ExternalResources内や、直接指定での動画再生.
        /// </summary>
        public MovieElement Play(ManaInfo movieInfo, Graphic targetGraphic)
        {
            return Play(movieInfo.UsmPath + CriAssetDefinition.UsmExtension, targetGraphic);
        }

        public MovieElement Play(string moviePath, Graphic targetGraphic)
        {
            var movieController = UnityUtility.GetOrAddComponent<CriManaMovieControllerForUI>(targetGraphic.gameObject);

            movieController.target = targetGraphic;

            var manaPlayer = movieController.player;

            manaPlayer.SetFile(null, moviePath);
            manaPlayer.Start();

            var movieElement = new MovieElement(manaPlayer, moviePath);

            movieElements.Add(movieElement);

            return movieElement;
        }

        public void Stop(MovieElement element)
        {
            element.GetPlayer().Stop();
        }

        private void UpdateElement()
        {
            // 呼ばれる頻度が多いのでforeachを使わない.
            for (var i = 0; i < movieElements.Count; ++i)
            {
                movieElements[i].Update();

                if(movieElements[i].Status.HasValue && movieElements[i].Status.Value == CriMana.Player.Status.PlayEnd)
                {
                    movieElements.RemoveAt(i);
                }
            }
        }

        public void ReleaseAll()
        {
            foreach (var movieElement in movieElements)
            {
                movieElement.GetPlayer().Stop();
            }

            movieElements.Clear();
        }
    }
}

#endif
