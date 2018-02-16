
var GameSparks = new function(){


	this.GSSocketInitialize = function(id,name) {
		GSSocketCollection.Get(id).Initialize(Pointer_stringify(name));
	};

	this.GSFreePtr = function(ptr) {
		console.log("freeing ptr: " + ptr);
		_free(ptr);
	}

	this.GSHmacSHA256 = function(message, key) {


	 	var hash = CryptoJS.HmacSHA256(Pointer_stringify(message), Pointer_stringify(key) );
	  	var hashInBase64 = CryptoJS.enc.Base64.stringify(hash);

	  	var targetPtr = _malloc(hashInBase64.length+10);
		writeStringToMemory(hashInBase64, targetPtr);
		
	  	return targetPtr;
	};

	this.GSSocketSend = function(id, data) {
		GSSocketCollection.Get(id).Send(Pointer_stringify(data));
	};

	this.GSSocketOpen = function(id, url) {
		GSSocketCollection.Get(id).Open(Pointer_stringify(url));
	};

	this.GSSocketClose = function(id) {
		GSSocketCollection.Get(id).Close();
	};

	this.GSGetDeviceId = function() {

		var deviceId = 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function(c) {var r = Math.random()*16|0,v=c=='x'?r:r&0x3|0x8;return v.toString(16);});
		var targetPtr = _malloc(deviceId.length+10);
		writeStringToMemory(deviceId, targetPtr);
		
	  	return targetPtr;
	};

	this.GSInitializeCrypto = function() {

		var CryptoJS=CryptoJS||function(h,s){var f={},g=f.lib={},q=function(){},m=g.Base={extend:function(a){q.prototype=this;var c=new q;a&&c.mixIn(a);c.hasOwnProperty("init")||(c.init=function(){c.$super.init.apply(this,arguments)});c.init.prototype=c;c.$super=this;return c},create:function(){var a=this.extend();a.init.apply(a,arguments);return a},init:function(){},mixIn:function(a){for(var c in a)a.hasOwnProperty(c)&&(this[c]=a[c]);a.hasOwnProperty("toString")&&(this.toString=a.toString)},clone:function(){return this.init.prototype.extend(this)}},
r=g.WordArray=m.extend({init:function(a,c){a=this.words=a||[];this.sigBytes=c!=s?c:4*a.length},toString:function(a){return(a||k).stringify(this)},concat:function(a){var c=this.words,d=a.words,b=this.sigBytes;a=a.sigBytes;this.clamp();if(b%4)for(var e=0;e<a;e++)c[b+e>>>2]|=(d[e>>>2]>>>24-8*(e%4)&255)<<24-8*((b+e)%4);else if(65535<d.length)for(e=0;e<a;e+=4)c[b+e>>>2]=d[e>>>2];else c.push.apply(c,d);this.sigBytes+=a;return this},clamp:function(){var a=this.words,c=this.sigBytes;a[c>>>2]&=4294967295<<
32-8*(c%4);a.length=h.ceil(c/4)},clone:function(){var a=m.clone.call(this);a.words=this.words.slice(0);return a},random:function(a){for(var c=[],d=0;d<a;d+=4)c.push(4294967296*h.random()|0);return new r.init(c,a)}}),l=f.enc={},k=l.Hex={stringify:function(a){var c=a.words;a=a.sigBytes;for(var d=[],b=0;b<a;b++){var e=c[b>>>2]>>>24-8*(b%4)&255;d.push((e>>>4).toString(16));d.push((e&15).toString(16))}return d.join("")},parse:function(a){for(var c=a.length,d=[],b=0;b<c;b+=2)d[b>>>3]|=parseInt(a.substr(b,
2),16)<<24-4*(b%8);return new r.init(d,c/2)}},n=l.Latin1={stringify:function(a){var c=a.words;a=a.sigBytes;for(var d=[],b=0;b<a;b++)d.push(String.fromCharCode(c[b>>>2]>>>24-8*(b%4)&255));return d.join("")},parse:function(a){for(var c=a.length,d=[],b=0;b<c;b++)d[b>>>2]|=(a.charCodeAt(b)&255)<<24-8*(b%4);return new r.init(d,c)}},j=l.Utf8={stringify:function(a){try{return decodeURIComponent(escape(n.stringify(a)))}catch(c){throw Error("Malformed UTF-8 data");}},parse:function(a){return n.parse(unescape(encodeURIComponent(a)))}},
u=g.BufferedBlockAlgorithm=m.extend({reset:function(){this._data=new r.init;this._nDataBytes=0},_append:function(a){"string"==typeof a&&(a=j.parse(a));this._data.concat(a);this._nDataBytes+=a.sigBytes},_process:function(a){var c=this._data,d=c.words,b=c.sigBytes,e=this.blockSize,f=b/(4*e),f=a?h.ceil(f):h.max((f|0)-this._minBufferSize,0);a=f*e;b=h.min(4*a,b);if(a){for(var g=0;g<a;g+=e)this._doProcessBlock(d,g);g=d.splice(0,a);c.sigBytes-=b}return new r.init(g,b)},clone:function(){var a=m.clone.call(this);
a._data=this._data.clone();return a},_minBufferSize:0});g.Hasher=u.extend({cfg:m.extend(),init:function(a){this.cfg=this.cfg.extend(a);this.reset()},reset:function(){u.reset.call(this);this._doReset()},update:function(a){this._append(a);this._process();return this},finalize:function(a){a&&this._append(a);return this._doFinalize()},blockSize:16,_createHelper:function(a){return function(c,d){return(new a.init(d)).finalize(c)}},_createHmacHelper:function(a){return function(c,d){return(new t.HMAC.init(a,
d)).finalize(c)}}});var t=f.algo={};return f}(Math);
		(function(h){for(var s=CryptoJS,f=s.lib,g=f.WordArray,q=f.Hasher,f=s.algo,m=[],r=[],l=function(a){return 4294967296*(a-(a|0))|0},k=2,n=0;64>n;){var j;a:{j=k;for(var u=h.sqrt(j),t=2;t<=u;t++)if(!(j%t)){j=!1;break a}j=!0}j&&(8>n&&(m[n]=l(h.pow(k,0.5))),r[n]=l(h.pow(k,1/3)),n++);k++}var a=[],f=f.SHA256=q.extend({_doReset:function(){this._hash=new g.init(m.slice(0))},_doProcessBlock:function(c,d){for(var b=this._hash.words,e=b[0],f=b[1],g=b[2],j=b[3],h=b[4],m=b[5],n=b[6],q=b[7],p=0;64>p;p++){if(16>p)a[p]=
c[d+p]|0;else{var k=a[p-15],l=a[p-2];a[p]=((k<<25|k>>>7)^(k<<14|k>>>18)^k>>>3)+a[p-7]+((l<<15|l>>>17)^(l<<13|l>>>19)^l>>>10)+a[p-16]}k=q+((h<<26|h>>>6)^(h<<21|h>>>11)^(h<<7|h>>>25))+(h&m^~h&n)+r[p]+a[p];l=((e<<30|e>>>2)^(e<<19|e>>>13)^(e<<10|e>>>22))+(e&f^e&g^f&g);q=n;n=m;m=h;h=j+k|0;j=g;g=f;f=e;e=k+l|0}b[0]=b[0]+e|0;b[1]=b[1]+f|0;b[2]=b[2]+g|0;b[3]=b[3]+j|0;b[4]=b[4]+h|0;b[5]=b[5]+m|0;b[6]=b[6]+n|0;b[7]=b[7]+q|0},_doFinalize:function(){var a=this._data,d=a.words,b=8*this._nDataBytes,e=8*a.sigBytes;
d[e>>>5]|=128<<24-e%32;d[(e+64>>>9<<4)+14]=h.floor(b/4294967296);d[(e+64>>>9<<4)+15]=b;a.sigBytes=4*d.length;this._process();return this._hash},clone:function(){var a=q.clone.call(this);a._hash=this._hash.clone();return a}});s.SHA256=q._createHelper(f);s.HmacSHA256=q._createHmacHelper(f)})(Math);
(function(){var h=CryptoJS,s=h.enc.Utf8;h.algo.HMAC=h.lib.Base.extend({init:function(f,g){f=this._hasher=new f.init;"string"==typeof g&&(g=s.parse(g));var h=f.blockSize,m=4*h;g.sigBytes>m&&(g=f.finalize(g));g.clamp();for(var r=this._oKey=g.clone(),l=this._iKey=g.clone(),k=r.words,n=l.words,j=0;j<h;j++)k[j]^=1549556828,n[j]^=909522486;r.sigBytes=l.sigBytes=m;this.reset()},reset:function(){var f=this._hasher;f.reset();f.update(this._iKey)},update:function(f){this._hasher.update(f);return this},finalize:function(f){var g=
this._hasher;f=g.finalize(f);g.reset();return g.finalize(this._oKey.clone().concat(f))}})})();

		window.CryptoJS = CryptoJS;

		(function () {
		    // Shortcuts
		    var C = CryptoJS;
		    var C_lib = C.lib;
		    var WordArray = C_lib.WordArray;
		    var C_enc = C.enc;

		    /**
		     * Base64 encoding strategy.
		     */
		    var Base64 = C_enc.Base64 = {
		        /**
		         * Converts a word array to a Base64 string.
		         *
		         * @param {WordArray} wordArray The word array.
		         *
		         * @return {string} The Base64 string.
		         *
		         * @static
		         *
		         * @example
		         *
		         *     var base64String = CryptoJS.enc.Base64.stringify(wordArray);
		         */
		        stringify: function (wordArray) {
		            // Shortcuts
		            var words = wordArray.words;
		            var sigBytes = wordArray.sigBytes;
		            var map = this._map;

		            // Clamp excess bits
		            wordArray.clamp();

		            // Convert
		            var base64Chars = [];
		            for (var i = 0; i < sigBytes; i += 3) {
		                var byte1 = (words[i >>> 2]       >>> (24 - (i % 4) * 8))       & 0xff;
		                var byte2 = (words[(i + 1) >>> 2] >>> (24 - ((i + 1) % 4) * 8)) & 0xff;
		                var byte3 = (words[(i + 2) >>> 2] >>> (24 - ((i + 2) % 4) * 8)) & 0xff;

		                var triplet = (byte1 << 16) | (byte2 << 8) | byte3;

		                for (var j = 0; (j < 4) && (i + j * 0.75 < sigBytes); j++) {
		                    base64Chars.push(map.charAt((triplet >>> (6 * (3 - j))) & 0x3f));
		                }
		            }

		            // Add padding
		            var paddingChar = map.charAt(64);
		            if (paddingChar) {
		                while (base64Chars.length % 4) {
		                    base64Chars.push(paddingChar);
		                }
		            }

		            return base64Chars.join('');
		        },

		        /**
		         * Converts a Base64 string to a word array.
		         *
		         * @param {string} base64Str The Base64 string.
		         *
		         * @return {WordArray} The word array.
		         *
		         * @static
		         *
		         * @example
		         *
		         *     var wordArray = CryptoJS.enc.Base64.parse(base64String);
		         */
		        parse: function (base64Str) {
		            // Shortcuts
		            var base64StrLength = base64Str.length;
		            var map = this._map;

		            // Ignore padding
		            var paddingChar = map.charAt(64);
		            if (paddingChar) {
		                var paddingIndex = base64Str.indexOf(paddingChar);
		                if (paddingIndex != -1) {
		                    base64StrLength = paddingIndex;
		                }
		            }

		            // Convert
		            var words = [];
		            var nBytes = 0;
		            for (var i = 0; i < base64StrLength; i++) {
		                if (i % 4) {
		                    var bits1 = map.indexOf(base64Str.charAt(i - 1)) << ((i % 4) * 2);
		                    var bits2 = map.indexOf(base64Str.charAt(i)) >>> (6 - (i % 4) * 2);
		                    words[nBytes >>> 2] |= (bits1 | bits2) << (24 - (nBytes % 4) * 8);
		                    nBytes++;
		                }
		            }

		            return WordArray.create(words, nBytes);
		        },

		        _map: 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/='
		    };
		}());




	};

	this.GSInitializeGSSocket = function () {


		window.GSSocketCollection = new function() {
			this.sockets = [];
		  
			this.Get = function(id) {

			for (var i = this.sockets.length - 1; i >= 0; i--) {
		  		if(this.sockets[i].Id() == id)
		  		{
		  			return this.sockets[i];
		  		}
		  	}

		  	var newSocket = new GSSocket(id);
		  	this.sockets.push(newSocket);
		  	return newSocket;
		  };

		  this.RemoveSocket = function(socketId) {
		  	console.log("Remove Socket: " + socketId);
		  	console.log(this.sockets);

			for (var i = this.sockets.length - 1; i >= 0; i--) {
		  		if(this.sockets[i].Id() == socketId)
		  		{
		  			console.log("found: " + i);
		  			this.sockets.splice(i,1);
		  			console.log("splice: " + this.sockets);
		  			break;
		  		}
		  	}
		  };
		}

		window.GSSocket = function(socketId) {
		  
		  this.Id = function() {
		  	return this.id;
		  }

		  this.debug = true;
			
		  this.id = socketId;
		  
		  this.webSocket;

		  this.gameObjectName = "";

		  this.socketUrl = "";

		  this.Initialize = function(name) {
		    this.log("GS Initialize websocket" + this.id + ": " + name);
		    this.gameObjectName = name;
		  }

		  this.Send = function (data) {
		    this.log("request send socket" + this.id + ": " + data);
		    if(this.checkWebSocketValid(function(state){return state == 1;}))
		    {
		      this.webSocket.send(data);
		    }
		  	else
		  	{
		  		throw "Could not send data through WebSocket. ";
		  	}
		  };

		  this.Open = function(url) {
		    this.log("request open socket" + this.id + ": " + url);
		    this.socketUrl = url;
		    this.connect();
		  };

		  this.Close = function() {
		    this.log("request close socket" + this.id );
		    if(this.checkWebSocketValid(function(state){return state < 2;}))
		    {
		      	this.webSocket.close();
		    }
		  	else
		  	{
		  		throw "Could not close websocket. ";
		  	}
		  };

		  function getWebSocket(location) {
		    if (window.WebSocket) {
		      return new WebSocket(location);
		    } else {
		      return new MozWebSocket(location);
		    }
		  }

		  this.log = function(message) {
		    if(this.debug && console)
		    {
		      console.log(message);
		    }
		  }

		  this.connect = function() {
		    this.webSocket = getWebSocket(this.socketUrl);
		    var self = this;
		    this.webSocket.onopen = function(evt) { self.onWebSocketOpen(evt); };
		    this.webSocket.onclose = function() { self.onWebSocketClose(); };
		    this.webSocket.onerror = function(evt) {self.onWebSocketError(evt); };
		    this.webSocket.onmessage = function(msg) { self.onWebSocketMessage(msg); };
		  }

		  this.checkWebSocketValid = function(stateValidator) {
		  	if(!this.webSocket)
		  	{
		  		this.log("WebSocket not initialized. ");
		  		return false;
		  	}
		  	else if(!stateValidator(this.webSocket.readyState))
	  		{
	  			this.log("WebSocket in wrong state: " + webSocketStateToString(this.webSocket.readyState));
	  		}

		  	return true;
		  }

		  function webSocketStateToString(state)
		  {
		  	switch(state)
		  	{
		  		case 0 : return "CONNECTING";
		  		case 1 : return "OPEN";
		  		case 2 : return "CLOSING";
		  		case 3 : return "CLOSED";
		  	}
		  	return "INVALID";
		  }

		  this.onWebSocketError = function(error) {
		    this.log('WebSocket onError' + this.id + ': Sorry, but there is some problem with your socket or the server is down. Socket said: ' + JSON.stringify(error));
		    this.errorCallback(error);
		  }

		  this.onWebSocketOpen = function(openEvent) {
		    this.log('WebSocket onopen' + this.id + ': Connected ' + openEvent);
		    this.openCallback();
		  }

		  this.onWebSocketMessage = function(message) {
		    if(message)
		    {
		      this.log('WebSocket onmessage' + this.id + ': ' + JSON.stringify(message));
		      this.messageCallback(message.data);
		    }
		  }

		  this.onWebSocketClose = function() {
		    this.log('WebSocket onclose' + this.id );
		    this.closeCallback();
		    GSSocketCollection.RemoveSocket(this.id);
		  }







		  this.errorCallback = function(error) {
		    this.sendToUnity("onError", error);
		  }

		  this.openCallback = function() {
		    this.sendToUnity("onOpen", "");
		  }

		  this.closeCallback = function() {
		    this.sendToUnity("onClose", "");

		  }

		  this.messageCallback = function(message) {
		    this.sendToUnity("onMessage", message);
		  }


		  this.sendToUnity = function (functionName, data) {
		    var jsonData = { "socketId" : this.id, "functionName" : functionName, "data" : data };
		    SendMessage(this.gameObjectName, "ServerToClient", JSON.stringify(jsonData) );
		  }
		};




/** end gssocket **/
	};

}

mergeInto(LibraryManager.library, GameSparks);