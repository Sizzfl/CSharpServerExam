using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GameServer.Network
{
	class Listener
	{
		// 비동기 접속을 위한 이벤트 객체
		SocketAsyncEventArgs m_acceptArgs;

		// 클라이언트 접속 소켓
		Socket m_listenSocket;

		// 접속 (accept) 처리 순서 제어 이벤트 변수
		AutoResetEvent m_flowControlEvent;

		// 새 클라이언트 접속 시 호출되는 콜백 딜리게이트
		public delegate void NewClientHandler(Socket cliSock, object token);
		public NewClientHandler m_OnNewClientCallback;

		// 메서드
		public Listener()
		{
			m_OnNewClientCallback = null;
		}

		public void start(string host, int port, int backlog)
		{
			// 소켓 생성
			m_listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

			IPAddress addr;

			addr = (host == "0.0.0.0") ? IPAddress.Any : IPAddress.Parse(host);

			IPEndPoint endPoint = new IPEndPoint(addr, port);

			try
			{
				// 소켓에 호스트 정보를 bind -> Listen 메서드 호출 후 대기
				m_listenSocket.Bind(endPoint);
				m_listenSocket.Listen(backlog);

				// 객체를 할당해준 뒤 Completed에 EventHandler 객체 연결 후 패러미터로 콜백 받을 함수를 넣어줌
				m_acceptArgs = new SocketAsyncEventArgs();
				m_acceptArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnAcceptCompleted);

				//Thread listenThread = new Thread(doListen);
				//listenThread.Start();
				Task listenTask = new Task(doListen);
				listenTask.Start();
				listenTask.Wait();

				//m_listenSocket.AcceptAsync(m_acceptArgs);
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
			}
		}

		private void doListen()
		{
			// Accept 처리를 위해 이벤트 객체 선언
			m_flowControlEvent = new AutoResetEvent(false);

			while(true)
			{
				// SocketAsyncEventArgs 재사용을 위해 null로 초기화
				m_acceptArgs.AcceptSocket = null;

				bool pending = true;

				try
				{
					// 클라이언트 접속 대기
					// 비동기 메서드이므로 논블로킹으로 리턴되며, 콜백 메서드를 통해 접속통보 받음
					// 동기적으로 수행이 완료되는 경우도 있으니 리턴 값을 확인하여 분기 처리해줄 필요가 있다.
					pending = m_listenSocket.AcceptAsync(m_acceptArgs);
				}
				catch(Exception e)
				{
					Console.WriteLine(e.Message);
					continue;
				}

				// 즉시 완료 (위에서 말한 동기적 처리) 가 되면 pending값이 false이다.
				// 이러면 이벤트가 발생하지 않으므로 따로 콜백 메서드를 호출해줘야 한다.
				if(!pending)
				{
					OnAcceptCompleted(null, m_acceptArgs);
				}

				// 클라이언트 접속 처리 완료 시 다시 루프 실행. 이벤트 객체의 신호를 전달받아야 한다.
				m_flowControlEvent.WaitOne();
			}
		}

		private void OnAcceptCompleted(object sender, SocketAsyncEventArgs eventArgs)
		{
			if(SocketError.Success == eventArgs.SocketError)
			{
				// 패러미터로 들어온 소켓을 보관
				Socket cliSock = eventArgs.AcceptSocket;

				// 다음 연결을 받음
				m_flowControlEvent.Set();

				// 해당 클래스는 Listen 클래스이므로 Accept까지의 역할만 수행함.
				// 이후 처리는 외부로 돌리기 위해 콜백 호출
				// 소켓 처리부와 컨텐츠 구현부를 나누기 위함
				// 컨텐츠와 달리 소켓쪽은 변경이 자주 일어나지 않기 때문에 분리시킨다.
				// 클래스 설계상 Listen 역할만 따로 분리하여 하나의 클래스로 잡기 위함.
				if(m_OnNewClientCallback != null)
				{
					m_OnNewClientCallback(cliSock, eventArgs.UserToken);
				}

				return;
			}
			else
			{
				// Accept 실패
				Console.WriteLine("Failed to accept client");
			}

			// 다음 연결을 받음
			m_flowControlEvent.Set();
		}
	}
}
