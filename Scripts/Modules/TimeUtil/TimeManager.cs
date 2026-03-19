
using UnityEngine;
using System;
using Extensions;
using R3;

namespace Modules.TimeUtil
{
    public abstract class TimeManager<TInstance> : Singleton<TInstance> where TInstance : TimeManager<TInstance>
    {
        //----- params -----

        //----- field -----

		private DateTime baseTime;

		private float startTime = 0f;

        //----- property -----

		public DateTime Now
		{
			get { return GetNowTime(); }
		}

		//----- method -----

		public void Set(DateTime baseTime)
		{
			this.baseTime = baseTime;

			this.startTime = Time.realtimeSinceStartup;
		}

		protected DateTime GetNowTime()
		{
			return baseTime.AddSeconds(Time.realtimeSinceStartup - startTime);
		}

		/// <summary>
		/// 指定された時刻にイベントを発行.
		/// </summary>
		public Observable<Unit> Notice(DateTime noticeTime)
		{
			return Observable.EveryUpdate()
				.SkipWhile(_ => Now < noticeTime)
				.FirstAsync()
				.Select(_ => Unit.Default);
		}
	}
}