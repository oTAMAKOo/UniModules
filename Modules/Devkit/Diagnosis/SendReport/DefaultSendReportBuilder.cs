
using UnityEngine;
using System;
using System.Globalization;

namespace Modules.Devkit.Diagnosis.SendReport
{
    public sealed class DefaultSendReportBuilder : ISendReportBuilder
    {
        public void Build(string screenShotData, string logData)
        {
            var sendReportManager = SendReportManager.Instance;

            const uint mega = 1024 * 1024;

            sendReportManager.AddReportContent("Time", DateTime.Now.ToString(CultureInfo.InvariantCulture));
            sendReportManager.AddReportContent("OperatingSystem", SystemInfo.operatingSystem);
            sendReportManager.AddReportContent("DeviceModel", SystemInfo.deviceModel);
            sendReportManager.AddReportContent("SystemMemorySize", (SystemInfo.systemMemorySize * mega).ToString());
            sendReportManager.AddReportContent("UseMemorySize", GC.GetTotalMemory(false).ToString());
            sendReportManager.AddReportContent("Log", logData);
            sendReportManager.AddReportContent("ScreenShotBase64", screenShotData);
        }
    }
}
