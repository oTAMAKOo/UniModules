
using System.Collections.Generic;
using Extensions;

namespace Modules.Master
{
    public interface ICacheData
    {
        void Clear();
    }

    public sealed class CacheDataManager : Singleton<CacheDataManager>
    {
        //----- params -----

        //----- field -----

        private List<ICacheData> manageItems = null;

        //----- property -----

        //----- method -----

        private CacheDataManager()
        {
            manageItems = new List<ICacheData>();
        }

        public void Add(ICacheData target)
        {
            if (!manageItems.Contains(target))
            {
                manageItems.Add(target);
            }
        }

        public void Remove(ICacheData target)
        {
            if (manageItems.Contains(target))
            {
                manageItems.Remove(target);
            }
        }

        public void Clear()
        {
            foreach (var item in manageItems)
            {
                item.Clear();
            }
        }
    }
}
