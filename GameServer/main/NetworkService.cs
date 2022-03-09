using GameServer.Manager;
using GameServer.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GameServer.main
{
	public class NetworkService
	{
		int m_connectedCount;
		// 클라이언트 접속 리스너
		Listener m_clientListener;

		// 메시지(패킷) 송수신 이벤트
		SocketAsyncEventArgsPool m_receiveEventArgsPool;
		SocketAsyncEventArgsPool m_sendEventArgsPool;

		// 패킷 송수신 시 소켓에서 사용할 버퍼 관리
		BufferManager m_bufferManager;

		// 클라이언트 접속 완료 시 호출되는 콜백 딜리게이트
		public delegate void SessionHandler(CustomUserToken token);
		public SessionHandler m_sessionCreateCallback { get; set; }

		int m_maxConnection;
		int m_bufferSize;
		readonly int m_preAllocCount = 2;

		public NetworkService()
		{
			m_connectedCount = 0;
			m_sessionCreateCallback = null;
		}

		public void Init()
		{
			m_maxConnection = 10000;
			m_bufferSize = 1024;

			// 동시커넥션 할 클라이언트 갯수 * 클라이언트별 패킷 버퍼의 사이즈 * 미리 잡아놓을 풀 사이즈
			// 풀 사이즈는 read, write 총 2개
			m_bufferManager = new BufferManager(m_maxConnection * m_bufferSize * m_preAllocCount, m_bufferSize);
			m_receiveEventArgsPool = new SocketAsyncEventArgsPool(m_maxConnection);
			m_sendEventArgsPool = new SocketAsyncEventArgsPool(m_maxConnection);

			m_bufferManager.InitBuffer();

			SocketAsyncEventArgs args;

			for (int i = 0; i < m_maxConnection; i++)
			{
				// 동일한 소켓에 send 및 receive
				// 유저 토큰은 클라이언트 세션 당 한개 생성 후
				// 각 eventArgs에서 동일한 토큰을 가지게 한 후 참조하도록 함
				CustomUserToken token = new CustomUserToken();

				{
					// receive pool
					args = new SocketAsyncEventArgs();
					args.Completed += new EventHandler<SocketAsyncEventArgs>(OnReceiveCompleted);
					args.UserToken = token;
					m_bufferManager.SetBuffer(args);
					m_receiveEventArgsPool.Push(args);
				}

				{
					// send pool
					args = new SocketAsyncEventArgs();
					args.Completed += new EventHandler<SocketAsyncEventArgs>(OnSendCompleted);
					args.UserToken = token;
					m_bufferManager.SetBuffer(args);
					m_sendEventArgsPool.Push(args);
				}
			}
		}

		public void Listen(string host, int port, int backlog)
		{
			m_clientListener = new Listener();

			m_clientListener.m_OnNewClientCallback += OnNewClient;
			m_clientListener.start(host, port, backlog);
		}

		public void OnConnectCompleted(Socket sock, CustomUserToken token)
		{
			// Send, Receive때와 달리, 풀을 사용하지 않고 바로 할당해준다.
			// 풀링은 서버 -> 클라 통신에 사용하기 위해 만든 것이고,
			// 클라이언트 -> 서버는 EventArgs가 2개밖에 필요하지 않기 때문에 그냥 할당해서 써준다.
			// 이는 서버간 연결시에도 동일 (분산 프로세스 서버 기준)

			SocketAsyncEventArgs receiveArgs = new SocketAsyncEventArgs();
			receiveArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnReceiveCompleted);
			receiveArgs.UserToken = token;
			receiveArgs.SetBuffer(new byte[1024], 0, 1024);

			SocketAsyncEventArgs sendArgs = new SocketAsyncEventArgs();
			sendArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnSendCompleted);
			sendArgs.UserToken = token;
			sendArgs.SetBuffer(new byte[1024], 0, 1024);

			OnBeginReceive(sock, receiveArgs, sendArgs);
		}

		private void OnNewClient(Socket cliSock, object token)
		{
			Interlocked.Increment(ref m_connectedCount);

			Console.WriteLine($"[{Thread.CurrentThread.ManagedThreadId}] A client connected. handle : {cliSock.Handle}, count : {m_connectedCount}");

			// 이벤트 풀에서 꺼내와 사용
			SocketAsyncEventArgs receiveArgs = m_receiveEventArgsPool.Pop();
			SocketAsyncEventArgs sendArgs = m_sendEventArgsPool.Pop();

			// SocketAsyncEventArgs를 생성할 때 만들어두었던 UserToken를 콜백 메서드의 패러미터로 넣어줌.
			CustomUserToken userToken = null;
			if(m_sessionCreateCallback != null)
			{
				userToken = receiveArgs.UserToken as CustomUserToken;
				m_sessionCreateCallback(userToken);
			}

			// 클라이언트로부터 데이터를 수신 대기
			OnBeginReceive(cliSock, receiveArgs, sendArgs);
		}

		private void OnBeginReceive(Socket cliSock, SocketAsyncEventArgs receiveArgs, SocketAsyncEventArgs sendArgs)
		{
			// 전체적인 구조는 Aceept와 유사함
			// receive, send 둘 중 아무거나 선택해도 됨. 어짜피 userToken은 같은 것
			CustomUserToken token = receiveArgs.UserToken as CustomUserToken;
			token.SetEventArgs(receiveArgs, sendArgs);

			// 생성된 클라이언트 소켓을 보관, 통신 시 사용
			token.m_sock = cliSock;

			// 데이터 수신용 메서드 호출
			// 비동기 수신 시 스레드 대기 중에 있다가 Completed에 더해준 메서드가 호출
			// 동기 완료 시 직접 호출
			bool pending = cliSock.ReceiveAsync(receiveArgs);
			if(!pending)
			{
				OnProcessReceive(receiveArgs);
			}
		}

		private void OnProcessReceive(SocketAsyncEventArgs e)
		{
			CustomUserToken token = e.UserToken as CustomUserToken;

			if(e.BytesTransferred > 0 && SocketError.Success == e.SocketError)
			{
				// e.Buffer == 클라이언트로부터 수신한 데이터 원본 바이트 배열
				// e.Offset == 버퍼 포지션. 즉, 데이터의 시작위치를 나타내는 정수값
				// e.BytesTransferred == 수신된 바이트 사이즈 정수
				token.OnReceive(e.Buffer, e.Offset, e.BytesTransferred);

				// 수신한 뒤 ReceiveAsync 재호출
				// 한번의 ReceiveAsync로는 모든 데이터를 다 받기 힘들다. 클라이언트 송신이 한번 쉬어지거나,
				// 통신 환경 등에 의해 잘개 쪼개어져 여러번 송신할 수 있기 때문.
				bool pending = token.m_sock.ReceiveAsync(e);
				if (!pending)
					OnProcessReceive(e);
			}
			else
			{
				Console.WriteLine($"error {e.SocketError}, transferred {e.BytesTransferred}");
				OnCloseClientSocket(token);
			}
		}

		private void OnCloseClientSocket(CustomUserToken token)
		{
			token.OnRemove();

			// EventArgs를 풀에 반환한다.
			// 버퍼는 반환하지 않는다. 어짜피 다음에 재사용할때 물고있던 버퍼를 그대로 사용할 수 있기 때문
			if (null != m_receiveEventArgsPool)
				m_receiveEventArgsPool.Push(token.m_receiveArgs);
			if (null != m_sendEventArgsPool)
				m_sendEventArgsPool.Push(token.m_sendArgs);
		}

		private void OnReceiveCompleted (object sender, SocketAsyncEventArgs e)
		{
			if(SocketAsyncOperation.Receive == e.LastOperation)
			{
				OnProcessReceive(e);
				return;
			}

			throw new ArgumentException("The last operation completed on the socket was not a receive");
		}

		private void OnSendCompleted(object sender, SocketAsyncEventArgs e)
		{
			CustomUserToken token = e.UserToken as CustomUserToken;
			token.OnProcessSend(e);
		}
	}
}
