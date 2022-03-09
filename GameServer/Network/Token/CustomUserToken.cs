using GameServer.Common;
using GameServer.Manager;
using GameServer.Packet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GameServer.Network
{
	public class CustomUserToken
	{
		static int sSentCount = 0;
		static object sLockCount = new object();

		public Socket m_sock { get; set; }

		public SocketAsyncEventArgs m_receiveArgs { get; private set; }
		public SocketAsyncEventArgs m_sendArgs { get; private set; }

		// 패킷 바이트 해독기
		PacketResolver m_packetResolver;

		// 세션 객체.
		IPeer m_peer;

		// 전송할 패킷 보관용 큐. 송신 처리용.
		Queue<CustomPacket> m_sendQueue;
		// 패킷보관 큐의 락 처리를 위한 객체
		private object m_lockQueue;

		public CustomUserToken()
		{
			m_lockQueue = new object();
			m_packetResolver = new PacketResolver();
			m_peer = null;
			m_sendQueue = new Queue<CustomPacket>();
		}

		public void SetPeer(IPeer p)
		{
			m_peer = p;
		}

		public void SetEventArgs(SocketAsyncEventArgs receiveArgs, SocketAsyncEventArgs sendArgs)
		{
			m_receiveArgs = receiveArgs;
			m_sendArgs = sendArgs;
		}

		// PacketResolver를 따로 호출해주는 이유
		// 추후 확장성을 고려하여 다른 Resolver도 만들게 될 경우 유저 토큰쪽 수정을 최소화하기 위함
		public void OnReceive(byte[] buffer, int offset, int transferred)
		{
			m_packetResolver.OnReceive(buffer, offset, transferred, OnPacket);
		}

		private void OnPacket(byte[] buffer)
		{
			if(null != m_peer)
			{
				m_peer.OnPacket(buffer);
			}
		}

		public void OnRemove()
		{
			m_sendQueue.Clear();
			if(null != m_peer)
			{
				m_peer.OnRemove();
			}
		}

		// 패킷 전송 메서드
		public void SendPacket(CustomPacket packet)
		{
			CustomPacket clonePacket = new CustomPacket();
			packet.CopyTo(clonePacket);

			lock(m_lockQueue)
			{
				// 큐가 비어있다면 큐에 추가 후 바로 비동기 송신 메서드를 호출한다.
				if(m_sendQueue.Count <= 0)
				{
					m_sendQueue.Enqueue(clonePacket);
					StartSend();
					return;
				}
				else
				{
					// 큐가 비어있지 않다면 아직 이전 패킷의 전송이 완료되지 않은 것임.
					// 이전 SendAsync의 호출이 완료된 이후 큐를 검사하여 데이터가 있으면 SendAsync를 호출하여 전송할 것임.
					Console.WriteLine($"Queue is not empty. Copy and Enqueue the packet. protocol ID : {packet.m_protocolID}");
					m_sendQueue.Enqueue(clonePacket);
				}
			}
		}

		void StartSend()
		{
			lock(m_lockQueue)
			{
				// 데이터만 가져오고 큐에서 제거하진 않는다.
				CustomPacket packet = m_sendQueue.Peek();

				// 헤더에 패킷 사이즈 기록
				packet.RecordSizeToHeader();

				// 보낼 패킷 사이즈만큼 버퍼 크기 설정
				m_sendArgs.SetBuffer(m_sendArgs.Offset, packet.m_position);

				// 패킷 내용을 SocketAsyncEventArgs 버퍼에 복사
				Array.Copy(packet.m_buffer, 0, m_sendArgs.Buffer, m_sendArgs.Offset, packet.m_position);

				// 비동기 전송 시작
				bool pending = m_sock.SendAsync(m_sendArgs);
				if(!pending)
				{
					OnProcessSend(m_sendArgs);
				}
			}
		}

		// 비동기 송신 완료 시 호출
		public void OnProcessSend(SocketAsyncEventArgs eventArgs)
		{
			if(eventArgs.BytesTransferred <= 0 || SocketError.Success != eventArgs.SocketError)
			{
				return;
			}

			lock(m_lockQueue)
			{
				if(m_sendQueue.Count <= 0)
				{
					throw new Exception("Sending queue count is less then zero");
				}

				// 재전송 검토. 패킷 하나를 다 못보냈을 경우
				int size = m_sendQueue.Peek().m_position;
				if(eventArgs.BytesTransferred != size)
				{
					Console.WriteLine($"Need to send more. transferred {eventArgs.BytesTransferred}, packet size {size}");
					return;
				}

				lock(sLockCount)
				{
					// 송신 데이터 관련 기록
					++sSentCount;
					Console.WriteLine($"Process send : {eventArgs.SocketError}, transferred {eventArgs.BytesTransferred}, sent count {sSentCount}");
				}

				// 전송 완료된 패킷을 큐에서 제거
				m_sendQueue.Dequeue();

				// 아직 전송하지 않은 패킷 존재 시 재요청
				if (m_sendQueue.Count > 0)
					StartSend();
			}
		}

		public void Disconnect()
		{
			try
			{
				m_sock.Shutdown(SocketShutdown.Send);
			}
			catch (Exception) { }

			m_sock.Close();
		}

		public void StartKeepAlive()
		{
			Timer keepAlive = new Timer((object e) =>
			{
				CustomPacket packet = CustomPacket.Create(0);
				packet.Push(0);
				SendPacket(packet);
			}, null, 0, 3000);
		}
	}
}
