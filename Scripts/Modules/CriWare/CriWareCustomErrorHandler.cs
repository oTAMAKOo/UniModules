
using UnityEngine;
using System;
using System.Collections.Generic;
using CriWare;
using Extensions;

namespace Modules.CriWare
{
    public sealed class CriWareCustomErrorHandler : MonoBehaviour
    {
        //----- params -----

        //----- field -----

        private bool isDebugBuild = false;

        private Dictionary<LogType, bool> logOutput = null;

        [NonSerialized]
        private bool initialized = false;

        //----- property -----

        //----- method -----

        public void Initialize()
        {
            if (initialized){ return; }

            logOutput = new Dictionary<LogType, bool>();

            isDebugBuild = Debug.isDebugBuild;

            SetLogOutput(LogType.Log, isDebugBuild);
            SetLogOutput(LogType.Warning, isDebugBuild);
            SetLogOutput(LogType.Error, isDebugBuild);

            initialized = true;
        }

        void OnEnable()
        {
            CriErrorNotifier.OnCallbackThreadUnsafe += OnCallback;
        }

        void OnDisable()
        {
            CriErrorNotifier.OnCallbackThreadUnsafe -= OnCallback;
        }

        private void OnCallback(string message)
        {
            if (message.StartsWith("E"))
            {
                var output = logOutput.GetValueOrDefault(LogType.Error);

                if (output)
                {
                    Debug.LogError(CriWareErrorHandler.logPrefix + " Error:" + message);
                }
            }
            else if (message.StartsWith("W"))
            {
                var output = logOutput.GetValueOrDefault(LogType.Warning);

                if (output)
                {
                    Debug.LogWarning(CriWareErrorHandler.logPrefix + " Warning:" + message);
                }
            }
            else
            {
                var output = logOutput.GetValueOrDefault(LogType.Log);

                if (output)
                {
                    Debug.Log(CriWareErrorHandler.logPrefix + message);
                }
            }
        }

        public void SetLogOutput(LogType logType, bool output)
        {
            logOutput[logType] = output;
        }
    }
}