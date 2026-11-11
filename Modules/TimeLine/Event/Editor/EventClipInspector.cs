
#if ENABLE_UNITY_TIMELINE

using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using Extensions;
using Extensions.Devkit;

using Object = UnityEngine.Object;

namespace Modules.TimeLine.Component
{
    [CustomEditor(typeof(EventClip), true)]
    public sealed class EventClipInspector : UnityEditor.Editor
    {
        //----- params -----

        //----- field -----

        private EventClip timeLineEvent = null;

        private List<EventMethodInfo> editTargets = null;
        private List<EventMethodInfo> deleteTargets = null;

        private bool initialized = false;

        //----- property -----

        //----- method -----

        public override void OnInspectorGUI()
        {
            Initialize();

            if (editTargets.IsEmpty())
            {
                var message = "You can add events by pressing the + button.";

                EditorGUILayout.HelpBox(message, MessageType.Info);
            }

            deleteTargets = new List<EventMethodInfo>();

            var save = false;

            foreach (var editTarget in editTargets)
            {
                save |= DrawEditTargetContentsGUI(editTarget);
            }

            foreach (var deleteTarget in deleteTargets)
            {
                editTargets.Remove(deleteTarget);
            }

            if (save)
            {
                Save();
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if(GUILayout.Button("+", EditorStyles.miniButton, GUILayout.Width(50f)))
                {
                    editTargets.Add(new EventMethodInfo());
                    Save();
                }

                GUILayout.FlexibleSpace();
            }
        }

        private void Initialize()
        {
            if (initialized) { return; }

            timeLineEvent = target as EventClip;

            BuildEditTargets();

            initialized = true;
        }

        private void BuildEditTargets()
        {
            var methods = Reflection.GetPrivateField<EventClip, EventMethod[]>(timeLineEvent, "methods");

            editTargets = new List<EventMethodInfo>();

            if (methods != null)
            {
                foreach (var method in methods)
                {
                    var info = new EventMethodInfo();

                    info.SetInvokeTarget(method.InvokeTarget.GetValue());
                    info.SetEventType(method.EventType);
                    info.SetMethodName(method.MethodName);
                    info.SetArguments(method.ValueArguments, method.ObjectArguments);

                    editTargets.Add(info);
                }
            }
        }

