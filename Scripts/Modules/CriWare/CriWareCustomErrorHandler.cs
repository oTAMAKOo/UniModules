
using UnityEngine;
using System;
using System.Collections.Generic;
using Extensions;

#if ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_ADX_LE || ENABLE_CRIWARE_SOFDEC

using CriWare;

#endif

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
            #if ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_ADX_LE || ENABLE_CRIWARE_SOFDEC

            CriErrorNotifier.OnCallbackThreadUnsafe += OnCallback;

            #endif
        }

        void OnDisable()
        {
            #if ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_ADX_LE || ENABLE_CRIWARE_SOFDEC
            
            CriErrorNotifier.OnCallbackThreadUnsafe -= OnCallback;

            #endif
        }

        private void OnCallback(string message)
        {
            if (message.StartsWith("E"))
            {
                var output = logOutput.GetValueOrDefault(LogType.Error);
                
                if (output)
                {
                    #if ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_ADX_LE || ENABLE_CRIWARE_SOFDEC

                    Debug.LogError(CriWareErrorHandler.logPrefix + " Error:" + message);

                    #endif
                }
            }
            else if (message.StartsWith("W"))
            {
                var output = logOutput.GetValueOrDefault(LogType.Warning);

                if (output)
                {
                    #if ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_ADX_LE || ENABLE_CRIWARE_SOFDEC
                    
                    Debug.LogWarning(CriWareErrorHandler.logPrefix + " Warning:" + message);

                    #endif
                }
            }
            else
            {
                var output = logOutput.GetValueOrDefault(LogType.Log);

                if (output)
                {
                    #if ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_ADX_LE || ENABLE_CRIWARE_SOFDEC

                    Debug.Log(CriWareErrorHandler.logPrefix + message);

                    #endif
                }
            }
        }

        public void SetLogOutput(LogType logType, bool output)
        {
            logOutput[logType] = output;
        }
    }
}