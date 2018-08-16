
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;

namespace Modules.SceneManagement
{
    /// <summary> シーンの読込、解放などのイベントを受け取る為のインターフェース </summary>
	public interface ISceneEvent
	{
        /// <summary> シーン読込時 </summary>
        IObservable<Unit> OnLoadSceneAsObservable();
        /// <summary> シーン解放時 </summary>
        IObservable<Unit> OnUnloadSceneAsObservable();
    }
}
