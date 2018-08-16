﻿﻿
using UnityEditor;
using System.Reflection;

namespace Extensions.Devkit
{
	public static class LocalIdentifierInFile
	{
        //----- params -----

        //----- field -----

        private static PropertyInfo cachedInspectorModeInfo;

        //----- property -----

        //----- method -----

        public static int Get(UnityEngine.Object unityObject)
        {
            var id = -1;

            if (unityObject == null) return id;

            if (cachedInspectorModeInfo == null)
            {
                cachedInspectorModeInfo = typeof(SerializedObject).GetProperty("inspectorMode", BindingFlags.NonPublic | BindingFlags.Instance);
            }

            var serializedObject = new SerializedObject(unityObject);
            cachedInspectorModeInfo.SetValue(serializedObject, InspectorMode.Debug, null);
            var serializedProperty = serializedObject.FindProperty("m_LocalIdentfierInFile");

            id = serializedProperty.intValue;

            if (id <= 0)
            {
                var prefabType = PrefabUtility.GetPrefabType(unityObject);

                id = prefabType != PrefabType.None ? Get(PrefabUtility.GetPrefabObject(unityObject)) : unityObject.GetInstanceID();
            }

            return id;
        }
    }
}