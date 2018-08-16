
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;

namespace Modules.UI
{
	public interface IVirtualScrollExtension
	{
        /// <summary> リストアイテムが生成された </summary>
        IObservable<Unit> OnCreateItem(GameObject item);

        /// <summary> リストアイテムが初期化された </summary>
        IObservable<Unit> OnItemInitialize(GameObject item);

        /// <summary>  </summary>
        IObservable<Unit> OnUpdateContents(GameObject[] items);
	}
}
