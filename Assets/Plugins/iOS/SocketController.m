//
//  SocketController.m
//  SocketRocket
//
//  Created by Kai Wegner on 10.02.15.
//
//

#import "SocketController.h"

SocketController* sSocketController = nil;


@implementation SocketController




+ (SocketController*) getInstance
{
    if (sSocketController == nil)
    {
        sSocketController = [[SocketController alloc] init];
    }
    
    
    return sSocketController;
}


- (id)init
{
    self = [super init];
    if (self) {
        self.sockets = [[NSMutableDictionary alloc] init];
    }
    return self;
}

- (GameSparksWebSocket*) createWithURL:(NSURL*) url andID: (int) identifier
{
    GameSparksWebSocket* socket = [[GameSparksWebSocket alloc] initWithURLRequest:[NSURLRequest requestWithURL:url]];
    socket.socketId = identifier;
    socket.delegate = self;
    
    [self.sockets setObject:socket forKey:[NSNumber numberWithInt:identifier]];
    
    return socket;
}

- (GameSparksWebSocket*) socketById:(int) identifier
{
    NSNumber* uniqueID = [NSNumber numberWithInt:identifier];
    return [self.sockets objectForKey:uniqueID];
}


- (void)webSocketDidOpen:(SRWebSocket *)webSocket
{
    GameSparksWebSocket* socket = (GameSparksWebSocket*)webSocket;
    
    NSMutableDictionary* dict = [[NSMutableDictionary alloc] init];
    [dict setObject:[@(socket.socketId) stringValue] forKey:@"socketId"];
    NSError* jsonError;
    NSData* jsonData = [NSJSONSerialization dataWithJSONObject:dict options:0 error:&jsonError];
    NSString* msg = [[NSString alloc] initWithData:jsonData encoding:NSUTF8StringEncoding];
    
    UnitySendMessage([socket.gameObjectName UTF8String], "GSSocketOnOpen", [msg UTF8String]);
}
- (void)webSocket:(SRWebSocket *)webSocket didFailWithError:(NSError *)error
{
    GameSparksWebSocket* socket = (GameSparksWebSocket*)webSocket;
    
    NSMutableDictionary* dict = [[NSMutableDictionary alloc] init];
    [dict setObject:[@(socket.socketId) stringValue] forKey:@"socketId"];

    if(error){
        [dict setObject:[error localizedDescription] forKey:@"error"];
    } else {
        [dict setObject:@"" forKey:@"error"];
    }
    
    NSError* jsonError;
    NSData* jsonData = [NSJSONSerialization dataWithJSONObject:dict options:0 error:&jsonError];
    NSString* msg = [[NSString alloc] initWithData:jsonData encoding:NSUTF8StringEncoding];
    
    UnitySendMessage([socket.gameObjectName UTF8String], "GSSocketOnError", [msg UTF8String]);
}
- (void)webSocket:(SRWebSocket *)webSocket didReceiveMessage:(id)message
{
    GameSparksWebSocket* socket = (GameSparksWebSocket*)webSocket;
    
    NSMutableDictionary* dict = [[NSMutableDictionary alloc] init];
    [dict setObject:[@(socket.socketId) stringValue] forKey:@"socketId"];
    [dict setObject:message forKey:@"message"];
    
    NSError* error;
    NSData* jsonData = [NSJSONSerialization dataWithJSONObject:dict options:0 error:&error];
    NSString* msg = [[NSString alloc] initWithData:jsonData encoding:NSUTF8StringEncoding];
    
    UnitySendMessage([socket.gameObjectName UTF8String], "GSSocketOnMessage", [msg UTF8String]);
}
- (void)webSocket:(SRWebSocket *)webSocket didCloseWithCode:(NSInteger)code reason:(NSString *)reason wasClean:(BOOL)wasClean
{
    GameSparksWebSocket* socket = (GameSparksWebSocket*)webSocket;
    
    NSMutableDictionary* dict = [[NSMutableDictionary alloc] init];
    [dict setObject:[@(socket.socketId) stringValue] forKey:@"socketId"];
    if(reason){
        [dict setObject:reason forKey:@"error"];
    } else {
        [dict setObject:@"" forKey:@"error"];
    }
    NSError* jsonError;
    NSData* jsonData = [NSJSONSerialization dataWithJSONObject:dict options:0 error:&jsonError];
    NSString* msg = [[NSString alloc] initWithData:jsonData encoding:NSUTF8StringEncoding];
    
    
    UnitySendMessage([socket.gameObjectName UTF8String], "GSSocketOnClose", [msg UTF8String]);
}


@end
