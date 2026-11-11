
#if ENABLE_DEVICE_KEY

using UnityEngine;
using System;

namespace Modules.DeviceKey
{
    /// <summary>
    /// 端末のセキュリティチップ（iOS: Secure Enclave / Android: Keystore の TEE）に P-256 の EC 鍵を作り、
    /// 秘密鍵を外に出さずに ECDSA（SHA-256）の署名を作る.
    /// エディタ・その他の環境は対応せず、鍵を作れない.
    /// メインスレッドから呼ぶ.
    /// </summary>
    public static partial class DeviceKey
    {
        //----- params -----

        private interface IKeyStore
        {
            bool IsSupported { get; }

            bool HasKey(string alias);

            bool CreateKey(string alias);

            byte[] GetPublicKey(string alias);

            byte[] Sign(string alias, byte[] data);

            bool DeleteKey(string alias);
        }

        private const string DefaultKeyName = "Default";

        private const string AliasPrefix = "UniModules.DeviceKey.";

        //----- field -----

        private static string keyName = DefaultKeyName;

        private static IKeyStore keyStore = null;

        //----- property -----

        /// <summary> 鍵の名前（同じ名前の鍵を 1 つ持つ） </summary>
        public static string KeyName { get { return keyName; } }

        /// <summary> この環境で鍵を作れるか </summary>
        public static bool IsSupported { get { return GetKeyStore().IsSupported; } }

        //----- method -----

        /// <summary> 鍵の名前を設定する（ほかの呼び出しより先に呼ぶ） </summary>
        public static void SetKeyName(string name)
        {
            if (string.IsNullOrEmpty(name)){ throw new ArgumentException("Key name is null or empty."); }

            keyName = name;
        }

        public static bool HasKey()
        {
            return GetKeyStore().HasKey(GetAlias());
        }

        /// <summary> 鍵を作る（同じ名前の鍵があれば削除して作り直す） </summary>
        public static bool CreateKey()
        {
            return GetKeyStore().CreateKey(GetAlias());
        }

        /// <summary> 公開鍵を X.509 SubjectPublicKeyInfo（DER）で返す. 鍵が無いときは null </summary>
        public static byte[] GetPublicKey()
        {
            return GetKeyStore().GetPublicKey(GetAlias());
        }

        /// <summary> データの ECDSA（P-256 / SHA-256）署名を DER で返す. 鍵が無い・失敗したときは null </summary>
        public static byte[] Sign(byte[] data)
        {
            if (data == null){ throw new ArgumentNullException("data"); }

            return GetKeyStore().Sign(GetAlias(), data);
        }

        public static bool DeleteKey()
        {
            return GetKeyStore().DeleteKey(GetAlias());
        }

        private static string GetAlias()
        {
            return AliasPrefix + keyName;
        }

        private static IKeyStore GetKeyStore()
        {
            if (keyStore == null)
            {
                #if UNITY_IOS && !UNITY_EDITOR

                keyStore = new SecureEnclaveKeyStore();

                #elif UNITY_ANDROID && !UNITY_EDITOR

                keyStore = new AndroidKeyStore();

                #else

                keyStore = new UnsupportedKeyStore();

                #endif
            }

            return keyStore;
        }
    }
}

#endif
