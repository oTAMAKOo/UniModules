
using System;
using UnityEngine;
using UnityEditor;
using UniRx;

namespace Extensions.Devkit
{
    public class SingletonEditorWindow<T> : EditorWindow where T : SingletonEditorWindow<T>
    {
        //----- params -----

        //----- field -----

        private static T instance = null;

        private Subject<Unit> onDestroy = null;

        private LifetimeDisposable lifetimeDisposable = new LifetimeDisposable();

        //----- property -----

        public CompositeDisposable Disposable { get { return lifetimeDisposable.Disposable; } }

        public static bool IsExist { get { return FindInstance() != null; } }

        //----- method -----

        public static T Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindInstance() ?? CreateInstance<T>();

                    if (instance != null)
                    {
                        instance.OnCreateInstance();
                    }
                }

                return instance;
            }
        }

        private static T FindInstance()
        {
            var windows = (T[])Resources.FindObjectsOfTypeAll(typeof(T));

            if (windows.Length == 0) { return null; }

            return windows[0];
        }

        void OnDestroy()
        {
            if (onDestroy != null)
            {
                onDestroy.OnNext(Unit.Default);
            }

            instance = null;
        }

        public IObservable<Unit> OnDestroyAsObservable()
        {
            return onDestroy ?? (onDestroy = new Subject<Unit>());
        }

        protected virtual void OnCreateInstance() { }
    }
}
