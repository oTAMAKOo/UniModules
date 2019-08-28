﻿
using UnityEngine;
using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Modules.SceneManagement;

namespace Modules.Window
{
    public abstract class PopupManager<TInstance> : SingletonMonoBehaviour<TInstance> where TInstance : PopupManager<TInstance>, new()
    {
        //----- params -----

        //----- field -----

        [SerializeField]
        private GameObject touchBlocPrefab = null;
        [SerializeField]
        private GameObject parentPrefab = null;
        [SerializeField]
        private int sceneCanvasOrder = 0;
        [SerializeField]
        private int globalCanvasOrder = 1;

        private PopupParent parentInScene = null;
        private PopupParent parentGlobal = null;

        private TouchBloc touchBloc = null;
        private IDisposable touchBlocDisposable = null;

        // 登録されているポップアップ.
        protected List<Window> scenePopups = new List<Window>();
        protected List<Window> globalPopups = new List<Window>();

        //----- property -----

        public GameObject ParentInScene { get { return parentInScene.Parent; } }
        public GameObject ParentGlobal { get { return parentGlobal.Parent; } }

        //----- method -----

        protected abstract int ParentInSceneLayer { get; }

        protected abstract int ParentGlobalLayer { get; }

        public virtual void Initialize()
        {
            parentGlobal = CreatePopupParent("Popup (Global)", gameObject, ParentGlobalLayer, globalCanvasOrder);

            UpdateContents();
        }

        /// <summary> ポップアップを開く </summary>
        public static IObservable<Unit> Open(Window popupWindow, bool isGlobal = false, bool inputProtect = true)
        {
            if (popupWindow == null)
            {
                throw new ArgumentException("Invalid popupWindow");
            }

            UnityUtility.SetActive(popupWindow, false);

            if (isGlobal)
            {
                Instance.RegisterGlobal(popupWindow);
            }
            else
            {
                Instance.RegisterScene(popupWindow);
            }

            Instance.UpdateContents();

            return popupWindow.Open(inputProtect);
        }

        private void RegisterGlobal(Window popupWindow)
        {
            // 新規登録された場合.
            if (globalPopups.All(x => x != popupWindow))
            {
                UnityUtility.SetLayer(gameObject, popupWindow.gameObject, true);
                UnityUtility.SetParent(popupWindow.gameObject, parentGlobal.Parent);

                popupWindow.OnCloseAsObservable()
                    .Subscribe(
                        _ =>
                        {
                            globalPopups.Remove(popupWindow);
                            UpdateContents();
                        })
                    .AddTo(this);

                globalPopups.Add(popupWindow);
            }
            else
            {
                // 既に登録済みの場合は最後尾に再登録.
                globalPopups.Remove(popupWindow);
                globalPopups.Add(popupWindow);
            }
        }

        private void RegisterScene(Window popupWindow)
        {
            // 新規登録された場合.
            if (scenePopups.All(x => x != popupWindow))
            {
                UnityUtility.SetLayer(gameObject, popupWindow.gameObject, true);
                UnityUtility.SetParent(popupWindow.gameObject, parentInScene.Parent);

                popupWindow.OnCloseAsObservable()
                    .Subscribe(
                        _ =>
                        {
                            scenePopups.Remove(popupWindow);
                            UpdateContents();
                        })
                    .AddTo(this);

                scenePopups.Add(popupWindow);
            }
            else
            {
                // 既に登録済みの場合は最後尾に再登録.
                scenePopups.Remove(popupWindow);
                scenePopups.Add(popupWindow);
            }
        }

        private PopupParent CreatePopupParent(string instanceName, GameObject parent, int layer, int canvasOrder)
        {
            var popupParent = UnityUtility.Instantiate<PopupParent>(parent, parentPrefab);

            popupParent.transform.name = instanceName;
            popupParent.Canvas.sortingOrder = canvasOrder;

            UnityUtility.SetLayer(layer, popupParent.gameObject, true);

            popupParent.Canvas.worldCamera = UnityUtility.FindCameraForLayer(layer).FirstOrDefault();

            return popupParent;
        }

        public void CreateInSceneParent(GameObject sceneRoot)
        {
            if (!UnityUtility.IsNull(parentInScene))
            {
                UnityUtility.SafeDelete(parentInScene.gameObject);
            }

            var popupParent = CreatePopupParent("Popup (InScene)", sceneRoot, ParentInSceneLayer, sceneCanvasOrder);

            var ignoreControl = UnityUtility.GetOrAddComponent<IgnoreControl>(popupParent.gameObject);

            if (ignoreControl != null)
            {
                ignoreControl.Type = IgnoreControl.IgnoreType.ActiveControl;
            }

            parentInScene = popupParent;
        }

        protected void UpdateContents()
        {
            var touchBlocIndex = 0;
            GameObject parent = null;

            if (touchBloc == null)
            {
                touchBloc = UnityUtility.Instantiate<TouchBloc>(parentGlobal.Parent, touchBlocPrefab);
                touchBloc.Initialize();
            }

            if (scenePopups.Any())
            {
                var index = 0;

                parent = parentInScene.Parent;
                touchBlocIndex = 0;

                foreach (var item in scenePopups)
                {
                    if (item == scenePopups.LastOrDefault())
                    {
                        touchBlocIndex = index++;
                    }

                    item.transform.SetSiblingIndex(index++);
                }
            }

            if (globalPopups.Any())
            {
                var index = 0;

                parent = parentGlobal.Parent;
                touchBlocIndex = 0;

                foreach (var item in globalPopups)
                {
                    if (item == globalPopups.LastOrDefault())
                    {
                        touchBlocIndex = index++;
                    }

                    item.transform.SetSiblingIndex(index++);
                }
            }

            // ポップアップがなかった場合はグローバルの下に退避.
            if (parent == null)
            {
                parent = parentGlobal.Parent;
            }

            UnityUtility.SetParent(touchBloc.gameObject, parent);

            touchBloc.transform.SetSiblingIndex(touchBlocIndex);

            UnityUtility.SetLayer(parent, touchBloc.gameObject, true);

            // 一つでも登録されたら表示.
            if (!touchBloc.Active && (scenePopups.Any() || globalPopups.Any()))
            {
                if (touchBlocDisposable != null)
                {
                    touchBlocDisposable.Dispose();
                    touchBlocDisposable = null;
                }

                touchBlocDisposable = touchBloc.FadeIn().Subscribe().AddTo(this);
            }

            // 空になったら非表示.
            if (touchBloc.Active && (scenePopups.IsEmpty() && globalPopups.IsEmpty()))
            {
                if (touchBlocDisposable != null)
                {
                    touchBlocDisposable.Dispose();
                    touchBlocDisposable = null;
                }

                touchBlocDisposable = touchBloc.FadeOut().Subscribe().AddTo(this);
            }
        }

        public void Clean()
        {
            foreach (var scenePopup in scenePopups)
            {
                UnityUtility.SafeDelete(scenePopup.gameObject);
            }

            scenePopups.Clear();

            UnityUtility.SetParent(touchBloc.gameObject, parentGlobal.Parent);
            UnityUtility.SetLayer(parentGlobal.Parent, touchBloc.gameObject, true);

            if (globalPopups.IsEmpty())
            {
                if (touchBlocDisposable != null)
                {
                    touchBlocDisposable.Dispose();
                    touchBlocDisposable = null;
                }

                touchBloc.Hide();
                touchBloc.transform.SetSiblingIndex(0);
            }
        }
    }
}
