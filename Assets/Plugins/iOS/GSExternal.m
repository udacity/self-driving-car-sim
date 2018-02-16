# include "GSExternal.h"

#import "GameSparksWebSocket.h"
#import "SocketController.h"

#include <CommonCrypto/CommonDigest.h>
#include <CommonCrypto/CommonHMAC.h>

#if __cplusplus
extern "C" {
#endif
    void GSExternalOpen(int socketId, char* urlChars, char* gameObjectName)
    {
        NSString* urlString = [NSString stringWithUTF8String:urlChars];

        NSURL* url = [NSURL URLWithString:urlString];
        SocketController* controller = [SocketController getInstance];
        GameSparksWebSocket *entry = [controller createWithURL: url andID: socketId];

        entry.gameObjectName = [NSString stringWithUTF8String:gameObjectName];
        
        [entry open];
    }
    
    void GSExternalClose(int socketId)
    {
        GameSparksWebSocket* entry = [[SocketController getInstance] socketById:socketId];
        if(entry != nil)
        {
            [entry close];
        }
    }
    
    void GSExternalSend(int socketId, char* message)
    {
        GameSparksWebSocket* entry = [[SocketController getInstance] socketById:socketId];
        [entry send:[NSString stringWithUTF8String:message]];
    }

    char* GSHMac(char* data, char* secret)
    {
        
        const char *cKey  = [[NSString stringWithUTF8String:secret] cStringUsingEncoding:NSASCIIStringEncoding];
        const char *cData = [[NSString stringWithUTF8String:data] cStringUsingEncoding:NSASCIIStringEncoding];
        unsigned char cHMAC[CC_SHA256_DIGEST_LENGTH];
        CCHmac(kCCHmacAlgSHA256, cKey, strlen(cKey), cData, strlen(cData), cHMAC);
        NSData* hmacdata = [[NSData alloc] initWithBytes:cHMAC length:sizeof(cHMAC)];


        NSString* hash = [hmacdata base64Encoding];
        const char* outputChars = [hash UTF8String];
        char* copyResult = malloc(sizeof(char)*hash.length +1 );
        memcpy(copyResult, outputChars, sizeof(char)*hash.length +1 );
        return copyResult;

    }

#if __cplusplus
}
#endif

// socket controller


