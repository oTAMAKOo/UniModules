
#if ENABLE_DEVICE_KEY && (UNITY_EDITOR || !(UNITY_IOS || UNITY_ANDROID))

using UnityEngine;

namespace Modules.DeviceKey
{
    public static partial class DeviceKey
    {
        /// <summary> 鍵を作れない環境で、鍵が無い状態として振る舞う </summary>
        private sealed class UnsupportedKeyStore : IKeyStore
        {
            //----- params -----

            //----- field -----

            private bool warned = false;

            //----- property -----

            public bool IsSupported { get { return false; } }

            //----- method -----

            public bool HasKey(string alias)
            {
                return false;
            }

            public bool CreateKey(string alias)
            {
                if (!warned)
                {
                    Debug.LogWarning("DeviceKey is not supported on this platform.");

                    warned = true;
                }

                return false;
            }

            public byte[] GetPublicKey(string alias)
            {
                return null;
            }

            public byte[] Sign(string alias, byte[] data)
            {
                return null;
            }

            public bool DeleteKey(string alias)
            {
                return true;
            }
        }
    }
}

#endif
