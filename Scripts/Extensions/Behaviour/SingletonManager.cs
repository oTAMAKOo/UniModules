
using System.Collections.Generic;
using System.Linq;

namespace Extensions
{
    public static class SingletonManager
    {
        //----- params -----

        //----- field -----

        private static List<ISingleton> singletons = null;

        //----- property -----

        //----- method -----

        public static void Register(ISingleton instance)
        {
            if (singletons == null)
            {
                singletons = new List<ISingleton>();
            }

            singletons.Add(instance);
        }

        public static void Remove(ISingleton instance)
        {
            if (singletons == null) { return; }

            if (singletons.Contains(instance))
            {
                singletons.Remove(instance);
            }
        }

        public static void Refresh()
        {
            if (singletons == null) { return; }

            var targets = new List<ISingleton>(singletons);

            foreach (var target in targets)
            {
                target.Refresh();
            }
        }

        public static void ReleaseAll()
        {
            if (singletons == null) { return; }

            while (singletons.Any())
            {
                var target = singletons.FirstOrDefault();

                if (target == null){ break; }

                target.Release();
            }
        }
    }
}