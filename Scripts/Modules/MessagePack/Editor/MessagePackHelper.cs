
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using UniRx;
using Extensions;

namespace Modules.MessagePack
{
    public static class MessagePackHelper
    {
        public static string GetDotNetPath()
        {
            var result = "dotnet";

            #if UNITY_EDITOR_WIN

            // 環境変数.
            var variable = Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.Process);

            if (variable != null)
            {
                foreach (var item in variable.Split(';'))
                {
                    var path = PathUtility.Combine(item, "dotnet.exe");

                    if (!File.Exists(path)){ continue; }

                    result = path;

                    break;
                }
            }
            
            #endif

            #if UNITY_EDITOR_OSX

            var mpcPathCandidate = new string[]
            {
                "/usr/local/bin",
            };

            foreach (var item in mpcPathCandidate)
            {
                var path = PathUtility.Combine(item, "dotnet");

                if (!File.Exists(path)){ continue; }

                result = path;

                break;
            }

            #endif

            return result;
        }
    }
}