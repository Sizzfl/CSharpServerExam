using GameServer.Packet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Manager
{
	public class PacketBufferManager
	{
		static object m_lockBuffer = new object();
		static Stack<CustomPacket> m_pool;
		static int m_poolCapacity;

		public static void Init(int capacity)
		{
			m_pool = new Stack<CustomPacket>();
			m_poolCapacity = capacity;
			Allocate();
		}

		static void Allocate()
		{
			for(int i = 0; i < m_poolCapacity; i++)
			{
				m_pool.Push(new CustomPacket());
			}
		}

		public static CustomPacket Pop()
		{
			lock(m_lockBuffer)
			{
				if(m_pool.Count <= 0)
				{
					Console.WriteLine("Packet Buffer Manager Reallocate");
					Allocate();
				}

				return m_pool.Pop();
			}
		}

		public static void Push(CustomPacket packet)
		{
			lock(m_lockBuffer)
			{
				m_pool.Push(packet);
			}
		}
	}
}
