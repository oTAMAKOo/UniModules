
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

            Priority = 10000;
        }

        public override bool HandleBackKey()
        {
            if (window == null) { return false; }

            var popupManager = GetPopupManager();

            if (popupManager.Current != window){ return false; }

            window.Close().Forget();

            return true;
        }

        public abstract IPopupManager GetPopupManager();
    }
}