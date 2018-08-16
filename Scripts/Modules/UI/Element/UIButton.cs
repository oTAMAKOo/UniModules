
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;

namespace Modules.UI.Element
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(Button))]
    public abstract class UIButton : UIElement<Button>
    {
        //----- params -----

        //----- field -----

        //----- property -----

        public Button Button { get { return component; } }

        //----- method -----

        public override void Modify()
        {
            // Tabでフォーカスを移動するのを防ぐ.
            var navigation = Button.navigation;
            navigation.mode = Navigation.Mode.None;
            Button.navigation = navigation;
        }

        #region Button Action Extension

        public void SetLongPressDuration(float duration)
        {
            component.SetLongPressDuration(duration);
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