        private bool DrawEditTargetContentsGUI(EventMethodInfo editTarget)
        {
            var requestSave = false;

            using (new ContentsScope())
            {
                EditorGUI.BeginChangeCheck();

                var target = EditorGUILayout.ObjectField("Target", editTarget.InvokeTarget, typeof(GameObject), true) as GameObject;

                if (EditorGUI.EndChangeCheck())
                {
                    editTarget.SetInvokeTarget(target);
                    requestSave = true;
                }

                EditorGUI.BeginChangeCheck();

                var eventType = (EventType)EditorGUILayout.EnumPopup("Event", editTarget.EventType);

                if (EditorGUI.EndChangeCheck())
                {
                    editTarget.SetEventType(eventType);
                    requestSave = true;
                }

                if (editTarget.InvokeTarget != null)
                {
                    if (editTarget.CallbackMethods.IsEmpty())
                    {
                        GUILayout.Space(3f);

                        var message = "TimeLineEvent attribute function is not defined.";

                        EditorGUILayout.HelpBox(message, MessageType.Warning);
                    }
                    else
                    {
                        //------ Method ------

                        if (editTarget.CallbackMethods.Any())
                        {
                            var index = editTarget.CallbackMethods.IndexOf(x => x == editTarget.MethodName);
                            var labels = editTarget.CallbackMethodNames.ToArray();

                            // 「None」の分ずらす.
                            index = index != -1 ? index + 1 : 0;

                            EditorGUI.BeginChangeCheck();

                            index = EditorGUILayout.Popup("Method", index, labels, GUILayout.ExpandWidth(true));

                            if (EditorGUI.EndChangeCheck())
                            {
                                var methodName = 0 < index ? editTarget.CallbackMethods[index - 1] : null;

                                editTarget.SetMethodName(methodName);
                                requestSave = true;
                            }
                        }

                        //------ Argument ------

                        if (editTarget.Arguments.Any())
                        {
                            EditorGUILayout.Separator();

                            using (new EditorGUILayout.HorizontalScope())
                            {
                                GUILayout.Space(20f);

                                using (new EditorGUILayout.VerticalScope())
                                {
                                    EditorLayoutTools.ContentTitle("Argument");

                                    using (new ContentsScope())
                                    {
                                        foreach (var argument in editTarget.Arguments)
                                        {
                                            var name = argument.info.Name;
                                            var type = argument.info.ParameterType;

                                            if (type == typeof(GameObject))
                                            {
                                                EditorGUI.BeginChangeCheck();

                                                var value = EditorLayoutTools.ObjectField(name, argument.value as Object, true);

                                                if (EditorGUI.EndChangeCheck())
                                                {
                                                    if (!AssetDatabase.IsMainAsset(value))
                                                    {
                                                        argument.value = value;
                                                        requestSave = true;
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                var space = 154f;

                                                object value = null;

                                                EditorGUI.BeginChangeCheck();

                                                if (type == typeof(string))
                                                {
                                                    value = EditorLayoutTools.DelayedTextField(name, (string)argument.value, space);
                                                }
                                                else if (type == typeof(int))
                                                {
                                                    value = EditorLayoutTools.DelayedIntField(name, (int)(argument.value ?? 0), space);
                                                }
                                                else if (type == typeof(float))
                                                {
                                                    value = EditorLayoutTools.DelayedFloatField(name, (float)(argument.value ?? 0f), space);
                                                }
                                                else if (type == typeof(bool))
                                                {
                                                    value = EditorLayoutTools.BoolField(name, (bool)(argument.value ?? false), space);
                                                }
                                                else if (type.IsEnum)
                                                {
                                                    var names = Enum.GetNames(type);
                                                    var values = Enum.GetValues(type).Cast<int>().ToArray();
                                                    var enumValue = (int)(argument.value ?? -1);
                                                    var index = Math.Max(Array.IndexOf(values, enumValue), 0);

                                                    index = EditorGUILayout.Popup(name, index, names, GUILayout.ExpandWidth(true));

                                                    value = 0 <= index ? (int)values[index] : -1;
                                                }

                                                if (EditorGUI.EndChangeCheck())
                                                {
                                                    argument.value = value;
                                                    requestSave = true;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();

                    using (new EditorGUILayout.VerticalScope(GUILayout.Width(50f)))
                    {
                        GUILayout.Space(3f);

                        if (GUILayout.Button("delete", EditorStyles.miniButton))
                        {
                            if (EditorUtility.DisplayDialog("Confirm", "Delete this event?", "delete", "cancel"))
                            {
                                deleteTargets.Add(editTarget);
                                requestSave = true;
                            }
                        }
                    }
                }
            }

            return requestSave;
        }

        private void Save()
        {
            UnityEditorUtility.RegisterUndo(timeLineEvent);

            // 上書きする為一旦全解放.
            var methods = Reflection.GetPrivateField<EventClip, EventMethod[]>(timeLineEvent, "methods");

            if (methods != null)
            {
                foreach (var method in methods)
                {
                    method.Clear();
                }
            }

            // 更新.
            var list = new List<EventMethod>();

            foreach (var editTarget in editTargets)
            {
                var method = new EventMethod();

                method.Setup(timeLineEvent.PlayableDirector);

                method.InvokeTarget.SetValue(editTarget.InvokeTarget);
                method.EventType = editTarget.EventType;
                method.MethodName = editTarget.MethodName;

                var obectArguments = new List<EventMethod.ArgumentObjects>();

                if (editTarget.Arguments != null)
                {
                    foreach (var argument in editTarget.Arguments)
                    {
                        if (argument.info.ParameterType != typeof(GameObject)) { continue; }

                        var argumentObject = new EventMethod.ArgumentObjects();

                        argumentObject.Setup(timeLineEvent.PlayableDirector);
                        argumentObject.TargetObject.SetValue(argument.value as GameObject);

                        obectArguments.Add(argumentObject);
                    }
                }

                method.ObjectArguments = obectArguments.ToArray();

                var valueArguments = new List<string>();

                if (editTarget.Arguments != null)
                {
                    var valueTypes = new Type[]
                    {
                        typeof(string),
                        typeof(int),
                        typeof(float),
                        typeof(bool),
                    };

                    foreach (var argument in editTarget.Arguments)
                    {
                        object value = null;
                        var type = argument.info.ParameterType;

                        if (type.IsValueType)
                        {
                            value = argument.value ?? Activator.CreateInstance(type);
                        }
                        else
                        {
                            value = argument.value;
                        }

                        if (valueTypes.Contains(type))
                        {
                            valueArguments.Add(value != null ? value.ToString() : null);
                        }
                        else if(type.IsEnum)
                        {
                            valueArguments.Add(((int)value).ToString());
                        }
                    }
                }

                method.ValueArguments = valueArguments.ToArray();

                list.Add(method);
            }

            Reflection.SetPrivateField(timeLineEvent, "methods", list.ToArray());

            BuildEditTargets();
        }
    }
}

#endif