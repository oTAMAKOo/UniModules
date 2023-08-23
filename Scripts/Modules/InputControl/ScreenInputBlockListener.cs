
using Extensions;

namespace Modules.InputControl.Components
{
    public sealed class ScreenInputBlockListener : InputBlockListener
    {
        //----- params -----

        //----- field -----

		private bool blocking = false;

        //----- property -----

		protected override InputBlockType BlockType { get { return InputBlockType.Screen; } }

		public bool IsBlocking { get { return blocking; } }

        //----- method -----

		protected override void UpdateInputBlock(bool isBlock)
		{
            blocking = isBlock;

            UnityUtility.SetActive(gameObject, blocking);
		}
    }
}