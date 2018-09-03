
using System;

namespace Extensions
{
    public abstract class Singleton<T> : LifetimeDisposable where T : Singleton<T>
    {
        //----- params -----

        //----- field -----

        protected static T instance = null;

        //----- property -----

        public static T Instance { get { return instance ?? (instance = CreateInstance()); } }

        public static bool Exists { get { return instance != null; } }

        //----- method -----

        protected Singleton() { }

        ~Singleton()
        {
            OnRelease();
        }

        public static T CreateInstance()
        {
            if (instance != null) { return instance; }

            var type = typeof(T);
            instance = Activator.CreateInstance(type, true) as T;

            instance.OnCreate();

            return instance;
        }

        public static void ReleaseInstance()
        {
            instance = null;
        }

        protected virtual void OnCreate() { }

        protected virtual void OnRelease() { }
    }
}
