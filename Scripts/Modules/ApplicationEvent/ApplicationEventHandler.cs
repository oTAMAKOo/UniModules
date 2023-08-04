﻿
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;

namespace Modules.ApplicationEvent
{
    public sealed class ApplicationEventHandler : SingletonMonoBehaviour<ApplicationEventHandler>
    {
        //----- params -----

        //----- field -----

        private static Subject<Unit> onSuspend = null;
        private static Subject<double> onResume = null;
        private static Subject<Unit> onLowMemory = null;
        private static Subject<Unit> onQuit = null;

        private DateTime? spendTime = null;

        //----- property -----

        //----- method -----

        protected override void Awake()
        {
            base.Awake();

            Application.lowMemory += OnLowMemory;
        }

        private void OnSuspend()
        {
            if (spendTime.HasValue) { return; }

            spendTime = DateTime.Now;

            if (onSuspend != null)
            {
                onSuspend.OnNext(Unit.Default);
            }
        }

        private void OnResume()
        {
            if (!spendTime.HasValue) { return; }

            if (onResume != null)
            {
                var time = DateTime.Now - spendTime.Value;
                onResume.OnNext(time.TotalSeconds);
            }

            spendTime = null;
        }

        private void OnLowMemory()
        {
            if (onLowMemory != null)
            {
                onLowMemory.OnNext(Unit.Default);
            }
        }

        /// <summary> サスペンド時のイベント </summary>
        public static IObservable<Unit> OnSuspendAsObservable()
        {
            return onSuspend ?? (onSuspend = new Subject<Unit>());
        }
        
        /// <summary> レジューム時のイベント (サスペンドしてからの秒数を返す) </summary>
        public static IObservable<double> OnResumeAsObservable()
        {
            return onResume ?? (onResume = new Subject<double>());
        }

        /// <summary> メモリが不足してきた時のイベント </summary>
        public static IObservable<Unit> OnLowMemoryAsObservable()
        {
            return onLowMemory ?? (onLowMemory = new Subject<Unit>());
        }

        /// <summary> アプリケーション終了時のイベント </summary>
        public static IObservable<Unit> OnQuitAsObservable()
        {
            return onQuit ?? (onQuit = new Subject<Unit>());
        }

        #region Unity Event

        void OnApplicationPause(bool isPause)
        {
            if (isPause)
            {
                OnSuspend();
            }
            else
            {
                OnResume();
            }
        }

        public void OnApplicationQuit()
        {
            if (onQuit != null)
            {
                onQuit.OnNext(Unit.Default);
            }
        }

        #endregion
    }
}
