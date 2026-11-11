
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Linq;
using System.Collections.Generic;
using R3;
using Extensions;
using Modules.UI;

namespace Extensions
{
    public static class ButtonExtensions
    {
        public static void SetLongPressDuration(this Button button, float duration)
        {
            var trigger = UnityUtility.GetOrAddComponent<ButtonEventTrigger>(button.gameObject);

            trigger.SetLongPressDuration(duration);
        }

        public static Observable<Unit> OnPressAsObservable(this Button button)
        {
            var trigger = UnityUtility.GetOrAddComponent<ButtonEventTrigger>(button.gameObject);

            return trigger.OnPressAsObservable();
        }

        public static Observable<float> OnReleaseAsObservable(this Button button)
        {
            var trigger = UnityUtility.GetOrAddComponent<ButtonEventTrigger>(button.gameObject);

            return trigger.OnReleaseAsObservable();
        }

        public static Observable<Unit> OnLongPressAsObservable(this Button button)
        {
            var trigger = UnityUtility.GetOrAddComponent<ButtonEventTrigger>(button.gameObject);

            return trigger.OnLongPressAsObservable();
        }

        public static Observable<float> OnLongPressReleaseAsObservable(this Button button)
        {
            var trigger = UnityUtility.GetOrAddComponent<ButtonEventTrigger>(button.gameObject);

            return trigger.OnLongPressReleaseAsObservable();
        }

        public static Observable<Unit> OnCancelAsObservable(this Button button)
        {
            var trigger = UnityUtility.GetOrAddComponent<ButtonEventTrigger>(button.gameObject);

            return trigger.OnCancelAsObservable();
        }
    }
}