
using UnityEngine;
using UniRx;
using Extensions;

namespace Modules.View
{
    public abstract class ViewBase<TViewModel> : MonoBehaviour, IView<TViewModel> where TViewModel : ViewModel
    {
        //----- params -----

        //----- field -----

        private TViewModel viewModel = null;

        //----- property -----

        protected TViewModel ViewModel
        {
            get
            {
                if (viewModel == null)
                {
                    viewModel = this.GetViewModel();

                    if (viewModel != null)
                    {
                        viewModel.OnDisposeAsObservable()
                            .Where(_ => !UnityUtility.IsNull(gameObject))
                            .Subscribe(_ => viewModel = null)
                            .AddTo(this);
                    }
                }

                return viewModel;
            }
        }

        //----- method -----

        public void ClearViewModelCache()
        {
            viewModel = null;
        }
    }
}
