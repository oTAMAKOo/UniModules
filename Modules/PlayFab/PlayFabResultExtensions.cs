
#if ENABLE_PLAYFAB_CSHARP

using PlayFab;
using PlayFab.Internal;

namespace Modules.PlayFabCSharp
{
    public static class PlayFabResultExtensions
    {
        public static bool HasError<TResult>(this PlayFabResult<TResult> self) where TResult : PlayFabResultCommon
        {
            return self.Error != null || self.Result == null;
        }
    }
}

#endif