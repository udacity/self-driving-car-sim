#region License
/*
 * Packet.cs
 *
 * The MIT License
 *
 * Copyright (c) 2014 Fabio Panettieri
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */
#endregion

namespace SocketIO
{
	public class Packet
	{
		public EnginePacketType enginePacketType;
		public SocketPacketType socketPacketType;

		public int attachments;
		public string nsp;
		public int id;
		public JSONObject json;

		public Packet() : this(EnginePacketType.UNKNOWN, SocketPacketType.UNKNOWN, -1, "/", -1, null) { }
		public Packet(EnginePacketType enginePacketType) : this(enginePacketType, SocketPacketType.UNKNOWN, -1, "/", -1, null) { }

		public Packet(EnginePacketType enginePacketType, SocketPacketType socketPacketType, int attachments, string nsp, int id, JSONObject json)
		{
			this.enginePacketType = enginePacketType;
			this.socketPacketType = socketPacketType;
			this.attachments = attachments;
			this.nsp = nsp;
			this.id = id;
			this.json = json;
		}

		public override string ToString()
		{
			return string.Format("[Packet: enginePacketType={0}, socketPacketType={1}, attachments={2}, nsp={3}, id={4}, json={5}]", enginePacketType, socketPacketType, attachments, nsp, id, json);
		}
	}
}
