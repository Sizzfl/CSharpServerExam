using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Manager
{
	class SocketAsyncEventArgsPool
	{
		Stack<SocketAsyncEventArgs> m_pool;

		public SocketAsyncEventArgsPool(int capacity)
		{
			// 스택의 총 용량 (캐파) 를 받아 풀에 할당해줌
			m_pool = new Stack<SocketAsyncEventArgs>(capacity);
		}

		public void Push(SocketAsyncEventArgs item)
		{
			if(null == item)
			{
				throw new ArgumentNullException("Items added to a SocketAsyncEventArgsPool cannot be null");
			}

			// 이벤트 풀은 공용자원 (여러 클라이언트에 돌려써야되는 자원)이므로 삽입 삭제 시 락을 걸어줌
			lock(m_pool)
			{
				m_pool.Push(item);
			}
		}

		public SocketAsyncEventArgs Pop()
		{
			// 삽입 시와 마찬가지로 락을 걸어줌
			lock(m_pool)
			{
				return m_pool.Pop();
			}
		}

		public int Size { get { return m_pool.Count; } }
	}
}
