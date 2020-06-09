
using UnityEngine;
using Extensions;

namespace Modules.UI.Extension
{
    [ExecuteInEditMode]
    public abstract class UIComponentBehaviour : MonoBehaviour
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----        
    }

    public abstract class UIComponent<T> : UIComponentBehaviour where T : Component
    {
        //----- params -----

        //----- field -----

        private T targetComponent = null;

        //----- property -----

        public T component
        {
            get
            {
                if (targetComponent == null)
                {
                    targetComponent = UnityUtility.GetComponent<T>(gameObject);
                }

                return targetComponent;
            }
        }

        //----- method -----        
    }
}
