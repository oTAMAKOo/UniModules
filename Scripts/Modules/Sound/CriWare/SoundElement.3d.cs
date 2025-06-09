
#if (ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_ADX_LE) && ENABLE_CRIWARE_POS3D

using UnityEngine;
using System;
using CriWare;
using UniRx;
using Extensions;

namespace Modules.Sound
{
    public sealed partial class SoundElement
    {
        //----- params -----

        //----- field -----

        private CriAtomEx3dSource source3d = null;

        //----- property -----

        //----- method -----

        public CriAtomEx3dSource Get3dSource()
        {
            return source3d;
        }

        public void Set3dSource(CriAtomEx3dSource source)
        {
            source3d = source;

            player.Set3dSource(source3d);
        }

        public void SetPosition(Vector3 position)
        {
            if (source3d == null){ return; }

            source3d.SetPosition(position.x, position.y, position.z);
        }
    }
}

#endif