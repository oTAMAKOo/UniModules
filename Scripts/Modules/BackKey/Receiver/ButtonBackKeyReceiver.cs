
using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Linq;
using Extensions;
using Modules.UI.Extension;

namespace Modules.BackKey
{
    [RequireComponent(typeof(UIButton))]
    public class ButtonBackKeyReceiver : BackKeyReceiver
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

        public override bool HandleBackKey()
        {
            if (button == null) { return false; }

            if (!button.interactable){ return false; }

            // 当たり判定設定なし.
            if (button.Button.targetGraphic == null){ return false; }

            var result = EmulateButtonEvent();

            return result;
        }

        private bool EmulateButtonEvent()
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
            
            if (validGameObject == null) { return false; }

            // ボタン位置にあるGameObjectからIPointerDownHandlerを保持しているGameObjectを取得.
            var currentPointerDownHandlerObject = ExecuteEvents.GetEventHandler<IPointerDownHandler>(validGameObject);

            // ボタン位置から得られたGameObjectとボタンのGameObjectが異なる = 別のもので遮られている ので処理しない.
            if (currentPointerDownHandlerObject != button.gameObject) { return false; }

            pointerEventData.pointerPress = currentPointerDownHandlerObject;
            
            ExecuteEvents.Execute(currentPointerDownHandlerObject, pointerEventData, ExecuteEvents.pointerDownHandler);

            //======================================
            // Release
            //======================================

            ExecuteEvents.Execute(pointerEventData.pointerPress, pointerEventData, ExecuteEvents.pointerUpHandler);

            ExecuteEvents.Execute(pointerEventData.pointerPress, pointerEventData, ExecuteEvents.pointerClickHandler);

            return true;
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
    }
}