
using Cysharp.Threading.Tasks;

namespace Modules.SceneManagement
{
    /// <summary> シーンの読込、解放などのイベントを受け取る為のインターフェース </summary>
	public interface ISceneEvent
	{
        /// <summary> シーン読込時 </summary>
        UniTask OnLoadSceneAsObservable();

        /// <summary> シーン解放時 </summary>
        UniTask OnUnloadSceneAsObservable();
    }
}
