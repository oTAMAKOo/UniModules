package com.unimodules.securestorage;

import android.content.Context;
import android.security.keystore.KeyGenParameterSpec;
import android.security.keystore.KeyPermanentlyInvalidatedException;
import android.security.keystore.KeyProperties;
import android.util.Base64;
import android.util.Log;

import java.io.ByteArrayOutputStream;
import java.io.File;
import java.io.FileInputStream;
import java.io.FileOutputStream;
import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;
import java.nio.charset.StandardCharsets;
import java.security.Key;
import java.security.KeyStore;
import java.security.MessageDigest;

import javax.crypto.AEADBadTagException;
import javax.crypto.Cipher;
import javax.crypto.KeyGenerator;
import javax.crypto.SecretKey;
import javax.crypto.spec.GCMParameterSpec;

/**
 * Keystore の AES 鍵で暗号化した値を、バックアップ対象外の領域（noBackupFilesDir）にファイルで保存する.
 * 値は Base64 文字列で受け渡す. 失敗しても例外は投げず、set / delete / deleteAll は false、get は null を返す.
 */
public final class SecureStorage
{
    private static final String Tag = "SecureStorage";

    private static final String KeyStoreType = "AndroidKeyStore";

    private static final String Transformation = "AES/GCM/NoPadding";

    private static final String DirectoryName = "SecureStorage";

    private static final String KeyAliasPrefix = "SecureStorage.";

    private static final String TempFileSuffix = ".tmp";

    private static final int KeySize = 256;

    private static final int IvLength = 12;

    private static final int TagLength = 128;

    private SecureStorage() { }

    public static synchronized boolean set(Context context, String service, String key, String base64Value)
    {
        try
        {
            byte[] value = Base64.decode(base64Value, Base64.NO_WRAP);

            SecretKey secretKey = getKey(service);

            if (secretKey == null)
            {
                secretKey = createKey(service);
            }

            Cipher cipher = Cipher.getInstance(Transformation);

            cipher.init(Cipher.ENCRYPT_MODE, secretKey);
            cipher.updateAAD(toBytes(key));

            byte[] iv = cipher.getIV();
            byte[] encrypted = cipher.doFinal(value);

            if (iv == null || iv.length != IvLength)
            {
                throw new IllegalStateException("Unexpected iv length.");
            }

            // 先頭に IV、続けて暗号文（認証タグ込み）を書く.
            byte[] data = new byte[iv.length + encrypted.length];

            System.arraycopy(iv, 0, data, 0, iv.length);
            System.arraycopy(encrypted, 0, data, iv.length, encrypted.length);

            writeFile(getFile(context, service, key), data);

            return true;
        }
        catch (Exception e)
        {
            Log.e(Tag, "Set failed. key = " + key, e);

            return false;
        }
    }

    public static synchronized String get(Context context, String service, String key)
    {
        File file = null;

        try
        {
            file = getFile(context, service, key);

            if (!file.exists()) { return null; }

            byte[] data = readFile(file);

            SecretKey secretKey = getKey(service);

            // 鍵が無い・壊れた値は復号できないため削除する.
            if (secretKey == null || data.length <= IvLength)
            {
                Log.w(Tag, "Value can not be decrypted. Delete value. key = " + key);

                file.delete();

                return null;
            }

            Cipher cipher = Cipher.getInstance(Transformation);

            cipher.init(Cipher.DECRYPT_MODE, secretKey, new GCMParameterSpec(TagLength, data, 0, IvLength));
            cipher.updateAAD(toBytes(key));

            byte[] value = cipher.doFinal(data, IvLength, data.length - IvLength);

            return Base64.encodeToString(value, Base64.NO_WRAP);
        }
        catch (AEADBadTagException | KeyPermanentlyInvalidatedException e)
        {
            // 復号できない値は読めないため削除する.
            Log.w(Tag, "Value can not be decrypted. Delete value. key = " + key, e);

            if (file != null) { file.delete(); }

            return null;
        }
        catch (Exception e)
        {
            Log.e(Tag, "Get failed. key = " + key, e);

            return null;
        }
    }

    public static synchronized boolean delete(Context context, String service, String key)
    {
        try
        {
            File file = getFile(context, service, key);

            return !file.exists() || file.delete();
        }
        catch (Exception e)
        {
            Log.e(Tag, "Delete failed. key = " + key, e);

            return false;
        }
    }

