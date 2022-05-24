
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

		protected abstract T CurrentLanguage { get; }

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