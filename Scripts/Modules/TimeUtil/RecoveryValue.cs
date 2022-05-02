
using System;

namespace Modules.TimeUtil
{
    /// <summary>
    /// 時間経過で回復していく値を管理するクラス.
    /// </summary>
    public sealed class RecoveryValue
    {
        //----- params -----

        //----- field -----

        // 現在の値.
        private double current = 0;

        // 次に回復するまでの時間.
        private double nextRecoveryTime = 0;

        //----- property -----

        /// <summary> 現在の値 </summary>
        public double Current
        {
            get { return current; }

            private set
            {
                current = value;

                if (Max <= current)
                {
                    current = Max;
                }
            }
        }

        /// <summary> 回復するのに必要な時間[秒] </summary>
        public double RecoveryInterval { get; private set; }
        /// <summary> 回復量 </summary>
        public double RecoveryAmount { get; private set; }
        /// <summary> 最後に回復した時間 </summary>
        public DateTime? LastRecoveryTime { get; private set; }
        /// <summary> 全回復時間 </summary>
        public DateTime? FullRecoveryTime { get; private set; }
        /// <summary> 最大値 </summary>
        public double Max { get; private set; }
        /// <summary> 最大か </summary>
        public bool IsMax { get { return Max <= Current; } }

        //----- method -----

        /// <summary>
        /// 初期化.
        /// </summary>
        /// <param name="max">最大値</param>
        /// <param name="recoveryInterval">回復するのに必要な時間</param>
        /// <param name="recoveryAmount">回復量</param>
        /// <param name="lastRecoveryTime">最後に回復した時間</param>
        /// <param name="fullRecoveryTime">全回復時間</param>
        public RecoveryValue(float max, float recoveryInterval, float recoveryAmount, DateTime? lastRecoveryTime, DateTime? fullRecoveryTime)
        {
            Max = max;
            RecoveryInterval = recoveryInterval;
            RecoveryAmount = recoveryAmount;
            LastRecoveryTime = lastRecoveryTime;
            FullRecoveryTime = fullRecoveryTime;

            if (FullRecoveryTime.HasValue && LastRecoveryTime.HasValue)
            {
                // 回復に掛かる時間.
                var recoveryTimeSpan = FullRecoveryTime.Value - LastRecoveryTime.Value;

                // 回復に掛かる時間から現在値を算出.
                Current = Max - recoveryTimeSpan.TotalSeconds / RecoveryInterval * RecoveryAmount;
            }
            else
            {
                Current = Max;
            }
        }

        /// <summary> 次に回復する時間までの残り時間. </summary>
        public TimeSpan GetNextRecoveryTime()
        {
            if (Max <= Current) { return TimeSpan.Zero; }

            return TimeSpan.FromSeconds(nextRecoveryTime);
        }

        /// <summary> 全体に対しての現在の割合(0～1). </summary>
        public double GetRatio()
        {
            if (Max <= Current) { return 1f; }

            if (Current == 0) { return 0f; }

            return Current / Max;
        }

        /// <summary> 更新. </summary>
        public void UpdateTime(DateTime currentTime)
        {
            if (Max <= Current)
            {
                Current = Max;
                return;
            }

            if (!LastRecoveryTime.HasValue)
            {
                Current = Max;
                return;
            }

            var diff = currentTime - LastRecoveryTime.Value;

            var totalSeconds = diff.TotalSeconds;

            while (totalSeconds > RecoveryInterval)
            {
                if (Max <= Current)
                {
                    Current = Max;
                    break;
                }

                totalSeconds -= RecoveryInterval;

                LastRecoveryTime = LastRecoveryTime.Value.Add(TimeSpan.FromSeconds(RecoveryInterval));

                Current += RecoveryAmount;           
            }

            nextRecoveryTime = RecoveryInterval - totalSeconds;
        }
    }
}
