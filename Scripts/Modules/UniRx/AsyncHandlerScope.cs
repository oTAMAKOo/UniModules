
using System;
using Extensions;

namespace Modules.UniRxExtension
{
    public sealed class AsyncHandlerScope : Scope
    {
        //----- params -----

        //----- field -----

        private AsyncHandler asyncHandler = null;

        //----- property -----

        //----- method -----

        public AsyncHandlerScope(AsyncHandler asyncHandler)
        {
            this.asyncHandler = asyncHandler;

            if (asyncHandler != null)
            {
                asyncHandler.Begin();
            }
        }

        protected override void CloseScope()
        {
            if (asyncHandler != null)
            {
                asyncHandler.End();
            }

            asyncHandler = null;
        }
    }
}