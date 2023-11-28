
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using Unity.Linq;
using Extensions;

namespace Modules.View
{
    public interface IViewRoot
    {
        ViewModel GetViewModel();
    }

    public interface IView<TViewModel> where TViewModel : ViewModel, new(){  }

    public static class ViewExtensions
    {
        private const int CacheRefreshFrameInterval = 20;

        private static int cacheRefreshFrameCount = 0;

        private static Dictionary<MonoBehaviour, ViewModel> viewModelCache = null;

        public static TViewModel GetViewModel<TViewModel>(this IView<TViewModel> view) where TViewModel : ViewModel, new()
        {
            if (viewModelCache == null)
            {
                viewModelCache = new Dictionary<MonoBehaviour, ViewModel>();
            }

            CacheRefresh();

            var monoBehaviour = view as MonoBehaviour;

            if (monoBehaviour == null) { return null; }

            var viewModel = viewModelCache.GetValueOrDefault(monoBehaviour);

            if (viewModel != null)
            {
                if (viewModel is TViewModel) { return (TViewModel)viewModel; }
            }

            if (viewModel != null && viewModel.IsDisposed)
            {
                viewModel = null;
            }

            if (viewModel == null)
            {
                var viewRoot = monoBehaviour.gameObject.AncestorsAndSelf()
                    .Select(x => UnityUtility.GetInterface<IViewRoot>(x))
                    .FirstOrDefault(x => x != null);

                if (viewRoot != null)
                {
                    viewModel = viewRoot.GetViewModel() as TViewModel;

                    viewModelCache[monoBehaviour] = viewModel;
                }
                else
                {
                    throw new Exception("IViewRoot interface not found in ancestors hierarchy.");
                }
            }

            return viewModel as TViewModel;
        }

        private static void CacheRefresh()
        {
            var frameCount = Time.frameCount;

            if (frameCount < cacheRefreshFrameCount + CacheRefreshFrameInterval){ return; }

            cacheRefreshFrameCount = frameCount;

            viewModelCache = viewModelCache
                .Where(x => !UnityUtility.IsNull(x.Key))
                .ToDictionary(x => x.Key, x => x.Value);
        }
    }
}