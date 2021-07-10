
using UnityEngine;
using Unity.Linq;
using System.Linq;
using Extensions;

namespace Modules.View
{
    public interface IViewRoot
    {
        ViewModel GetViewModel();
    }

    public abstract class ViewBase<TViewModel> : MonoBehaviour where TViewModel : ViewModel, new()
    {
        //----- params -----

        //----- field -----

        private TViewModel viewModel = null;

        //----- property -----

        protected TViewModel ViewModel
        {
            get { return GetViewModel(); }
        }

        //----- method -----

        private TViewModel GetViewModel()
        {
            if (viewModel != null && viewModel.IsDisposed)
            {
                viewModel = null;
            }

            if (viewModel == null)
            {
                var viewRoot = gameObject.AncestorsAndSelf()
                    .Select(x => UnityUtility.GetInterface<IViewRoot>(x))
                    .FirstOrDefault(x => x != null);

                if (viewRoot != null)
                {
                    viewModel = viewRoot.GetViewModel() as TViewModel;
                }
                else
                {
                    Debug.LogError("IViewRoot interface not found in ancestors hierarchy.");
                }
            }

            return viewModel;
        }

        public void SetViewModel(TViewModel viewModel)
        {
            this.viewModel = viewModel;
        }
    }
}
