
using System;

namespace Extensions
{
    public interface ISingleton
    {
        void Refresh();
        void Release();
    }

    public abstract class Singleton<T> : LifetimeDisposable, ISingleton where T : Singleton<T>
    {
        //----- params -----

        //----- field -----

        [NonSerialized]
        protected static T instance = null;

        //----- property -----

        public static T Instance { get { return instance ?? (instance = CreateInstance()); } }

        public static bool Exists { get { return instance != null; } }

        //----- method -----

        protected Singleton() { }

        public void Release()
        {
            SingletonManager.Remove(instance);

            OnRelease();

            instance = null;
        }

        public void Refresh()
        {
            OnRefresh();
        }

        public static T CreateInstance()
        {
            if (instance != null) { return instance; }

            var type = typeof(T);

            instance = Activator.CreateInstance(type, true) as T;

            SingletonManager.Register(instance);

            instance.OnCreate();

            return instance;
        }

        public static void ReleaseInstance()
        {
            if (instance != null)
            {
                instance.Release();
            }
        }

        protected virtual void OnCreate() { }

        protected virtual void OnRelease() { }

        protected virtual void OnRefresh() { }
    }
}
