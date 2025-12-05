
#if  ENABLE_CRIWARE_SOFDEC

using CriWare;

namespace Modules.Movie
{
	public sealed class CriMovieForUI : CriManaMovieControllerForUI
	{
		//----- params -----

		//----- field -----

		//----- property -----

		public bool Initialized { get; set; } = false;

		//----- method -----

		protected override void Awake()
		{
			uiRenderMode = true;

			if (Initialized){ return; }

			base.Awake();

			Initialized = true;
		}

		public void ManualInitialize()
		{
            if (Initialized){ return; }

			PlayerManualInitialize();

			Initialized = true;
		}
	}
}

#endif
