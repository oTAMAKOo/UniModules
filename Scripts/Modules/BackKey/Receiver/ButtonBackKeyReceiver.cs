
using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Linq;
using Extensions;
using Modules.UI.Extension;
using Modules.Window;

namespace Modules.BackKey
{
    [RequireComponent(typeof(UIButton))]
    public abstract class ButtonBackKeyReceiver : BackKeyReceiver
    {
        //----- params -----

        //----- field -----

        private UIButton button = null;

        //----- property -----

        //----- method -----

        protected override void OnInitialize()
        {
            button = UnityUtility.GetComponent<UIButton>(gameObject);
        }

        protected override void HandleBackKey()
        {
            if (button == null) { return; }

            if (!button.interactable){ return; }

            var popupManager = GetPopupManager();

            // ポップアップ表示中はそちらを優先. 
            if (popupManager.Current != null){ return; }

            // 当たり判定設定なし.
            if (button.Button.targetGraphic == null){ return; }

            EmulateButtonEvent();
        }

        private static RaycastResult[] GetRaycastObjects(Vector3 position)
        {
            var eventSystem = EventSystem.current;

            if (eventSystem == null) { return new RaycastResult[0]; }

            var pointer = new PointerEventData(eventSystem);

            pointer.position = position;

            var raycastResults = new List<RaycastResult>();

            eventSystem.RaycastAll(pointer, raycastResults);
            
            return raycastResults.OrderByDescending(x => x.depth).ToArray();
        }

        private void EmulateButtonEvent()
        {
            var pointerEventData = new PointerEventData(EventSystem.current)
            {
                button = PointerEventData.InputButton.Left,
                position = button.transform.position,
            };

            var camera = button.Button.targetGraphic.canvas.worldCamera;

            var screenPosition = RectTransformUtility.WorldToScreenPoint(camera, button.transform.position);

            var raycastResults = GetRaycastObjects(screenPosition);

            //======================================
            // Press
            //======================================

            // 一番最初にぶつかっている有効なGameObject取得.
            var validGameObject = raycastResults.Select(result => result.gameObject).FirstOrDefault(go => go != null);
            
            if (validGameObject == null) { return; }

            // ボタン位置にあるGameObjectからIPointerDownHandlerを保持しているGameObjectを取得.
            var currentPointerDownHandlerObject = ExecuteEvents.GetEventHandler<IPointerDownHandler>(validGameObject);

            // ボタン位置から得られたGameObjectとボタンのGameObjectが異なる = 別のもので遮られている ので処理しない.
            if (currentPointerDownHandlerObject != button.gameObject) { return; }

            pointerEventData.pointerPress = currentPointerDownHandlerObject;
            
            ExecuteEvents.Execute(currentPointerDownHandlerObject, pointerEventData, ExecuteEvents.pointerDownHandler);

            //======================================
            // Release
            //======================================

            ExecuteEvents.Execute(pointerEventData.pointerPress, pointerEventData, ExecuteEvents.pointerUpHandler);

            ExecuteEvents.Execute(pointerEventData.pointerPress, pointerEventData, ExecuteEvents.pointerClickHandler);
        }

        public abstract IPopupManager GetPopupManager();
    }
}