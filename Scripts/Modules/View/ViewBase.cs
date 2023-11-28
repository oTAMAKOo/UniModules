
using UnityEngine;

namespace Modules.View
{
    public abstract class ViewBase<TViewModel> : MonoBehaviour, IView<TViewModel> where TViewModel : ViewModel, new()
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
