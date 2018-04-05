﻿using Alex.Networking.Java.Util;

namespace Alex.Networking.Java.Packets.Play
{
	public class ChatMessagePacket : Packet<ChatMessagePacket>
	{
		public const byte Chat = 0;
		public const byte System = 1;
		public const byte Hotbar = 2;

		public ChatMessagePacket()
		{
			PacketId = 0x0E;
		}

		public string Message;
		public byte Position = Chat;
		public override void Decode(MinecraftStream stream)
		{
			Message = stream.ReadString();
		}

		public override void Encode(MinecraftStream stream)
		{
			stream.WriteString(Message);
			stream.WriteByte(Position);
		}
	}
}