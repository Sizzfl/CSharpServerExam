using GameServer.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Manager
{
	class Defines
	{
		// 헤더 사이즈 ==> 데이터 본문의 사이즈
		// 데이터가 2바이트 (== 16비트) 를 넘어갈 경우 헤더 사이즈를 4로 잡을 것
		public static readonly short HEADER_SIZE = 2;
	}

	class PacketResolver
	{
		public delegate void OnCompletedResolveCallback(byte[] buffer);

		// 패킷의 사이즈
		int m_packetSize;

		// 패킷 버퍼
		byte[] m_packetBuffer = new byte[1024];

		// 받아온 패킷 버퍼의 위치를 가리킴. 패킷 완성 뒤에는 0으로 초기화할 것.
		int m_currentBufferPos;

		// 읽어야 할 위치
		int m_positionToRead;

		// 남은 사이즈
		int m_remainByteSize;

		// 생성자
		public PacketResolver()
		{
			m_packetSize = 0;
			m_currentBufferPos = 0;
			m_positionToRead = 0;
			m_remainByteSize = 0;
		}

		// 목표지점 (dest)로 설정된 위치까지의 바이트를 원본으로부터 복사
		// 데이터가 모지랄 경우 현재 남은 바이트까지만 복사
		// 다 읽었으면 true, 데이터가 모지래서 못읽었으면 false
		bool readUntil(byte[] buffer, ref int srcPos, int offset, int transferred)
		{
			if(m_currentBufferPos >= offset + transferred)
			{
				// 다 읽음. 읽을 데이터 없음
				return false;
			}

			// 읽어야되는 바이트
			// 데이터가 쪼개져서 올 경우 이전에 읽은 값을 빼서 그 다음부터 읽을 수 있게끔 계산
			int copySize = m_positionToRead - m_currentBufferPos;
			
			// 계산된 바이트 사이즈보다 남은 사이즈 적다면 그냥 남은 사이즈로 잡아줌
			if (m_remainByteSize < copySize)
				copySize = m_remainByteSize;

			// 버퍼에 복사함
			// 원본 버퍼, 원본 버퍼 읽을 위치, 복사할 버퍼, 복사할 버퍼 읽을 위치, 읽어올 사이즈
			Array.Copy(buffer, srcPos, m_packetBuffer, m_currentBufferPos, copySize);

			// 복사 후 원본의 버퍼 읽을 위치를 사이즈만큼 이동
			// 원본 버퍼 변수를 ref로 잡은 이유
			srcPos += copySize;

			// 목표 타겟 버퍼의 읽을 포지션도 이동
			m_currentBufferPos += copySize;

			// 남은 바이트 수 계산
			m_remainByteSize -= copySize;

			// 데이터 읽을 포지션이 목표지점에 도달하지 못할 경우
			if (m_currentBufferPos < m_positionToRead)
				return false;

			return true;
		}

		// 클라이언트로부터 데이터 수신 시 호출
		// 하나의 패킷을 제대로 완성할 때 까지 반복 후, 완성되면 콜백을 호출함
		public void OnReceive(byte[] buffer, int offset, int transferred, OnCompletedResolveCallback callback)
		{
			// 수신한 패킷 사이즈
			m_remainByteSize = transferred;

			// 원본 버퍼의 포지션
			// 원본 패킷이 여러개로 쪼개져 올 경우 읽어올 위치를 계속 변경해주기 위한 변수
			int srcPos = offset;

			// 남은 데이터가 없을 때 까지 반복
			while(m_remainByteSize > 0)
			{
				bool isCompleted = false;

				// 헤더만큼 못읽은 경우 헤더를 먼저 읽어옴
				if(m_currentBufferPos < Defines.HEADER_SIZE)
				{
					// 헤더 위치까지 도달하도록 목표치 설정
					m_positionToRead = Defines.HEADER_SIZE;

					isCompleted = readUntil(buffer, ref srcPos, offset, transferred);
					if (!isCompleted)
						return; // 아직 다 읽지 못했으므로 다음 receive를 기다림

					// 헤더 하나를 온전히 읽어왔으므로 패킷의 사이즈를 구함
					m_packetSize = GetBodySize();

					// 다음 목표 지점은 (헤더 + 패킷 사이즈)가 됨
					m_positionToRead = m_packetSize + Defines.HEADER_SIZE;
				}

				isCompleted = readUntil(buffer, ref srcPos, offset, transferred);
				if(isCompleted)
				{
					callback(m_packetBuffer);

					ClearBuffer();
				}
			}
		}

		int GetBodySize()
		{
			// 헤더 타입의 바이트만큼 읽어와 패킷의 사이즈를 리턴
			Type type = Defines.HEADER_SIZE.GetType();
			if (type.Equals(typeof(Int16)))
				return BitConverter.ToInt16(m_packetBuffer, 0);

			return BitConverter.ToInt32(m_packetBuffer, 0);
		}

		void ClearBuffer()
		{
			Array.Clear(m_packetBuffer, 0, m_packetBuffer.Length);

			m_currentBufferPos = 0;
			m_packetSize = 0;
		}
	}
}
