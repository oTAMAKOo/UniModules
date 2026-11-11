package com.unimodules.devicekey;

import android.security.keystore.KeyGenParameterSpec;
import android.security.keystore.KeyProperties;
import android.util.Base64;
import android.util.Log;

import java.security.Key;
import java.security.KeyPairGenerator;
import java.security.KeyStore;
import java.security.PrivateKey;
import java.security.Signature;
import java.security.cert.Certificate;
import java.security.spec.ECGenParameterSpec;

/**
 * AndroidKeyStore に P-256 の EC 鍵を作り、秘密鍵を外に出さずに ECDSA（SHA-256）の署名を作る.
 * 値は Base64 文字列で受け渡す. 失敗しても例外は投げず、createKey / deleteKey は false、getPublicKey / sign は null を返す.
 */
public final class DeviceKey
{
    private static final String Tag = "DeviceKey";

    private static final String KeyStoreType = "AndroidKeyStore";

    private static final String CurveName = "secp256r1";

    private static final String SignatureAlgorithm = "SHA256withECDSA";

    private DeviceKey() { }

    public static synchronized boolean hasKey(String alias)
    {
        try
        {
            return loadKeyStore().getKey(alias, null) instanceof PrivateKey;
        }
        catch (Exception e)
        {
            Log.e(Tag, "Has key failed. alias = " + alias, e);

            return false;
        }
    }

    // 同じ alias の鍵は削除して作り直す.
    public static synchronized boolean createKey(String alias)
    {
        try
        {
            KeyStore keyStore = loadKeyStore();

            if (keyStore.containsAlias(alias))
            {
                keyStore.deleteEntry(alias);
            }

            KeyGenParameterSpec spec = new KeyGenParameterSpec.Builder(alias, KeyProperties.PURPOSE_SIGN)
                .setAlgorithmParameterSpec(new ECGenParameterSpec(CurveName))
                .setDigests(KeyProperties.DIGEST_SHA256)
                .build();

            KeyPairGenerator generator = KeyPairGenerator.getInstance(KeyProperties.KEY_ALGORITHM_EC, KeyStoreType);

            generator.initialize(spec);
            generator.generateKeyPair();

            return true;
        }
        catch (Exception e)
        {
            Log.e(Tag, "Create key failed. alias = " + alias, e);

            return false;
        }
    }

    // 公開鍵を X.509 SubjectPublicKeyInfo（DER）の Base64 文字列で返す.
    public static synchronized String getPublicKey(String alias)
    {
        try
        {
            Certificate certificate = loadKeyStore().getCertificate(alias);

            if (certificate == null) { return null; }

            return Base64.encodeToString(certificate.getPublicKey().getEncoded(), Base64.NO_WRAP);
        }
        catch (Exception e)
        {
            Log.e(Tag, "Get public key failed. alias = " + alias, e);

            return null;
        }
    }

    // 署名を DER の Base64 文字列で返す.
    public static synchronized String sign(String alias, String base64Data)
    {
        try
        {
            Key key = loadKeyStore().getKey(alias, null);

            if (!(key instanceof PrivateKey)) { return null; }

            byte[] data = Base64.decode(base64Data, Base64.NO_WRAP);

            Signature signature = Signature.getInstance(SignatureAlgorithm);

            signature.initSign((PrivateKey)key);
            signature.update(data);

            return Base64.encodeToString(signature.sign(), Base64.NO_WRAP);
        }
        catch (Exception e)
        {
            Log.e(Tag, "Sign failed. alias = " + alias, e);

            return null;
        }
    }

    public static synchronized boolean deleteKey(String alias)
    {
        try
        {
            KeyStore keyStore = loadKeyStore();

            if (keyStore.containsAlias(alias))
            {
                keyStore.deleteEntry(alias);
            }

            return true;
        }
        catch (Exception e)
        {
            Log.e(Tag, "Delete key failed. alias = " + alias, e);

            return false;
        }
    }

    private static KeyStore loadKeyStore() throws Exception
    {
        KeyStore keyStore = KeyStore.getInstance(KeyStoreType);

        keyStore.load(null);

        return keyStore;
    }
}
