//
//  GSExternal.h
//  SocketRocket
//
//  Created by Kai Wegner on 10.02.15.
//
//


#if __cplusplus
extern "C" {
#endif
    void GSExternalOpen(int socketId, char* url, char* gameObjectName);
    
    void GSExternalClose(int socketId);
    
    void GSExternalSend(int socketId, char* message);
#if __cplusplus
}
#endif
