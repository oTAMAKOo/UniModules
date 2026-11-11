#import <Foundation/Foundation.h>
#import <Security/Security.h>
#import <TargetConditionals.h>

#include <string.h>

// Secure Enclave に P-256 の EC 秘密鍵を作り、kSecAttrApplicationTag（tag）で引いて使う.
// シミュレーターは Secure Enclave が無いため、ソフトウェアの鍵を作る.
// 戻り値の int は OSStatus をそのまま返す. CFError で失敗したときは DeviceKeyStatusError を返す.

static const int DeviceKeyStatusError = -1;

static NSData* DeviceKeyCreateTag(const unsigned char* tag, int tagLength)
{
    if (tag == NULL || tagLength <= 0) { return [NSData data]; }

    return [NSData dataWithBytes:tag length:(NSUInteger)tagLength];
}

static NSMutableDictionary* DeviceKeyCreateQuery(const unsigned char* tag, int tagLength)
{
    NSMutableDictionary* query = [NSMutableDictionary dictionary];

    query[(__bridge id)kSecClass] = (__bridge id)kSecClassKey;
    query[(__bridge id)kSecAttrKeyClass] = (__bridge id)kSecAttrKeyClassPrivate;
    query[(__bridge id)kSecAttrKeyType] = (__bridge id)kSecAttrKeyTypeECSECPrimeRandom;
    query[(__bridge id)kSecAttrApplicationTag] = DeviceKeyCreateTag(tag, tagLength);

    return query;
}

// 戻り値の SecKeyRef は呼び出し側が CFRelease する.
static SecKeyRef DeviceKeyCopyPrivateKey(const unsigned char* tag, int tagLength, OSStatus* status)
{
    NSMutableDictionary* query = DeviceKeyCreateQuery(tag, tagLength);

    query[(__bridge id)kSecReturnRef] = @YES;
    query[(__bridge id)kSecMatchLimit] = (__bridge id)kSecMatchLimitOne;

    CFTypeRef result = NULL;

    *status = SecItemCopyMatching((__bridge CFDictionaryRef)query, &result);

    if (*status != errSecSuccess)
    {
        if (result != NULL) { CFRelease(result); }

        return NULL;
    }

    return (SecKeyRef)result;
}

static void DeviceKeyLogError(NSString* message, CFErrorRef error)
{
    if (error == NULL)
    {
        NSLog(@"[DeviceKey] %@", message);
        return;
    }

    NSError* nsError = (__bridge_transfer NSError*)error;

    NSLog(@"[DeviceKey] %@ %@", message, nsError);
}

static char* DeviceKeyCreateBase64String(NSData* data)
{
    NSString* base64 = [data base64EncodedStringWithOptions:0];

    const char* utf8 = [base64 UTF8String];

    return strdup(utf8 != NULL ? utf8 : "");
}