    public static synchronized boolean deleteAll(Context context, String service)
    {
        try
        {
            boolean result = true;

            File directory = getDirectory(context, service);

            File[] files = directory.listFiles();

            if (files != null)
            {
                for (File file : files)
                {
                    result &= file.delete();
                }
            }

            if (directory.exists())
            {
                result &= directory.delete();
            }

            KeyStore keyStore = loadKeyStore();

            String alias = getKeyAlias(service);

            if (keyStore.containsAlias(alias))
            {
                keyStore.deleteEntry(alias);
            }

            return result;
        }
        catch (Exception e)
        {
            Log.e(Tag, "Delete all failed. service = " + service, e);

            return false;
        }
    }

    //====== Key ======

    private static KeyStore loadKeyStore() throws Exception
    {
        KeyStore keyStore = KeyStore.getInstance(KeyStoreType);

        keyStore.load(null);

        return keyStore;
    }

    private static String getKeyAlias(String service)
    {
        return KeyAliasPrefix + service;
    }

    private static SecretKey getKey(String service) throws Exception
    {
        KeyStore keyStore = loadKeyStore();

        Key key = keyStore.getKey(getKeyAlias(service), null);

        return key instanceof SecretKey ? (SecretKey)key : null;
    }

    private static SecretKey createKey(String service) throws Exception
    {
        KeyGenParameterSpec spec = new KeyGenParameterSpec.Builder(getKeyAlias(service), KeyProperties.PURPOSE_ENCRYPT | KeyProperties.PURPOSE_DECRYPT)
            .setBlockModes(KeyProperties.BLOCK_MODE_GCM)
            .setEncryptionPaddings(KeyProperties.ENCRYPTION_PADDING_NONE)
            .setKeySize(KeySize)
            .build();

        KeyGenerator generator = KeyGenerator.getInstance(KeyProperties.KEY_ALGORITHM_AES, KeyStoreType);

        generator.init(spec);

        return generator.generateKey();
    }

    //====== File ======

    private static File getDirectory(Context context, String service) throws Exception
    {
        File root = new File(context.getApplicationContext().getNoBackupFilesDir(), DirectoryName);

        return new File(root, toHash(service));
    }

    private static File getFile(Context context, String service, String key) throws Exception
    {
        return new File(getDirectory(context, service), toHash(key));
    }

    private static void writeFile(File file, byte[] data) throws IOException
    {
        File directory = file.getParentFile();

        if (directory != null && !directory.exists() && !directory.mkdirs())
        {
            throw new IOException("Create directory failed.");
        }

        // 一時ファイルへ書いてから置き換え、書き込み途中の値を残さない.
        File tempFile = new File(file.getPath() + TempFileSuffix);

        OutputStream stream = new FileOutputStream(tempFile);

        try
        {
            stream.write(data);
        }
        finally
        {
            stream.close();
        }

        if (!tempFile.renameTo(file))
        {
            tempFile.delete();

            throw new IOException("Rename file failed.");
        }
    }

    private static byte[] readFile(File file) throws IOException
    {
        InputStream stream = new FileInputStream(file);

        try
        {
            ByteArrayOutputStream output = new ByteArrayOutputStream();

            byte[] buffer = new byte[4096];

            int length;

            while ((length = stream.read(buffer)) != -1)
            {
                output.write(buffer, 0, length);
            }

            return output.toByteArray();
        }
        finally
        {
            stream.close();
        }
    }

    //====== Utility ======

    private static byte[] toBytes(String value)
    {
        return value.getBytes(StandardCharsets.UTF_8);
    }

    // ファイル名に使えない文字を避けるため、名前は SHA-256 の 16 進文字列にする.
    private static String toHash(String value) throws Exception
    {
        MessageDigest digest = MessageDigest.getInstance("SHA-256");

        byte[] hash = digest.digest(toBytes(value));

        StringBuilder builder = new StringBuilder(hash.length * 2);

        for (byte b : hash)
        {
            builder.append(Character.forDigit((b >> 4) & 0xF, 16));
            builder.append(Character.forDigit(b & 0xF, 16));
        }

        return builder.toString();
    }
}
