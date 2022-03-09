using GameServer.Common;
using GameServer.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Packet
{
	public class PacketHandler
	{
		CustomUserToken m_token;

		public PacketHandler(CustomUserToken token)
		{
			m_token = token;
		}

		public void ParsePacket(CustomPacket packet)
		{
			PT packetProtocolID = (PT)packet.PopProtocalID();

			switch(packetProtocolID)
			{
				case PT.PT_CS_LOGIN_REQ:
					break;
				case PT.PT_CS_CHAT_REQ:
					CS_CHAT_REQ(packet);
					break;
			}
		}

		public void CS_CHAT_REQ(CustomPacket packet)
		{
			//string text = packet.PopString();
			//Console.WriteLine($"Chat : {text}");

			PT_CS_CHAT_REQ req = PacketData<PT_CS_CHAT_REQ>.Deserialize(packet.m_buffer);

			//CustomPacket ack = CustomPacket.Create((short)PT.PT_SC_CHAT_ACK);
			//ack.Push(text);
			PT_SC_CHAT_ACK ack = new PT_SC_CHAT_ACK();
			ack.chatType = 0;
			ack.chatStr = req.chatStr;

			CustomPacket send = CustomPacket.Create((short)PT.PT_SC_CHAT_ACK);
			send.Push(ack.Serialize());

			m_token.SendPacket(send);
		}
	}
}
