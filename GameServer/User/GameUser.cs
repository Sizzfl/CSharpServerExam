using GameServer.Common;
using GameServer.Network;
using GameServer.Packet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.User
{
	class GameUser : IPeer
	{
		CustomUserToken m_token;

		public GameUser(CustomUserToken token)
		{
			m_token = token;
			m_token.SetPeer(this);
		}

		void IPeer.OnPacket(byte[] buffer)
		{
			// 에코 채팅 관련 패킷 처리문
			CustomPacket req = new CustomPacket(buffer, this);
			PT protocolID = (PT)req.PopProtocalID();

			switch(protocolID)
			{
				case PT.PT_CS_CHAT_REQ:
					string text = req.PopString();
					Console.WriteLine($"Chat : {text}");

					CustomPacket ack = CustomPacket.Create((short)PT.PT_SC_CHAT_ACK);
					ack.Push(text);
					OnSend(ack);
				break;
			}
		}

		void IPeer.OnRemove()
		{
			Console.WriteLine("Client disconnected");

			Program.RemoveUser(this);
		}

		public void OnSend(CustomPacket packet)
		{
			m_token.SendPacket(packet);
		}

		void IPeer.OnDisconnect()
		{
			m_token.m_sock.Disconnect(false);
		}

		void IPeer.OnProcessUserOperation(CustomPacket packet)
		{

		}
	}
}
