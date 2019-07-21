﻿﻿﻿
using UnityEngine;

#if !UNITY_EDITOR

public static class Debug
{
    private static bool? isDevlopmentBuild = null;

    public static bool isDebugBuild
    {
        get
        {
            if(!isDevlopmentBuild.HasValue)
            {
                isDevlopmentBuild = UnityEngine.Debug.isDebugBuild;
            }

            return isDevlopmentBuild.Value;
        }
    }

    [RuntimeInitializeOnLoadMethod]
    static void RuntimeInitializeOnLoadMethod()
    {
        if (!isDevlopmentBuild.HasValue)
        {
            isDevlopmentBuild = UnityEngine.Debug.isDebugBuild;
        }
    }

    #region Log

    // Log.

    public static void Log( object message )
    {
        if (!isDebugBuild) { return; }

        UnityEngine.Debug.Log(message);
    }

    public static void Log( object message, Object context )
    {
        if (!isDebugBuild) { return; }

        UnityEngine.Debug.Log(message, context);
    }

    public static void LogFormat(string format, params object[] args)
    {
        if (!isDebugBuild) { return; }

        UnityEngine.Debug.LogFormat(format, args);
    }

    public static void LogFormat(Object context, string format, params object[] args)
    {
        if (!isDebugBuild) { return; }

        UnityEngine.Debug.LogFormat(context, format, args);
    }

    // Warning.

    public static void LogWarning(object message)
    {
        if (!isDebugBuild) { return; }

        UnityEngine.Debug.LogWarning(message);
    }

    public static void LogWarning(object message, Object context)
    {
        if (!isDebugBuild) { return; }

        UnityEngine.Debug.LogWarning(message, context);
    }

    public static void LogWarningFormat(string format, params object[] args)
    {
        if (!isDebugBuild) { return; }

        UnityEngine.Debug.LogWarningFormat(format, args);
    }

    public static void LogWarningFormat(Object context, string format, params object[] args)
    {
        if (!isDebugBuild) { return; }

        UnityEngine.Debug.LogWarningFormat(context, format, args);
    }

    // Error.

    public static void LogError(object message)
    {
        if (!isDebugBuild) { return; }

        UnityEngine.Debug.LogError(message);
    }

    public static void LogError(object message, Object context)
    {
        if (!isDebugBuild) { return; }

        UnityEngine.Debug.LogError(message, context);
    }

    public static void LogErrorFormat(string format, params object[] args)
    {
        if (!isDebugBuild) { return; }

        UnityEngine.Debug.LogErrorFormat(format, args);
    }

    public static void LogErrorFormat(Object context, string format, params object[] args)
    {
        if (!isDebugBuild) { return; }

        UnityEngine.Debug.LogErrorFormat(context, format, args);
    }

    // Assertion.

    public static void LogAssertion(object message)
    {
        if (!isDebugBuild) { return; }

        UnityEngine.Debug.LogAssertion(message);
    }

    public static void LogAssertion(object message, Object context)
    {
        if (!isDebugBuild) { return; }

        UnityEngine.Debug.LogAssertion(message, context);
    }

    public static void LogAssertionFormat(string format, params object[] args)
    {
        if (!isDebugBuild) { return; }

        UnityEngine.Debug.LogAssertionFormat(format, args);
    }

    public static void Assert(bool condition)
    {
        UnityEngine.Debug.Assert(condition);
    }

    public static void Assert(bool condition, string message)
    {
        if (!isDebugBuild){ return; }

        UnityEngine.Debug.Assert(condition, message);
    }

    // Exception.

    public static void LogException(System.Exception exception)
    {
        if (!isDebugBuild) { return; }

        UnityEngine.Debug.LogException(exception);
    }

    public static void LogException(System.Exception exception, Object context)
    {
        if (!isDebugBuild) { return; }

        UnityEngine.Debug.LogException(exception, context);
    }

    #endregion

    public static void Break()
    {
        if (!isDebugBuild) { return; }

        UnityEngine.Debug.Break();
    }

    #region DrawLine

    public static void DrawLine(Vector3 start, Vector3 end)
    {
        if (!isDebugBuild) { return; }

        UnityEngine.Debug.DrawLine(start, end);
    }

    public static void DrawLine(Vector3 start, Vector3 end, Color color)
    {
        if (!isDebugBuild) { return; }

        UnityEngine.Debug.DrawLine(start, end, color);
    }

    public static void DrawLine(Vector3 start, Vector3 end, Color color, float duration)
    {
        if (!isDebugBuild) { return; }

        UnityEngine.Debug.DrawLine(start, end, color, duration);
    }

    public static void DrawLine(Vector3 start, Vector3 end, Color color, float duration, bool depthTest)
    {
        if (!isDebugBuild) { return; }

        UnityEngine.Debug.DrawLine(start, end, color, duration, depthTest);        
    }

    #endregion
}

#endif
