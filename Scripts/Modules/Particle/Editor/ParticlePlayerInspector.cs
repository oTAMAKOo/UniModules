﻿﻿﻿﻿﻿
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System;
using UniRx;
using Extensions;
using Extensions.Devkit;
using Modules.Devkit.EventHook;

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
        private IDisposable applyDisposable = null;
        private IDisposable updateDisposable = null;

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

            if(applyDisposable != null)
            {
                applyDisposable.Dispose();
            }

            if (!Application.isPlaying)
            {
                ParticlePlayerEmulator.update -= Emulate;
                ParticlePlayerEmulator.update += Emulate;

                // Applyされたタイミングで子階層のSortingOrderを戻してから保存する.
                applyDisposable = PrefabApplyHook.OnApplyPrefabAsObservable().Subscribe(
                        x =>
                        {
                            if (instance != null && x == instance.gameObject)
                            {
                                Reflection.InvokePrivateMethod(instance, "RevertOriginSortingOrder");
                            }
                        })
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

                if (applyDisposable != null)
                {
                    applyDisposable.Dispose();
                }

                instance.Simulate(0);
                instance.Stop(true, true);
            }

            SetParticleSystemsDirty();
        }

        private void Emulate(float time)
        {
            if (instance != null && instance.State == State.Play)
            {
                instance.Simulate(time);
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
            var lifecycleType = Reflection.GetPrivateField<ParticlePlayer, LifcycleType>(instance, "lifecycleType");
            var lifeTime = Reflection.GetPrivateField<ParticlePlayer, float>(instance, "lifeTime");

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

                    lifecycleType = (LifcycleType)EditorGUILayout.EnumPopup("Lifcycle Type", lifecycleType);

                    if (lifecycleType == LifcycleType.Manual)
                    {
                        lifeTime = EditorGUILayout.FloatField("Life Time", lifeTime);
                    }

                    if (EditorGUI.EndChangeCheck())
                    {
                        UnityEditorUtility.RegisterUndo("ParticlePlayerInspector Undo", instance);

                        Reflection.SetPrivateField(instance, "activateOnPlay", activateOnPlay);
                        Reflection.SetPrivateField(instance, "endActionType", endActionType);
                        Reflection.SetPrivateField(instance, "ignoreTimeScale", ignoreTimeScale);
                        Reflection.SetPrivateField(instance, "lifecycleType", lifecycleType);
                        Reflection.SetPrivateField(instance, "lifeTime", lifeTime);
                        Reflection.SetPrivateField(instance, "activateOnPlay", activateOnPlay);
                    }

                    EditorLayoutTools.SetLabelWidth(originLabelWidth);
                }
            }

            EditorGUILayout.Separator();
        }

        private void SetParticleSystemsDirty()
        {
            var particleSystemInfos = Reflection.GetPrivateField<ParticlePlayer, ParticlePlayer.ParticleSystemInfo[]>(instance, "particleSystems");

            foreach (var info in particleSystemInfos)
            {
                if (UnityUtility.IsNull(info.ParticleSystem)) { continue; }

                EditorUtility.SetDirty(info.ParticleSystem.gameObject);
            }

            InternalEditorUtility.RepaintAllViews();
        }
    }
}
