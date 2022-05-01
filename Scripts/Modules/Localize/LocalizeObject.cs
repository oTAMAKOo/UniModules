
using UnityEngine;
using System;
using Extensions;

namespace Modules.Localize
{
    public abstract class LocalizeObject<T> : MonoBehaviour where T : Enum
    {
        //----- params -----

        //----- field -----

		[SerializeField]
		private T language = default;

        //----- property -----

		public static T CurrentLanguage { get; set; }

		//----- method -----

		void OnEnable()
		{
			if (!language.Equals(CurrentLanguage))
			{
				UnityUtility.SetActive(gameObject, false);
			}
		}
    }
}