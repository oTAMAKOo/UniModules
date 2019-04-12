﻿﻿﻿﻿﻿﻿
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using Extensions;
using Extensions.Devkit;
using Unity.Linq;
using SortingLayer = Constants.SortingLayer;

namespace Modules.Particle
{
    [InitializeOnLoad]
    public static class ParticlePlayerEmulator
    {
        public static Action<float> update;

        static ParticlePlayerEmulator()
        {
            var prevTime = EditorApplication.timeSinceStartup;

            EditorApplication.update += () =>
            {
                if (!Application.isPlaying)
                {
                    var time = EditorApplication.timeSinceStartup - prevTime;

                    if (update != null)
                    {
                        update((float)time);
                    }

                    prevTime = EditorApplication.timeSinceStartup;
                }
            };
        }
    }

    [CustomEditor(typeof(ParticlePlayer), true)]
    public class ParticlePlayerInspector : UnityEditor.Editor
    {
        //----- params -----

        //----- field -----

        private LifetimeDisposable disposable = new LifetimeDisposable();

        private IDisposable emulateDisposable = null;
        private IDisposable updateDisposable = null;
        private IDisposable eventLogDisposable = null;

        private Vector2 scrollPosition = Vector2.zero;

        private ParticlePlayer instance = null;

        //----- property -----

        //----- method -----

        void OnEnable()
        {
            instance = target as ParticlePlayer;

            if (!instance.IsInitialized)
            {
                Reflection.InvokePrivateMethod(instance, "Initialize");
            }

            if (!Application.isPlaying)
            {
                ParticlePlayerEmulator.update -= Emulate;
                ParticlePlayerEmulator.update += Emulate;

                var colorCode = Color.magenta.ColorToHex();

                eventLogDisposable = instance.OnEventAsObservable()
                    .Subscribe(x => Debug.LogFormat("<color=#{0}><b>[ParticlePlayer Event]</b></color> {1}", colorCode, x))
                    .AddTo(disposable.Disposable);
            }
            else
            {
                updateDisposable = instance
                    .ObserveEveryValueChanged(x => x.CurrentTime)
                    .Subscribe(x => Repaint())
                    .AddTo(disposable.Disposable);
            }
        }

        void OnDisable()
        {
            if(Application.isPlaying)
            {
                if (updateDisposable != null)
                {
                    updateDisposable.Dispose();
                }
            }
            else
            {
                ParticlePlayerEmulator.update -= Emulate;
                
                instance.Stop(true, true);

                if (eventLogDisposable != null)
                {
                    eventLogDisposable.Dispose();
                }
            }

            SetParticleSystemsDirty();
        }

        private void Emulate(float time)
        {
            if (instance != null && instance.State == State.Play)
            {
                Reflection.InvokePrivateMethod(instance, "UpdateCurrentTime", new object[] { time });

                Reflection.InvokePrivateMethod(instance, "InvokeEvent");

                if (!instance.IsAlive())
                {
                    Reflection.InvokePrivateMethod(instance, "ResetContents");
                }

                SetParticleSystemsDirty();
                
                Repaint();
            }
        }

