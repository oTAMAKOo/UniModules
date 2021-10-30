﻿﻿﻿
using System;
using UniRx;

namespace Modules.Devkit.Log
{
    public static class DebugLog
    {
        //----- params -----

        //----- field -----

        private static Subject<string> onLogReceived = null;
        
        private static Subject<string> onWarningReceived = null;

        private static Subject<string> onErrorReceived = null;

        private static Subject<string> onAssertReceived = null;

        private static Subject<Exception> onExceptionReceived = null;

        //----- property -----

        //----- method -----

        public static void ReceiveLog(string message)
        {
            if (onLogReceived != null)
            {
                onLogReceived.OnNext(message);
            }
        }

        public static void ReceiveWarning(string message)
        {
            if (onWarningReceived != null)
            {
                onWarningReceived.OnNext(message);
            }
        }

        public static void ReceiveError(string message)
        {
            if (onErrorReceived != null)
            {
                onErrorReceived.OnNext(message);
            }
        }

        public static void ReceiveAssert(string message)
        {
            if (onAssertReceived != null)
            {
                onAssertReceived.OnNext(message);
            }
        }

        public static void ReceiveException(Exception exception)
        {
            if (onExceptionReceived != null)
            {
                onExceptionReceived.OnNext(exception);
            }
        }

        public static IObservable<string> OnLogReceivedAsObservable()
        {
            return onLogReceived ?? (onLogReceived = new Subject<string>());
        }

        public static IObservable<string> OnWarningReceivedAsObservable()
        {
            return onWarningReceived ?? (onWarningReceived = new Subject<string>());
        }

        public static IObservable<string> OnErrorReceivedAsObservable()
        {
            return onErrorReceived ?? (onErrorReceived = new Subject<string>());
        }

        public static IObservable<string> OnAssertReceivedAsObservable()
        {
            return onAssertReceived ?? (onAssertReceived = new Subject<string>());
        }

        public static IObservable<Exception> OnExceptionReceivedAsObservable()
        {
            return onExceptionReceived ?? (onExceptionReceived = new Subject<Exception>());
        }
    }
}
