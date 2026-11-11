
using UnityEngine;
using Extensions;

namespace Modules.Animation
{
    public enum State
    {
        Play,
        Pause,
        Stop,
    }

    public enum EndActionType
    {
        None,
        Destroy,
        Deactivate,
        Loop
    }
}
