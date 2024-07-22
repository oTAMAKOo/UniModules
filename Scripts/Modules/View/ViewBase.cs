
using UnityEngine;

namespace Modules.View
{
    public abstract class ViewBase<TViewModel> : MonoBehaviour, IView<TViewModel> where TViewModel : ViewModel
    {
        //----- params -----

        //----- field -----

        //----- property -----

        protected TViewModel ViewModel
        {
            get { return this.GetViewModel(); }
        }

        //----- method -----
    }
}
