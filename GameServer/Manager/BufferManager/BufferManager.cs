using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Manager
{
	internal class BufferManager
	{
		// 이 버퍼 풀에서 제어할 총 바이트 수
		int m_numBytes;
		byte[] m_buffer;
		Stack<int> m_freeIndexPool;
		int m_currentIndex;
		int m_bufferSize;

		public BufferManager(int totalBytes, int bufferSize)
		{
			// 초기화
			m_numBytes = totalBytes;
			m_currentIndex = 0;
			m_bufferSize = bufferSize;
			m_freeIndexPool = new Stack<int>();
		}

		public void InitBuffer()
		{
			// 버퍼 (바이트배열) 의 초기 메모리 크기를 잡아줌
			m_buffer = new byte[m_numBytes];
		}

		public bool SetBuffer(SocketAsyncEventArgs eventArgs)
		{
			// SocketAsyncEventArgs 객체에 버퍼를 잡아줌 (SetBuffer)
			if(m_freeIndexPool.Count > 0)
			{
				eventArgs.SetBuffer(m_buffer, m_freeIndexPool.Pop(), m_bufferSize);
			}
			else
			{
				if((m_numBytes - m_bufferSize) < m_currentIndex)
				{
					return false;
				}

				// 0부터 일정 크기만큼 버퍼의 사이즈를 잡아준다.
				eventArgs.SetBuffer(m_buffer, m_currentIndex, m_bufferSize);
				// 다음 버퍼는 이미 할당된 사이즈 바로 다음부터 할당되도록 시작 인덱스를 바꿔준다.
				// 즉, 다음 버퍼의 위치를 가리키도록 바꿔줌
				m_currentIndex += m_bufferSize;
			}

			return true;
		}

		public void FreeBuffer(SocketAsyncEventArgs eventArgs)
		{
			// 사용하지 않는 버퍼를 반환
			// 프로그램 시작 시 최대 동시 접속 수치만큼 버퍼 할당 후 서버를 끌때까지 물고 있을 것이기 때문에
			// 실상 사용되지 않을 함수. SocketAsyncEventArgs를 풀링하여 가지고 있을 것이므로 호출이 안될 예정
			m_freeIndexPool.Push(eventArgs.Offset);
			eventArgs.SetBuffer(null, 0, 0);
		}
	}
}
