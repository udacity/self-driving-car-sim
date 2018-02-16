//
//  GameSparksWebSocket.h
//  SocketRocket
//
//  Created by Kai Wegner on 10.02.15.
//
//

#import <Foundation/Foundation.h>
#import "SRWebSocket.h"

@interface GameSparksWebSocket : SRWebSocket
@property (assign) int socketId;
@property (strong) NSString* gameObjectName;
@end
