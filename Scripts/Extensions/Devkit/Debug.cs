
#if !UNITY_EDITOR

using UnityEngine;
using Modules.Devkit.Log;

public static class Debug
{
    public static bool isDebugBuild
    {
        get { return debugBuild; }
    }

    private static bool enable
    {
        get { return debugBuild || batchMode; }
    }

    private static bool debugBuild = false;
    private static bool batchMode = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    private static void RuntimeInitializeOnLoadMethod()
    {
        debugBuild = UnityEngine.Debug.isDebugBuild;
        batchMode = Application.isBatchMode;
    }

    #region Log

    // Log.

    public static void Log(object message)
    {
        DebugLog.ReceiveLog(message?.ToString());

        if (!enable) { return; }

        UnityEngine.Debug.Log(message);
    }

    public static void Log(object message, Object context)
    {
        DebugLog.ReceiveLog(message?.ToString());

        if (!enable) { return; }

        UnityEngine.Debug.Log(message, context);
    }

    public static void LogFormat(string format, params object[] args)
    {
        DebugLog.ReceiveLog(string.Format(format, args));

        if (!enable) { return; }

        UnityEngine.Debug.LogFormat(format, args);
    }

    public static void LogFormat(Object context, string format, params object[] args)
    {
        DebugLog.ReceiveLog(string.Format(format, args));

        if (!enable) { return; }

        UnityEngine.Debug.LogFormat(context, format, args);
    }

    // Warning.

    public static void LogWarning(object message)
    {
        DebugLog.ReceiveWarning(message?.ToString());

        if (!enable) { return; }

        UnityEngine.Debug.LogWarning(message);
    }

    public static void LogWarning(object message, Object context)
    {
        DebugLog.ReceiveWarning(message?.ToString());

        if (!enable) { return; }

        UnityEngine.Debug.LogWarning(message, context);
    }

    public static void LogWarningFormat(string format, params object[] args)
    {
        DebugLog.ReceiveWarning(string.Format(format, args));

        if (!enable) { return; }

        UnityEngine.Debug.LogWarningFormat(format, args);
    }

    public static void LogWarningFormat(Object context, string format, params object[] args)
    {
        DebugLog.ReceiveWarning(string.Format(format, args));

        if (!enable) { return; }

        UnityEngine.Debug.LogWarningFormat(context, format, args);
    }

    // Error.

    public static void LogError(object message)
    {
        DebugLog.ReceiveError(message?.ToString());

        if (!enable) { return; }

        UnityEngine.Debug.LogError(message);
    }

    public static void LogError(object message, Object context)
    {
        DebugLog.ReceiveError(message?.ToString());

        if (!enable) { return; }

        UnityEngine.Debug.LogError(message, context);
    }

    public static void LogErrorFormat(string format, params object[] args)
    {
        DebugLog.ReceiveError(string.Format(format, args));

        if (!enable) { return; }

        UnityEngine.Debug.LogErrorFormat(format, args);
    }

    public static void LogErrorFormat(Object context, string format, params object[] args)
    {
        DebugLog.ReceiveError(string.Format(format, args));

        if (!enable) { return; }

        UnityEngine.Debug.LogErrorFormat(context, format, args);
    }

    // Assertion.

    public static void LogAssertion(object message)
    {
        DebugLog.ReceiveAssert(message?.ToString());

        if (!enable) { return; }

        UnityEngine.Debug.LogAssertion(message);
    }

    public static void LogAssertion(object message, Object context)
    {
        DebugLog.ReceiveAssert(message?.ToString());

        if (!enable) { return; }

        UnityEngine.Debug.LogAssertion(message, context);
    }

    public static void LogAssertionFormat(string format, params object[] args)
    {
        DebugLog.ReceiveAssert(string.Format(format, args));

        if (!enable) { return; }

        UnityEngine.Debug.LogAssertionFormat(format, args);
    }

    public static void LogAssertionFormat(Object context, string format, params object[] args)
    {
        DebugLog.ReceiveAssert(string.Format(format, args));

        if (!enable) { return; }

        UnityEngine.Debug.LogAssertionFormat(context, format, args);
    }

    public static void Assert(bool condition)
    {
        if (condition)
        {
            DebugLog.ReceiveAssert(null);
        }

        if (!enable) { return; }

        UnityEngine.Debug.Assert(condition);
    }

    public static void Assert(bool condition, string message)
    {
        if (condition)
        {
            DebugLog.ReceiveAssert(message);
        }

        if (!enable) { return; }

        UnityEngine.Debug.Assert(condition, message);
    }

    // Exception.

    public static void LogException(System.Exception exception)
    {
        DebugLog.ReceiveException(exception);

        if (!enable) { return; }

        UnityEngine.Debug.LogException(exception);
    }

    public static void LogException(System.Exception exception, Object context)
    {
        DebugLog.ReceiveException(exception);

        if (!enable) { return; }

        UnityEngine.Debug.LogException(exception, context);
    }

    #endregion

    public static void Break()
    {
        if (!enable) { return; }

        UnityEngine.Debug.Break();
    }

    #region DrawLine

    public static void DrawLine(Vector3 start, Vector3 end)
    {
        if (!enable) { return; }

        UnityEngine.Debug.DrawLine(start, end);
    }

    public static void DrawLine(Vector3 start, Vector3 end, Color color)
    {
        if (!enable) { return; }

        UnityEngine.Debug.DrawLine(start, end, color);
    }

    public static void DrawLine(Vector3 start, Vector3 end, Color color, float duration)
    {
        if (!enable) { return; }

        UnityEngine.Debug.DrawLine(start, end, color, duration);
    }

    public static void DrawLine(Vector3 start, Vector3 end, Color color, float duration, bool depthTest)
    {
        if (!enable) { return; }

        UnityEngine.Debug.DrawLine(start, end, color, duration, depthTest);        
    }

    #endregion
}

#endif
