
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Reflection;

namespace Extensions.Devkit
{
    public static class SplitterGUILayout
    {
        //----- params -----

        private static BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

        //----- field -----

        private static Type splitterStateType = null;

        private static ConstructorInfo splitterStateCtor = null;

        private static Type splitterGUILayoutType = null;

        private static MethodInfo beginVerticalSplitMethodInfo = null;

        private static MethodInfo endVerticalSplitMethodInfo = null;

        //----- property -----

        //----- method -----

        public static void BeginVerticalSplit(float[] relativeSizes, int[] minSizes, int[] maxSizes, params GUILayoutOption[] options)
        {
            var ctor = GetSplitterStateCtor();

            var splitterState = ctor.Invoke(new object[] { relativeSizes, minSizes, maxSizes });

            var methodInfo = GetBeginVerticalSplitMethodInfo();

            methodInfo.Invoke(null, new object[] { splitterState, options });
        }

        public static void EndVerticalSplit()
        {
            var methodInfo = GetEndVerticalSplitMethodInfo();

            methodInfo.Invoke(null, Type.EmptyTypes);
        }

        private static Type GetSplitterStateType()
        {
            if (splitterStateType == null)
            {
                splitterStateType = typeof(EditorWindow).Assembly.GetTypes().First(x => x.FullName == "UnityEditor.SplitterState");
            }

            return splitterStateType;
        }

        private static ConstructorInfo GetSplitterStateCtor()
        {
            var type = GetSplitterStateType();

            if (splitterStateCtor == null)
            {
                splitterStateCtor = type.GetConstructor(flags, null, new Type[] { typeof(float[]), typeof(int[]), typeof(int[]) }, null);
            }

            return splitterStateCtor;
        }

        private static Type GetSplitterGUILayoutType()
        {
            if (splitterGUILayoutType == null)
            {
                splitterGUILayoutType = typeof(EditorWindow).Assembly.GetTypes().First(x => x.FullName == "UnityEditor.SplitterGUILayout");
            }

            return splitterGUILayoutType;
        }

        private static MethodInfo GetBeginVerticalSplitMethodInfo()
        {
            var splitterGUILayoutType = GetSplitterGUILayoutType();
            var splitterStateType = GetSplitterStateType();

            if (beginVerticalSplitMethodInfo == null)
            {
                beginVerticalSplitMethodInfo = splitterGUILayoutType.GetMethod("BeginVerticalSplit", flags, null, new Type[] { splitterStateType, typeof(GUILayoutOption[]) }, null);
            }

            return beginVerticalSplitMethodInfo;
        }

        private static MethodInfo GetEndVerticalSplitMethodInfo()
        {
            var type = GetSplitterGUILayoutType();

            if (endVerticalSplitMethodInfo == null)
            {
                endVerticalSplitMethodInfo = type.GetMethod("EndVerticalSplit", flags, null, Type.EmptyTypes, null);
            }

            return endVerticalSplitMethodInfo;
        }
    }
}
