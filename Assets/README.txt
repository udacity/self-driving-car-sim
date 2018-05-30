Simple WebSockets for Unity WebGL
=================================

Description
===========

This package implements basic support for the WebSocket networking protocol in Unity WebGL. 

For security reasons, direct access to IP Sockets is not available in JavaScript. However, it is possible to use the WebSockets protocol to implement networking in WebGL games. This package provides a JavaScript wrapper library which lets you use the browsers WebSockets JavaScript API from within Unity. For testing in the editor and running in other platforms, an implementation of the protocol using the websocket-sharp library is used.

See the contained EchoTest.cs for a simple sample using a WebSocket connection to send and receive packages from an echo server.

Disclaimer
==========

This is an unsupported package provided by Unity Technologies for demonstration purposes.

Manual
======

WebSocket.WebSocket(Uri url)

Creates a new WebSocket instance to connect to a specific Uri. The Uri should use the ws or wss uri protocol scheme.




IEnumerator WebSocket.Connect()

Coroutine to initiate a WebSocket connection. You can use this function like this to initiate a connection and wait for the connection result:

yield return StartCoroutine(w.Connect());



void WebSocket.Send(byte[] buffer)
void WebSocket.SendString(string str)

Send a package over a WebSocket connection. In the first form, the package is given as binary data in a byte array, in the second form it is supplied as a string.




string WebSocket.RecvString()
byte[] WebSocket.Recv()

Checks if there are any pending received messages on the WebSocket connection. If yes, it will return the first message (as a string or byte array, depending on the function used), and remove it from the message queue. If there are no pending messages, it will return null.



void WebSocket.Close()

Closes the websocket connection.



string WebSocket.error

An error message if there were any errors in the last operation, null otherwise.




Notes
=====

websocket-sharp.dll built from https://github.com/sta/websocket-sharp.git, commit 1a94864e86368453455a9c77e1defad6ecd0bd41.

WebSocket-Sharp License:
========================

The MIT License (MIT)

Copyright (c) 2010-2015 sta.blockhead

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.