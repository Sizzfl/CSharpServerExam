using GameServer.Packet;
using GameServer.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GameServer
{
	class GameServer
	{
		readonly object m_operLock;
		Queue<CustomPacket> m_operQueue;

		//Task m_logicTask;
		Thread m_logicTask;
		AutoResetEvent m_loopEvent;

		public GameServer()
		{
			m_operLock = new object();
			m_loopEvent = new AutoResetEvent(false);
			m_operQueue = new Queue<CustomPacket>();

			//m_logicTask = new Task(GameLoop);
			m_logicTask = new Thread(GameLoop);
			m_logicTask.Start();
			//m_logicTask.Wait();
		}

		// 패킷 처리 담당 루프
		private void GameLoop()
		{
			while(true)
			{
				CustomPacket packet = null;
				lock(m_operLock)
				{
					if (m_operQueue.Count > 0)
						packet = m_operQueue.Dequeue();
				}

				if (packet != null)
					ProcessReceive(packet);

				if (m_operQueue.Count <= 0)
					m_loopEvent.WaitOne();
			}
		}

		public void EnqueuePacket(CustomPacket pack, GameUser user)
		{
			lock(m_operLock)
			{
				m_operQueue.Enqueue(pack);
				m_loopEvent.Set();
			}
		}

		private void ProcessReceive(CustomPacket packet)
		{
			packet.m_owner.OnProcessUserOperation(packet);
		}

		public void UserDisconnect(GameUser user)
		{

		}
	}
}
