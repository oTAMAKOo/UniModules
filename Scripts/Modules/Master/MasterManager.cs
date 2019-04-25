
using UnityEngine;
using System.Collections.Generic;
using UniRx;
using Extensions;

namespace Modules.Master
{
    public partial class MasterManager : Singleton<MasterManager>
    {
        //----- params -----

        private const string MasterFileExtension = ".master";

        //----- field -----

        //----- property -----

        public List<IMaster> All { get; private set; }

        public string InstallDirectory { get; private set; }

        //----- method -----

        protected MasterManager()
        {
            All = new List<IMaster>();

            InstallDirectory = string.Format("{0}/Master", Application.temporaryCachePath);
        }

        public string GetInstallPath<T>() where T : IMaster
        {
            return PathUtility.Combine(InstallDirectory, GetMasterFileName<T>());
        }

        public string GetMasterFileName<T>() where T : IMaster
        {
            return typeof(T).Name + MasterFileExtension;
        }
    }
}
