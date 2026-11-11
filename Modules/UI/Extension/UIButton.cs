
using UnityEngine;
using UnityEngine.UI;
using System;
using R3;
using Extensions;

namespace Modules.UI.Extension
{
    [ExecuteAlways]
    [RequireComponent(typeof(Button))]
    public abstract class UIButton : UIComponent<Button>
    {
        //----- params -----

        //----- field -----

        private bool initialize = false;

        //----- property -----

        public Button Button { get { return component; } }

        public bool interactable
        {
            get { return component.interactable; }
            set { component.interactable = value; }
        }

        //----- method -----

        void OnEnable()
        {
            Initialize();
        }

        public void Initialize()
        {
            if (initialize){ return; }

            OnInitialize();

            initialize = true;
        }

        protected virtual void OnInitialize()
        {
            // Tabでフォーカスを移動するのを防ぐ.
            var navigation = Button.navigation;

            navigation.mode = Navigation.Mode.None;

            Button.navigation = navigation;
        }

        #region Button Event Extension

        public Observable<Unit> OnClickAsObservable()
        {
            return component.OnClickAsObservable();
        }

        public Observable<Unit> OnPressAsObservable()
        {
            return component.OnPressAsObservable();
        }

        public Observable<float> OnReleaseAsObservable()
        {
            return component.OnReleaseAsObservable();
        }

        public Observable<Unit> OnLongPressAsObservable()
        {
            return component.OnLongPressAsObservable();
        }

        public Observable<float> OnLongPressReleaseAsObservable()
        {
            return component.OnLongPressReleaseAsObservable();
        }

        public Observable<Unit> OnCancelAsObservable()
        {
            return component.OnCancelAsObservable();
        }

        #endregion

        #region Button Action Extension

        public void SetLongPressDuration(float duration)
        {
            component.SetLongPressDuration(duration);
        }

        public IDisposable OnClick(Action action)
        {
            return OnClickAsObservable().Subscribe(_ => action()).AddTo(this);
        }

        public IDisposable OnPress(Action action)
        {
            return component.OnPressAsObservable().Subscribe(_ => action()).AddTo(this);
        }

        public IDisposable OnRelease(Action<float> action)
        {
            return component.OnReleaseAsObservable().Subscribe(x => action(x)).AddTo(this);
        }

        public IDisposable OnLongPress(Action action)
        {
            return component.OnLongPressAsObservable().Subscribe(_ => action()).AddTo(this);
        }

        public IDisposable OnLongPressRelease(Action<float> action)
        {
            return component.OnLongPressReleaseAsObservable().Subscribe(x => action(x)).AddTo(this);
        }

        public IDisposable OnCancel(Action action)
        {
            return component.OnCancelAsObservable().Subscribe(_ => action()).AddTo(this);
        }

        #endregion
    }
}
