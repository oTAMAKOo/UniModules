
using UnityEngine;
using System.Collections.Generic;
using Extensions;

namespace Modules.Devkit
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

        private static bool? isDevlopmentBuild = null;

        //----- property -----

        public static HashSet<string> DisableEventNames { get; private set; }

        //----- method -----

        static UnityConsole()
        {
            DisableEventNames = new HashSet<string>();
        }

        private static bool isDebugBuild
        {
            get
            {
                if (!isDevlopmentBuild.HasValue)
                {
                    isDevlopmentBuild = UnityEngine.Debug.isDebugBuild;
                }

                return isDevlopmentBuild.Value;
            }
        }

        public static void Info(string message)
        {
            if (!isDebugBuild) { return; }
            
            Event(InfoEvent.ConsoleEventName, InfoEvent.ConsoleEventColor, message);
        }

        public static void Info(string format, params object[] args)
        {
            if (!isDebugBuild) { return; }

            Info(string.Format(format, args));
        }

        public static void Event(string eventName, Color color, string message)
        {
            if (!isDebugBuild) { return; }

            if (DisableEventNames.Contains(eventName)) { return; }

            using (new DisableStackTraceScope())
            {
                var colorCode = color.ColorToHex(false);

                Debug.Log(string.Format("<color=#{0}><b>[{1}]</b></color> {2}", colorCode, eventName, message));
            }            
        }

        public static void Event(string eventName, Color color, string format, params object[] args)
        {
            if (!isDebugBuild) { return; }

            if (DisableEventNames.Contains(eventName)) { return; }

            Event(eventName, color, string.Format(format, args));
        }
    }
}
