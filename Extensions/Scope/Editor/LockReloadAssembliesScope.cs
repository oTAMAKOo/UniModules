
using UnityEditor;

namespace Extensions
{
    public sealed class LockReloadAssembliesScope : Scope
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

        public LockReloadAssembliesScope()
        {
            EditorApplication.LockReloadAssemblies();
        }

        protected override void CloseScope()
        {
            EditorApplication.UnlockReloadAssemblies();
        }
    }
}
