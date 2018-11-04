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

        private GameObject parentInScene = null;
        private GameObject parentGlobal = null;
        private GameObject touchBloc = null;

        // 登録されているポップアップ.
        protected List<Window> scenePopups = new List<Window>();
        protected List<Window> globalPopups = new List<Window>();

        //----- property -----

        public GameObject ParentInScene { get { return parentInScene; } }
        public GameObject ParentGlobal { get { return parentGlobal; } }

        //----- method -----

        public virtual void Initialize()
        {
            var popupParent = UnityUtility.Instantiate<PopupParent>(gameObject, parentPrefab);

            popupParent.transform.name = "Popup (Global)";
            popupParent.Canvas.sortingOrder = globalCanvasOrder;

            parentGlobal = popupParent.Parent;

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
                UnityUtility.SetParent(popupWindow.gameObject, parentGlobal);

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
                UnityUtility.SetParent(popupWindow.gameObject, parentInScene);

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

        public void CreateInSceneParent(GameObject sceneRoot)
        {
            if (!UnityUtility.IsNull(parentInScene))
            {
                UnityUtility.SafeDelete(parentInScene);
            }

            var popupParent = UnityUtility.Instantiate<PopupParent>(sceneRoot, parentPrefab);

            popupParent.transform.name = "Popup (InScene)";
            popupParent.Canvas.sortingOrder = sceneCanvasOrder;

            var ignoreControl = UnityUtility.GetOrAddComponent<IgnoreControl>(popupParent.gameObject);

            if (ignoreControl != null)
            {
                ignoreControl.Type = IgnoreControl.IgnoreType.ActiveControl;
            }

            parentInScene = popupParent.Parent;
        }

        protected void UpdateContents()
        {
            var touchBlocIndex = 0;
            GameObject parent = null;

            if (touchBloc == null)
            {
                touchBloc = UnityUtility.Instantiate(parentGlobal, touchBlocPrefab);
            }

            if (scenePopups.Any())
            {
                var index = 0;

                parent = parentInScene;
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

                parent = parentGlobal;
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
                parent = parentGlobal;
            }

            UnityUtility.SetParent(touchBloc, parent);

            touchBloc.transform.SetSiblingIndex(touchBlocIndex);

            UnityUtility.SetLayer(parent, touchBloc, true);

            UnityUtility.SetActive(touchBloc, scenePopups.Any() || globalPopups.Any());
        }

        public void Clean()
        {
            foreach (var scenePopup in scenePopups)
            {
                UnityUtility.SafeDelete(scenePopup.gameObject);
            }

            scenePopups.Clear();

            UnityUtility.SetParent(touchBloc, parentGlobal);
            UnityUtility.SetLayer(parentGlobal, touchBloc, true);

            if (globalPopups.IsEmpty())
            {
                UnityUtility.SetActive(touchBloc, false);

                touchBloc.transform.SetSiblingIndex(0);
            }
        }
    }
}
