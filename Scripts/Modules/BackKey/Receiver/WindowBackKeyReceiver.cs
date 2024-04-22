
using Cysharp.Threading.Tasks;
using Extensions;
using Modules.Window;

namespace Modules.BackKey
{
    public abstract class WindowBackKeyReceiver : BackKeyReceiver
    {
        //----- params -----

        //----- field -----

        private Modules.Window.Window window = null;

        //----- property -----

        //----- method -----

        protected override void OnInitialize()
        {
            window = UnityUtility.GetComponent<Modules.Window.Window>(gameObject);
        }

        protected override void HandleBackKey()
        {
            if (window == null) { return; }

            var popupManager = GetPopupManager();

            if (popupManager.Current != window){ return; }

            window.Close().Forget();
        }

        public abstract IPopupManager GetPopupManager();
    }
}