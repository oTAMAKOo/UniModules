
using UnityEngine;
using UnityEngine.UI;
using Unity.Linq;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Utage;

namespace Modules.UtageExtension
{
	public abstract class ExtendGraphicManager : MonoBehaviour
	{
        //----- params -----

        //----- field -----

        [SerializeField]
        private Camera canvasCamera = null;

        protected Canvas canvas = null;

        //----- property -----

        protected abstract Dictionary<string, Type> CustomTypeTable { get; }

        //----- method -----

        protected virtual void Awake()
        {
            AdvGraphicInfo.CallbackCreateCustom += OnCreateCustomGraphicObject;

            canvas = UnityUtility.GetOrAddComponent<Canvas>(gameObject);

            if (canvas != null)
            {
                canvas.worldCamera = canvasCamera;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
            }
        }

        // 独自オブジェクトを作成するためのコールバック.
        private void OnCreateCustomGraphicObject(string fileType, ref Type type)
        {
            type = CustomTypeTable.GetValueOrDefault(fileType);
        }

        void OnTransformChildrenChanged()
        {
            var childrenCanvas = gameObject.Descendants().OfComponent<Canvas>().ToArray();

            foreach (var canvas in childrenCanvas)
            {
                canvas.overrideSorting = true;
            }
        }
    }
}
