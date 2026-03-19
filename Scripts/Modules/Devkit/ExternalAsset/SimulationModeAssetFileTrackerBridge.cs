
using System;
using R3;

namespace Modules.Devkit.ExternalAssets
{
    public static class SimulationModeAssetFileTrackerBridge
    {
		//----- params -----

		//----- field -----

		private static Subject<bool> onRequestStatusChange = null;

        //----- property -----

        //----- method -----

		public static void SetRecordingStatus(bool status)
		{
			if (onRequestStatusChange != null)
			{
				onRequestStatusChange.OnNext(status);
			}
		}

		public static Observable<bool> OnRequestStatusChangeAsObservable()
		{
			return onRequestStatusChange ?? (onRequestStatusChange = new Subject<bool>());
		}
    }
}