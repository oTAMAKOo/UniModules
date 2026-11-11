#import <Foundation/Foundation.h>
#import <Security/Security.h>

#include <string.h>

// Keychain の汎用パスワード項目を service と key（account）で読み書きする.
// 戻り値の int は OSStatus をそのまま返す.

static NSString* SecureStorageCreateString(const unsigned char* bytes, int length)
{
    if (bytes == NULL || length <= 0) { return @""; }

    NSString* str = [[NSString alloc] initWithBytes:bytes length:(NSUInteger)length encoding:NSUTF8StringEncoding];

    return str != nil ? str : @"";
}

static NSMutableDictionary* SecureStorageCreateQuery(const unsigned char* service, int serviceLength)
{
    NSMutableDictionary* query = [NSMutableDictionary dictionary];

    query[(__bridge id)kSecClass] = (__bridge id)kSecClassGenericPassword;
    query[(__bridge id)kSecAttrService] = SecureStorageCreateString(service, serviceLength);

    return query;
}

static NSMutableDictionary* SecureStorageCreateItemQuery(const unsigned char* service, int serviceLength, const unsigned char* key, int keyLength)
{
    NSMutableDictionary* query = SecureStorageCreateQuery(service, serviceLength);

    query[(__bridge id)kSecAttrAccount] = SecureStorageCreateString(key, keyLength);

    return query;
}

extern "C"
{
    int _SecureStorage_Set(const unsigned char* service, int serviceLength, const unsigned char* key, int keyLength, const unsigned char* value, int valueLength)
    {
        @autoreleasepool
        {
            NSMutableDictionary* query = SecureStorageCreateItemQuery(service, serviceLength, key, keyLength);

            NSData* data = (value != NULL && valueLength > 0) ? [NSData dataWithBytes:value length:(NSUInteger)valueLength] : [NSData data];

            NSDictionary* attributes = @{
                (__bridge id)kSecValueData : data,
                (__bridge id)kSecAttrAccessible : (__bridge id)kSecAttrAccessibleAfterFirstUnlockThisDeviceOnly,
            };

            OSStatus status = SecItemUpdate((__bridge CFDictionaryRef)query, (__bridge CFDictionaryRef)attributes);

            if (status == errSecItemNotFound)
            {
                [query addEntriesFromDictionary:attributes];

                status = SecItemAdd((__bridge CFDictionaryRef)query, NULL);
            }

            return (int)status;
        }
    }

    // 値を Base64 文字列で返す. 戻り値は malloc で確保し、呼び出し側（マーシャラー）が解放する.
    char* _SecureStorage_Get(const unsigned char* service, int serviceLength, const unsigned char* key, int keyLength, int* status)
    {
        @autoreleasepool
        {
            NSMutableDictionary* query = SecureStorageCreateItemQuery(service, serviceLength, key, keyLength);

            query[(__bridge id)kSecReturnData] = @YES;
            query[(__bridge id)kSecMatchLimit] = (__bridge id)kSecMatchLimitOne;

            CFTypeRef result = NULL;

            OSStatus resultStatus = SecItemCopyMatching((__bridge CFDictionaryRef)query, &result);

            if (status != NULL) { *status = (int)resultStatus; }

            if (resultStatus != errSecSuccess || result == NULL)
            {
                if (result != NULL) { CFRelease(result); }

                return NULL;
            }

            NSData* data = (__bridge_transfer NSData*)result;

            NSString* base64 = [data base64EncodedStringWithOptions:0];

            const char* utf8 = [base64 UTF8String];

            return strdup(utf8 != NULL ? utf8 : "");
        }
    }

    int _SecureStorage_Delete(const unsigned char* service, int serviceLength, const unsigned char* key, int keyLength)
    {
        @autoreleasepool
        {
            NSMutableDictionary* query = SecureStorageCreateItemQuery(service, serviceLength, key, keyLength);

            return (int)SecItemDelete((__bridge CFDictionaryRef)query);
        }
    }

    int _SecureStorage_DeleteAll(const unsigned char* service, int serviceLength)
    {
        @autoreleasepool
        {
            NSMutableDictionary* query = SecureStorageCreateQuery(service, serviceLength);

            return (int)SecItemDelete((__bridge CFDictionaryRef)query);
        }
    }
}
