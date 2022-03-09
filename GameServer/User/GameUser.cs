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
		PacketHandler m_packetHandler;

		public GameUser(CustomUserToken token)
		{
			m_token = token;
			m_token.SetPeer(this);

			m_packetHandler = new PacketHandler(token);
		}

		void IPeer.OnPacket(byte[] buffer)
		{
			byte[] packetClone = new byte[1024];
			Array.Copy(buffer, packetClone, buffer.Length);

			CustomPacket packet = new CustomPacket(packetClone, this);
			Program.mainGame.EnqueuePacket(packet, this);
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
			m_packetHandler.ParsePacket(packet);
		}
	}
}
