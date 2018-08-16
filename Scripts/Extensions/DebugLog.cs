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
        if(isDebugBuild)
        {
            UnityEngine.Debug.Log( message );
        }
    }

    public static void Log( object message, Object context )
    {
        if(isDebugBuild)
        {
            UnityEngine.Debug.Log( message, context );
        }
    }

    public static void LogFormat(string format, params object[] args)
    {
        if (isDebugBuild)
        {
            UnityEngine.Debug.LogFormat(format, args);
        }
    }

    public static void LogFormat(Object context, string format, params object[] args)
    {
        if (isDebugBuild)
        {
            UnityEngine.Debug.LogFormat(context, format, args);
        }
    }

    // Warning.

    public static void LogWarning(object message)
    {
        if (isDebugBuild)
        {
            UnityEngine.Debug.LogWarning(message);
        }
    }

    public static void LogWarning(object message, Object context)
    {
        if (isDebugBuild)
        {
            UnityEngine.Debug.LogWarning(message, context);
        }
    }

    public static void LogWarningFormat(string format, params object[] args)
    {
        if (isDebugBuild)
        {
            UnityEngine.Debug.LogWarningFormat(format, args);
        }
    }

    public static void LogWarningFormat(Object context, string format, params object[] args)
    {
        if (isDebugBuild)
        {
            UnityEngine.Debug.LogWarningFormat(context, format, args);
        }
    }

    // Error.

    public static void LogError(object message)
    {
        if (isDebugBuild)
        {
            UnityEngine.Debug.LogError(message);
        }
    }

    public static void LogError(object message, Object context)
    {
        if (isDebugBuild)
        {
            UnityEngine.Debug.LogError(message, context);
        }
    }

    public static void LogErrorFormat(string format, params object[] args)
    {
        if (isDebugBuild)
        {
            UnityEngine.Debug.LogErrorFormat(format, args);
        }
    }

    public static void LogErrorFormat(Object context, string format, params object[] args)
    {
        if (isDebugBuild)
        {
            UnityEngine.Debug.LogErrorFormat(context, format, args);
        }
    }

    // Assertion.

    public static void LogAssertion(object message)
    {
        if (isDebugBuild)
        {
            UnityEngine.Debug.LogAssertion(message);
        }
    }

    public static void LogAssertion(object message, Object context)
    {
        if (isDebugBuild)
        {
            UnityEngine.Debug.LogAssertion(message, context);
        }
    }

    public static void LogAssertionFormat(string format, params object[] args)
    {
        if (isDebugBuild)
        {
            UnityEngine.Debug.LogAssertionFormat(format, args);
        }
    }

    // Exception.

    public static void LogException(System.Exception exception)
    {
        if (isDebugBuild)
        {
            UnityEngine.Debug.LogException(exception);
        }
    }

    public static void LogException(System.Exception exception, Object context)
    {
        if (isDebugBuild)
        {
            UnityEngine.Debug.LogException(exception, context);
        }
    }

    #endregion

    public static void Break()
    {
        if (isDebugBuild)
        {
            UnityEngine.Debug.Break();
        }
    }

    #region DrawLine

    public static void DrawLine(Vector3 start, Vector3 end)
    {
        if (isDebugBuild)
        {
            UnityEngine.Debug.DrawLine(start, end);
        }
    }

    public static void DrawLine(Vector3 start, Vector3 end, Color color)
    {
        if (isDebugBuild)
        {
            UnityEngine.Debug.DrawLine(start, end, color);
        }
    }

    public static void DrawLine(Vector3 start, Vector3 end, Color color, float duration)
    {
        if (isDebugBuild)
        {
            UnityEngine.Debug.DrawLine(start, end, color, duration);
        }
    }

    public static void DrawLine(Vector3 start, Vector3 end, Color color, float duration, bool depthTest)
    {
        if (isDebugBuild)
        {
            UnityEngine.Debug.DrawLine(start, end, color, duration, depthTest);
        }
    }

    #endregion
}

#endif