        public override void OnInspectorGUI()
        {
            instance = target as ParticlePlayer;
            
            var activateOnPlay = Reflection.GetPrivateField<ParticlePlayer, bool>(instance, "activateOnPlay");
            var endActionType = Reflection.GetPrivateField<ParticlePlayer, EndActionType>(instance, "endActionType");
            var ignoreTimeScale = Reflection.GetPrivateField<ParticlePlayer, bool>(instance, "ignoreTimeScale");
            var lifecycleType = Reflection.GetPrivateField<ParticlePlayer, LifecycleControl>(instance, "lifecycleControl");
            var lifeTime = Reflection.GetPrivateField<ParticlePlayer, float>(instance, "lifeTime");
            var events = Reflection.GetPrivateField<ParticlePlayer, ParticlePlayer.EventInfo[]>(instance, "eventInfos");

            EditorGUILayout.Separator();

            if (!Application.isPlaying)
            {
                using(new EditorGUILayout.HorizontalScope())
                {
                    if (State.Play == instance.State)
                    {
                        if (GUILayout.Button("Pause"))
                        {
                            instance.Pause = true;
                            Repaint();
                        }
                    }
                    else
                    {
                        if (GUILayout.Button("Play"))
                        {
                            if (instance.State == State.Pause)
                            {
                                instance.Pause = false;
                            }
                            else
                            {
                                if (emulateDisposable != null)
                                {
                                    emulateDisposable.Dispose();
                                    emulateDisposable = null;
                                }

                                Reflection.InvokePrivateMethod(instance, "RunCollectContents");

                                emulateDisposable = instance.Play()
                                    .Subscribe(_ =>
                                        {
                                            instance.Stop(true, true);

                                            if (emulateDisposable != null)
                                            {
                                                emulateDisposable.Dispose();
                                                emulateDisposable = null;
                                            }
                                        })
                                    .AddTo(disposable.Disposable);
                            }

                            Repaint();
                        }
                    }

                    GUI.enabled = State.Play == instance.State || State.Pause == instance.State;

                    if (GUILayout.Button("Stop"))
                    {
                        if (emulateDisposable != null)
                        {
                            emulateDisposable.Dispose();
                            emulateDisposable = null;
                        }

                        instance.Stop(true, true);

                        Repaint();

                        SetParticleSystemsDirty();
                    }

                    GUI.enabled = true;

                    GUILayout.Space(20f);

                    var centeredStyle = GUI.skin.GetStyle("TextArea");
                    centeredStyle.alignment = TextAnchor.MiddleRight;

                    var value = string.Format("{0:f2}", instance.CurrentTime);
                    GUILayout.Label(value, centeredStyle, GUILayout.Width(60f), GUILayout.Height(18f));
                }
            }

            EditorGUILayout.Separator();

            if (EditorLayoutTools.DrawHeader("SortingLayer", "ParticlePlayerInspector-SortingLayer"))
            {
                using(new ContentsScope())
                {
                    var originLabelWidth = EditorLayoutTools.SetLabelWidth(150f);

                    EditorGUI.BeginChangeCheck();

                    var sortingLayerState = (SortingLayer)EditorGUILayout.EnumPopup("SortingLayer", instance.SortingLayer);

                    if (EditorGUI.EndChangeCheck())
                    {
                        UnityEditorUtility.RegisterUndo("ParticlePlayerInspector Undo", instance);

                        Reflection.InvokePrivateMethod(instance, "RunCollectContents");
                        instance.SortingLayer = sortingLayerState;
                    }

                    EditorLayoutTools.SetLabelWidth(150f);

                    var sortingOrderState = instance.SortingOrder;

                    // ProjectViewで編集されると子階層への伝搬が正しく出来ないのでHierarchyからのみ編集させる.
                    if(AssetDatabase.IsMainAsset(instance.gameObject))
                    {
                        EditorGUILayout.IntField("SortingOrder", instance.SortingOrder);
                        EditorGUILayout.HelpBox("This parameter can edit in hierarchy.", MessageType.Info);
                    }
                    else
                    {
                        sortingOrderState = EditorGUILayout.IntField("SortingOrder", instance.SortingOrder);
                    }

                    if (instance.SortingOrder != sortingOrderState)
                    {
                        UnityEditorUtility.RegisterUndo("ParticlePlayerInspector Undo", instance);

                        Reflection.InvokePrivateMethod(instance, "RunCollectContents");
                        instance.SortingOrder = sortingOrderState;
                    }

                    EditorLayoutTools.SetLabelWidth(originLabelWidth);
                }
            }

            if (EditorLayoutTools.DrawHeader("Option", "ParticlePlayerInspector-Option"))
            {
                using (new ContentsScope())
                {
                    EditorGUI.BeginChangeCheck();

                    var originLabelWidth = EditorLayoutTools.SetLabelWidth(150f);

                    activateOnPlay = EditorGUILayout.Toggle("AutoPlay", activateOnPlay);

                    ignoreTimeScale = EditorGUILayout.Toggle("Ignore TimeScale", ignoreTimeScale);

                    endActionType = (EndActionType)EditorGUILayout.EnumPopup("End Action", endActionType);

                    lifecycleType = (LifecycleControl)EditorGUILayout.EnumPopup("Lifcycle Type", lifecycleType);

                    if (lifecycleType == LifecycleControl.Manual)
                    {
                        lifeTime = EditorGUILayout.FloatField("Life Time", lifeTime);
                    }

                    if (EditorGUI.EndChangeCheck())
                    {
                        UnityEditorUtility.RegisterUndo("ParticlePlayerInspector Undo", instance);

                        Reflection.SetPrivateField(instance, "activateOnPlay", activateOnPlay);
                        Reflection.SetPrivateField(instance, "endActionType", endActionType);
                        Reflection.SetPrivateField(instance, "ignoreTimeScale", ignoreTimeScale);
                        Reflection.SetPrivateField(instance, "lifecycleControl", lifecycleType);
                        Reflection.SetPrivateField(instance, "lifeTime", lifeTime);
                        Reflection.SetPrivateField(instance, "activateOnPlay", activateOnPlay);
                    }

                    EditorLayoutTools.SetLabelWidth(originLabelWidth);
                }
            }

            if (EditorLayoutTools.DrawHeader("Event", "ParticlePlayerInspector-Event"))
            {
                var updated = false;
                var eventList = events != null ? events.ToList() : new List<ParticlePlayer.EventInfo>();

                using (new ContentsScope())
                {
                    if (eventList.IsEmpty())
                    {
                        EditorGUILayout.HelpBox("Press the + button if add event", MessageType.Info);
                    }
                    else
                    {
                        GUILayout.Space(3f);

                        DrawEventHeaderGUI(eventList);

                        var deleteTargets = new List<ParticlePlayer.EventInfo>();

                        Action drawContents = () =>
                        {
                            foreach (var eventInfo in eventList)
                            {
                                EditorGUI.BeginChangeCheck();

                                var delete = DrawEventInfoGUI(eventInfo);

                                if (EditorGUI.EndChangeCheck())
                                {
                                    updated = true;
                                }

                                if (delete)
                                {
                                    deleteTargets.Add(eventInfo);
                                    updated = true;
                                }

                                GUILayout.Space(2f);
                            }
                        };

                        if (eventList.Count <= 5)
                        {
                            drawContents();
                        }
                        else
                        {
                            using (var scrollViewScope = new EditorGUILayout.ScrollViewScope(scrollPosition, GUILayout.Height(100f)))
                            {
                                drawContents();

                                scrollPosition = scrollViewScope.scrollPosition;
                            }
                        }

                        foreach (var deleteTarget in deleteTargets)
                        {
                            eventList.Remove(deleteTarget);
                        }
                    }

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();

                        if (GUILayout.Button("+", EditorStyles.miniButton, GUILayout.Width(60f)))
                        {
                            var eventInfo = new ParticlePlayer.EventInfo()
                            {
                                trigger = ParticlePlayer.EventInfo.EventTrigger.Time,
                                target = null,
                                time = 0f,
                                message = string.Empty,
                            };

                            eventList.Add(eventInfo);

                            updated = true;
                        }
                    }

                    if (updated)
                    {
                        Reflection.SetPrivateField(instance, "eventInfos", eventList.ToArray());
                    }
                }
            }