extern "C"
{
    int _DeviceKey_Has(const unsigned char* tag, int tagLength)
    {
        @autoreleasepool
        {
            NSMutableDictionary* query = DeviceKeyCreateQuery(tag, tagLength);

            return (int)SecItemCopyMatching((__bridge CFDictionaryRef)query, NULL);
        }
    }

    int _DeviceKey_Create(const unsigned char* tag, int tagLength)
    {
        @autoreleasepool
        {
            // 同じ tag の鍵は削除して作り直す.
            SecItemDelete((__bridge CFDictionaryRef)DeviceKeyCreateQuery(tag, tagLength));

            CFErrorRef error = NULL;

            #if TARGET_OS_SIMULATOR
            SecAccessControlCreateFlags flags = 0;
            #else
            SecAccessControlCreateFlags flags = kSecAccessControlPrivateKeyUsage;
            #endif

            SecAccessControlRef access = SecAccessControlCreateWithFlags(kCFAllocatorDefault, kSecAttrAccessibleAfterFirstUnlockThisDeviceOnly, flags, &error);

            if (access == NULL)
            {
                DeviceKeyLogError(@"Create access control failed.", error);

                return DeviceKeyStatusError;
            }

            NSDictionary* privateKeyAttributes = @{
                (__bridge id)kSecAttrIsPermanent : @YES,
                (__bridge id)kSecAttrApplicationTag : DeviceKeyCreateTag(tag, tagLength),
                (__bridge id)kSecAttrAccessControl : (__bridge id)access,
            };

            NSMutableDictionary* attributes = [NSMutableDictionary dictionary];

            attributes[(__bridge id)kSecAttrKeyType] = (__bridge id)kSecAttrKeyTypeECSECPrimeRandom;
            attributes[(__bridge id)kSecAttrKeySizeInBits] = @256;
            attributes[(__bridge id)kSecPrivateKeyAttrs] = privateKeyAttributes;

            #if !TARGET_OS_SIMULATOR
            attributes[(__bridge id)kSecAttrTokenID] = (__bridge id)kSecAttrTokenIDSecureEnclave;
            #endif

            SecKeyRef privateKey = SecKeyCreateRandomKey((__bridge CFDictionaryRef)attributes, &error);

            CFRelease(access);

            if (privateKey == NULL)
            {
                DeviceKeyLogError(@"Create key failed.", error);

                return DeviceKeyStatusError;
            }

            CFRelease(privateKey);

            return (int)errSecSuccess;
        }
    }

    // 公開鍵を X9.63（非圧縮 65 バイト）の Base64 文字列で返す. 戻り値は malloc で確保し、呼び出し側（マーシャラー）が解放する.
    char* _DeviceKey_GetPublicKey(const unsigned char* tag, int tagLength, int* status)
    {
        @autoreleasepool
        {
            OSStatus resultStatus = errSecSuccess;

            SecKeyRef privateKey = DeviceKeyCopyPrivateKey(tag, tagLength, &resultStatus);

            if (status != NULL) { *status = (int)resultStatus; }

            if (privateKey == NULL) { return NULL; }

            SecKeyRef publicKey = SecKeyCopyPublicKey(privateKey);

            CFRelease(privateKey);

            if (publicKey == NULL)
            {
                DeviceKeyLogError(@"Copy public key failed.", NULL);

                if (status != NULL) { *status = DeviceKeyStatusError; }

                return NULL;
            }

            CFErrorRef error = NULL;

            CFDataRef data = SecKeyCopyExternalRepresentation(publicKey, &error);

            CFRelease(publicKey);

            if (data == NULL)
            {
                DeviceKeyLogError(@"Export public key failed.", error);

                if (status != NULL) { *status = DeviceKeyStatusError; }

                return NULL;
            }

            return DeviceKeyCreateBase64String((__bridge_transfer NSData*)data);
        }
    }

    // ECDSA（SHA-256）の署名を DER（X9.62）の Base64 文字列で返す. 戻り値は malloc で確保し、呼び出し側（マーシャラー）が解放する.
    char* _DeviceKey_Sign(const unsigned char* tag, int tagLength, const unsigned char* data, int dataLength, int* status)
    {
        @autoreleasepool
        {
            OSStatus resultStatus = errSecSuccess;

            SecKeyRef privateKey = DeviceKeyCopyPrivateKey(tag, tagLength, &resultStatus);

            if (status != NULL) { *status = (int)resultStatus; }

            if (privateKey == NULL) { return NULL; }

            SecKeyAlgorithm algorithm = kSecKeyAlgorithmECDSASignatureMessageX962SHA256;

            if (!SecKeyIsAlgorithmSupported(privateKey, kSecKeyOperationTypeSign, algorithm))
            {
                CFRelease(privateKey);

                DeviceKeyLogError(@"Algorithm is not supported.", NULL);

                if (status != NULL) { *status = DeviceKeyStatusError; }

                return NULL;
            }

            NSData* message = (data != NULL && dataLength > 0) ? [NSData dataWithBytes:data length:(NSUInteger)dataLength] : [NSData data];

            CFErrorRef error = NULL;

            CFDataRef signature = SecKeyCreateSignature(privateKey, algorithm, (__bridge CFDataRef)message, &error);

            CFRelease(privateKey);

            if (signature == NULL)
            {
                DeviceKeyLogError(@"Sign failed.", error);

                if (status != NULL) { *status = DeviceKeyStatusError; }

                return NULL;
            }

            return DeviceKeyCreateBase64String((__bridge_transfer NSData*)signature);
        }
    }

    int _DeviceKey_Delete(const unsigned char* tag, int tagLength)
    {
        @autoreleasepool
        {
            NSMutableDictionary* query = DeviceKeyCreateQuery(tag, tagLength);

            return (int)SecItemDelete((__bridge CFDictionaryRef)query);
        }
    }
}
