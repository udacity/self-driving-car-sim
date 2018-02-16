//
//  SocketController.h
//  SocketRocket
//
//  Created by Kai Wegner on 10.02.15.
//
//

#import <Foundation/Foundation.h>

#import "GameSparksWebSocket.h"
#import "SRWebSocket.h"

@interface SocketController : NSObject <SRWebSocketDelegate>

@property (strong) NSMutableDictionary* sockets;

+ (SocketController*) getInstance; // singleton
- (GameSparksWebSocket*) createWithURL:(NSURL*) url andID: (int) identifier;
- (GameSparksWebSocket*) socketById:(int) identifier;

@end
