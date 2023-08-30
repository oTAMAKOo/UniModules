
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;
using Extensions;
using Modules.Scene;

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

        protected PopupParent parentInScene = null;
        protected PopupParent parentGlobal = null;

        protected TouchBloc touchBloc = null;
        protected CancellationTokenSource cancelTokenSource = null;

        // 登録されているポップアップ.
        protected List<Window> scenePopups = new List<Window>();
        protected List<Window> globalPopups = new List<Window>();

        private Subject<Unit> onBlocTouch = null;

        //----- property -----

        public GameObject ParentInScene { get { return parentInScene != null ? parentInScene.Parent : null; } }

        public GameObject ParentGlobal { get { return parentGlobal != null ? parentGlobal.Parent : null; } }

        public IReadOnlyList<Window> ScenePopups { get { return scenePopups; } }

        public IReadOnlyList<Window> GlobalPopups { get { return globalPopups; } }

        //----- method -----

        protected abstract int ParentInSceneLayer { get; }

        protected abstract int ParentGlobalLayer { get; }

        public virtual void Initialize()
        {
            parentGlobal = CreatePopupParent("Popup (Global)", null, ParentGlobalLayer, globalCanvasOrder);

            DontDestroyOnLoad(parentGlobal);

            UpdateContents();
        }

        /// <summary> ポップアップを開く </summary>
        public static async UniTask Open(Window popupWindow, bool isGlobal = false, bool inputProtect = true)
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

            await popupWindow.Open(inputProtect);
        }

        private void RegisterGlobal(Window popupWindow)
        {
            // 新規登録された場合.
            if (globalPopups.All(x => x != popupWindow))
            {
                UnityUtility.SetLayer(parentGlobal.Parent, popupWindow.gameObject, true);
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

            globalPopups = globalPopups.OrderBy(x => x.DisplayPriority).ToList();
        }

        private void RegisterScene(Window popupWindow)
        {
            // 新規登録された場合.
            if (scenePopups.All(x => x != popupWindow))
            {
                UnityUtility.SetLayer(parentInScene.Parent, popupWindow.gameObject, true);
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

            scenePopups = scenePopups.OrderBy(x => x.DisplayPriority).ToList();
        }

        private PopupParent CreatePopupParent(string instanceName, GameObject parent, int layer, int canvasOrder)
        {
            var popupParent = UnityUtility.Instantiate<PopupParent>(parent, parentPrefab);

            popupParent.transform.name = instanceName;
            popupParent.Canvas.sortingOrder = canvasOrder;

            UnityUtility.SetLayer(layer, popupParent.gameObject, true);

            popupParent.Canvas.worldCamera = UnityUtility.FindCameraForLayer(1 << layer).FirstOrDefault();

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

        private void CreateTouchBloc()
        {
            if (touchBloc != null){ return; }

            touchBloc = UnityUtility.Instantiate<TouchBloc>(parentGlobal.Parent, touchBlocPrefab);

            touchBloc.Initialize();

            touchBloc.OnBlocTouchAsObservable()
                .Subscribe(_ =>
                   {
                       if (onBlocTouch != null)
                       {
                           onBlocTouch.OnNext(Unit.Default);
                       }
                   })
                .AddTo(this);
        }

        protected void UpdateContents()
        {
            var touchBlocIndex = 0;
            GameObject parent = null;

            CreateTouchBloc();

            if (scenePopups.Any())
            {
                var index = 0;

                parent = parentInScene.Parent;
                touchBlocIndex = 0;
                
                var lastPopup = scenePopups.LastOrDefault();

                foreach (var popup in scenePopups)
                {
                    if (popup == lastPopup)
                    {
                        touchBlocIndex = index++;
                    }

                    popup.transform.SetSiblingIndex(index++);
                }
            }

            if (globalPopups.Any())
            {
                var index = 0;

                parent = parentGlobal.Parent;
                touchBlocIndex = 0;
                
                var lastPopup = globalPopups.LastOrDefault();

                foreach (var popup in globalPopups)
                {
                    if (popup == lastPopup)
                    {
                        touchBlocIndex = index++;
                    }

                    popup.transform.SetSiblingIndex(index++);
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
            if (scenePopups.Any() || globalPopups.Any())
            {
                if (cancelTokenSource != null)
                {
                    cancelTokenSource.Cancel();
                }

                UnityUtility.SetActive(touchBloc, true);

                cancelTokenSource = new CancellationTokenSource();

                touchBloc.FadeIn(cancelTokenSource.Token).Forget();
            }

            // 空になったら非表示.
            if (scenePopups.IsEmpty() && globalPopups.IsEmpty())
            {
                if (cancelTokenSource != null)
                {
                    cancelTokenSource.Cancel();
                }

                cancelTokenSource = new CancellationTokenSource();

                touchBloc.FadeOut(cancelTokenSource.Token).Forget();
            }
        }

        public void Clean()
        {
            foreach (var scenePopup in scenePopups)
            {
                UnityUtility.DeleteGameObject(scenePopup);
            }

            scenePopups.Clear();

            UnityUtility.SetParent(touchBloc.gameObject, parentGlobal.Parent);
            UnityUtility.SetLayer(parentGlobal.Parent, touchBloc.gameObject, true);

            if (globalPopups.IsEmpty())
            {
                if (cancelTokenSource != null)
                {
                    cancelTokenSource.Cancel();
                    cancelTokenSource = null;
                }

                touchBloc.Hide();
                touchBloc.transform.SetSiblingIndex(0);
            }
        }

        public Window GetCurrentWindow()
        {
            if (globalPopups.Any())
            {
                return globalPopups.FirstOrDefault();
            }

            if (scenePopups.Any())
            {
                return scenePopups.FirstOrDefault();
            }

            return null;
        }

        public IObservable<Unit> OnBlocTouchAsObservable()
        {
            return onBlocTouch ?? (onBlocTouch = new Subject<Unit>());
        }
    }
}
