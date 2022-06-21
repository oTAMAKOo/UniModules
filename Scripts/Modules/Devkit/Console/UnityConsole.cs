
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Threading;
using Extensions;

namespace Modules.Devkit.Console
{
    // ※ 色コードは下記参照.
    // http://arikalog.hateblo.jp/entry/2013/12/02/215401

    public static class UnityConsole
    {
        //----- params -----

        public static class InfoEvent
        {
            public static readonly string ConsoleEventName = "Info";
            public static readonly Color ConsoleEventColor = new Color(0f, 1f, 1f);
        }

        //----- field -----

        private static SynchronizationContext unitySynchronizationContext = null;

        private static int mainThreadId = 0;

        private static bool? isDevelopmentBuild = null;

        private static bool enableNextLogStackTrace = false;

        //----- property -----

        /// <summary> 無効化するイベント名 </summary>
        public static HashSet<string> DisableEventNames { get; private set; }

        //----- method -----

        static UnityConsole()
        {
            enableNextLogStackTrace = false;

            DisableEventNames = new HashSet<string>();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void OnAfterAssembliesLoaded()
        {
            unitySynchronizationContext = SynchronizationContext.Current;

            mainThreadId = Thread.CurrentThread.ManagedThreadId;

            isDevelopmentBuild = UnityEngine.Debug.isDebugBuild;
        }

        public static bool Enable
        {
            get
			{
				#if UNITY_EDITOR

				return true;

				#else

				return isDevelopmentBuild.HasValue && isDevelopmentBuild.Value;

				#endif
			}
        }

        public static void Info(string message)
        {
            if (!Enable) { return; }
            
            Event(InfoEvent.ConsoleEventName, InfoEvent.ConsoleEventColor, message);
        }

        public static void Info(string format, params object[] args)
        {
            if (!Enable) { return; }

            Info(string.Format(format, args));
        }

        public static void Event(string eventName, Color color, string message, LogType logType = LogType.Log)
        {
            if (!Enable) { return; }

            if (DisableEventNames.Contains(eventName)) { return; }

            #if UNITY_EDITOR

            if (!CheckEnable(eventName)) { return; }

            #endif
            
            var colorCode = color.ColorToHex(false);

            var text = string.Format("<color=#{0}><b>[{1}]</b></color> {2}", colorCode, eventName, message);

            if (enableNextLogStackTrace)
            {
                LogOutput(logType, text);
            }
            else
            {
                Action output = () =>
                {
                    using (new DisableStackTraceScope())
                    {
                        LogOutput(logType, text);
                    }
                };

                if (mainThreadId != Thread.CurrentThread.ManagedThreadId)
                {
                    unitySynchronizationContext.Post(_ => output.Invoke(), null);
                }
                else
                {
                    output.Invoke();
                }
            }

            enableNextLogStackTrace = false;
        }

        /// <summary> 次に出力するログのスタックトレースを有効化 </summary>
        public static void EnableNextLogStackTrace()
        {
            enableNextLogStackTrace = true;
        }

        /// <summary> ログタイプ別出力 </summary>
        private static void LogOutput(LogType logType, string text)
        {
            switch (logType)
            {
                case LogType.Log:
                    Debug.Log(text);
                    break;
                case LogType.Warning:
                    Debug.LogWarning(text);
                    break;
                case LogType.Error:
                    Debug.LogError(text);
                    break;
                default:
                    throw new NotSupportedException(string.Format("{0} not support. \n\n{1}", logType, text));
            }
        }

        #if UNITY_EDITOR

        private static bool CheckEnable(string eventName)
        {
            var unityConsoleManager = UnityConsoleManager.Instance;

            if (!unityConsoleManager.IsEnable()){ return false; }

            return unityConsoleManager.IsEventEnable(eventName);
        }

        #endif
    }
}
