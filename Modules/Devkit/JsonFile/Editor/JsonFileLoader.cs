
using UnityEngine;
using System;
using System.IO;
using Newtonsoft.Json;
using Extensions;

namespace Modules.Devkit.JsonFile
{
    [Serializable]
    public sealed class JsonFileLoader
    {
        //----- params -----

        //----- field -----
        
        [SerializeField]
        private string jsonFileRelativePath = null;

        //----- property -----

        //----- method -----

        public T Load<T>() where T : class
        {
            var filePath = UnityPathUtility.RelativePathToFullPath(jsonFileRelativePath);

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException(filePath);
            }

            var json = File.ReadAllText(filePath);

            var config = JsonConvert.DeserializeObject<T>(json);

            return config;
        }
    }
}