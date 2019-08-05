
using UnityEngine;
using Extensions;

namespace Modules.Devkit
{
    // ※ 色コードは下記参照.
    // http://arikalog.hateblo.jp/entry/2013/12/02/215401

    public static class UnityConsole
    {
        private static bool? isDevlopmentBuild = null;

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

            var color = new Color(0, 255, 255);

            Event("Info", color, message);
        }

        public static void Info(string format, params object[] args)
        {
            if (!isDebugBuild) { return; }

            Info(string.Format(format, args));
        }

        public static void Event(string eventName, Color color, string message)
        {
            if (!isDebugBuild) { return; }

            var originStackTraceLogType = Application.GetStackTraceLogType(LogType.Log);

            Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);

            var colorCode = color.ColorToHex(false);

            Debug.Log(string.Format("<color=#{0}><b>[{1}]</b></color> {2}", colorCode, eventName, message));

            Application.SetStackTraceLogType(LogType.Log, originStackTraceLogType);
        }

        public static void Event(string eventName, Color color, string format, params object[] args)
        {
            if (!isDebugBuild) { return; }

            Event(eventName, color, string.Format(format, args));
        }
    }
}
