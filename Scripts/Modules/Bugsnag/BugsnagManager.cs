
#if ENABLE_BUGSNAG

using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using BugsnagUnity;
using Extensions;

namespace Modules.Bugsnag
{
    public enum Section
    {
        [Label("app")]
        App,
        [Label("device")]
        Device,
        [Label("user")]
        User,
    }
    
    public abstract class BugsnagManager<TInstance> : Singleton<TInstance> where TInstance : BugsnagManager<TInstance>
    {
        //----- params -----

        //----- field -----

        private string apiKey = null;

        //----- property -----
        
        public virtual bool IsEnable 
        { 
            get
            {
                return !string.IsNullOrEmpty(apiKey) && !UnityUtility.isEditor;
            }
        }

        //----- method -----

        protected override void OnCreate()
        {
            apiKey = GetApiKey();

            if (IsEnable)
            {
                var config = SetupConfiguration();

                config.ApiKey = apiKey;

                BugsnagUnity.Bugsnag.Start(config);

                BugsnagUnity.Bugsnag.AddOnError(e => OnErrorCallback(e));

                OnAfterStart();
            }
        }

        public void AddGlobalMetadata(Section section, string key, object value)
        {
            AddGlobalMetadata((Enum)section, key, value);
        }

        public void AddGlobalMetadata(Enum section, string key, object value)
        {
            var sectionName = section.ToLabelName();

            if (string.IsNullOrEmpty(sectionName) || string.IsNullOrEmpty(key) || value == null)
            {
                Debug.LogError("Metadata is empty.");
                return;
            }
            
            BugsnagUnity.Bugsnag.AddMetadata(sectionName, key, value);
        }

        /// <summary> 情報通知 </summary>
        public void Info(string name, string message, Dictionary<string, object> extraData = null)
        {
            if (!Instance.IsEnable) { return; }
            
            var stackTrace = StackTraceUtility.ExtractStackTrace();
            
            BugsnagUnity.Bugsnag.Notify(name, message, stackTrace, report => SetExtraMetadata(report, extraData));
        }

        /// <summary> エラー通知 </summary>
        public void Notify(Exception e, Dictionary<string, object> extraData = null, Severity severity = Severity.Error)
        {
            if (!Instance.IsEnable) { return; }

            BugsnagUnity.Bugsnag.Notify(e, severity, report => SetExtraMetadata(report, extraData));
        }
        
        /// <summary> パン屑情報を設定 </summary>
        public void Breadcrumb(string message, Dictionary<string, object> extraData = null)
        {
            if (!Instance.IsEnable) { return; }
            
            BugsnagUnity.Bugsnag.LeaveBreadcrumb(message, extraData);
        }
        
        private bool SetExtraMetadata(IEvent report, Dictionary<string, object> extraData)
        {
            if (extraData != null && extraData.Any())
            {
                report.AddMetadata("Extra", extraData);
            }
            
            return true;
        }

        protected virtual Configuration SetupConfiguration()
        {
            return BugsnagSettingsObject.LoadConfiguration();
        }

        protected virtual void OnAfterStart()
        {

        }

        protected virtual bool OnErrorCallback(IEvent e)
        {
            return true;
        }

        protected abstract string GetApiKey();
    }

    public static class IEventExtensions
    {
        public static void AddMetadata(this IEvent e, Section section, string key, object value)
        {
            e.AddMetadata((Enum)section, key, value);
        }
        
        public static void AddMetadata(this IEvent e, Enum section, string key, object value)
        {
            var sectionName = section.ToLabelName();
            
            if (!string.IsNullOrEmpty(sectionName))
            {
                e.AddMetadata(sectionName, key, value);
            }
            else
            {
                throw new ArgumentException("section has not LabelName");
            }
        }
    }
}

#endif