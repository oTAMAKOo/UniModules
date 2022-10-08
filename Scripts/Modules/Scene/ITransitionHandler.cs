
using Cysharp.Threading.Tasks;

namespace Modules.Scene
{
    public interface ITransitionHandler
    {
		UniTask<bool> HandleTransition();
    }
}