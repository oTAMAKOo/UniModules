
using System.Collections.Generic;
using Modules.View;

namespace Modules.Scene
{
	public abstract partial class SceneManager<TInstance, TScenes>
	{
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

		/// <summary> ロード済みの指定したシーンのViewModelを取得. </summary>
        public TViewModel GetViewModel<TViewModel>(TScenes scene) where TViewModel : ViewModel
        {
			var sceneInstance = loadedScenes.GetValueOrDefault(scene);

			if (sceneInstance == null){ return null; }

			var viewRoot = sceneInstance.Instance as IViewRoot;
            
			return viewRoot != null ? viewRoot.GetViewModel() as TViewModel : null;
		}

		/// <summary> ロード済みの指定したシーンのViewModelを取得. </summary>
		public TViewModel GetViewModel<TViewModel>() where TViewModel : ViewModel
		{
			TViewModel viewModel = null;

			foreach (var scene in loadedScenes.Values)
			{
				var viewRoot = scene.Instance as IViewRoot;

				if (viewRoot == null){ continue; }

				viewModel = viewRoot.GetViewModel() as TViewModel;

				if (viewModel != null){ break; }
			}

			return viewModel;
		}
    }
}