            EditorGUILayout.Separator();
        }

        private void DrawEventHeaderGUI(List<ParticlePlayer.EventInfo> eventList)
        {
            var style = new GUIStyle("ShurikenModuleTitle");
            style.font = new GUIStyle(EditorStyles.label).font;
            style.border = new RectOffset(2, 2, 2, 2);
            style.fixedHeight = 16;
            style.contentOffset = new Vector2(0f, -2f);
            style.alignment = TextAnchor.MiddleCenter;

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                EditorGUILayout.LabelField("Trigger", style, GUILayout.Width(55f));
                EditorGUILayout.LabelField("Invoke", style, GUILayout.Width(150f));
                EditorGUILayout.LabelField("Message", style, GUILayout.Width(150f));
                GUILayout.Space(eventList.Count <= 5 ? 30f : 43f);

                GUILayout.FlexibleSpace();
            }
        }

        private bool DrawEventInfoGUI(ParticlePlayer.EventInfo info)
        {
            var delete = false;

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                info.trigger = (ParticlePlayer.EventInfo.EventTrigger)EditorGUILayout.EnumPopup(info.trigger, GUILayout.Width(55f));

                switch (info.trigger)
                {
                    case ParticlePlayer.EventInfo.EventTrigger.Time:
                        {
                            info.target = null;

                            info.time = (float)EditorGUILayout.DelayedDoubleField(info.time, GUILayout.Width(150f));
                        }
                        break;

                    case ParticlePlayer.EventInfo.EventTrigger.Birth:
                    case ParticlePlayer.EventInfo.EventTrigger.Alive:
                    case ParticlePlayer.EventInfo.EventTrigger.Death:
                        {
                            info.time = 0f;

                            EditorGUI.BeginChangeCheck();

                            var particleSystem = (ParticleSystem)EditorGUILayout.ObjectField(info.target, typeof(ParticleSystem), true, GUILayout.Width(150f));

                            if (EditorGUI.EndChangeCheck())
                            {
                                var isChild = instance.gameObject.DescendantsAndSelf().OfComponent<ParticleSystem>().Contains(particleSystem);

                                if (isChild)
                                {
                                    info.target = particleSystem;
                                }
                            }
                        }
                        break;
                }

                info.message = EditorGUILayout.DelayedTextField(info.message, GUILayout.Width(150f));

                if (GUILayout.Button("-", EditorStyles.miniButton, GUILayout.Width(25f)))
                {
                    delete = true;
                }

                GUILayout.FlexibleSpace();
            }

            return delete;
        }

        private void SetParticleSystemsDirty()
        {
            var particleInfos = Reflection.GetPrivateField<ParticlePlayer, ParticlePlayer.ParticleInfo[]>(instance, "particleInfos");

            foreach (var info in particleInfos)
            {
                if (UnityUtility.IsNull(info.ParticleSystem)) { continue; }

                EditorUtility.SetDirty(info.ParticleSystem.gameObject);
            }

            InternalEditorUtility.RepaintAllViews();
        }
    }
}
