using GameServer.main;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Network
{
	// IP EndPoint 정보를 받아 서버에 접속시킴
	// 접속하려는 서버 하나당 인스턴스 하나 생성
	public class Connector
	{
		public delegate void ConnectedHandler(CustomUserToken token);
		public ConnectedHandler m_connectedCallback { get; set; }

		// 서버와의 연결을 위한 소켓
		Socket m_sock;

		NetworkService m_networkService;

		public Connector(NetworkService networkService)
		{
			m_networkService = networkService;
			m_connectedCallback = null;
		}

		public void Connect(IPEndPoint remoteEndPoint)
		{
			m_sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

			// 비동기 접속을 위한 AsyncEventArgs
			SocketAsyncEventArgs eventArgs = new SocketAsyncEventArgs();
			eventArgs.Completed += OnConnectCompleted;
			eventArgs.RemoteEndPoint = remoteEndPoint;

			bool pending = m_sock.ConnectAsync(eventArgs);
			if(!pending)
			{
				OnConnectCompleted(null, eventArgs);
			}
		}

		private void OnConnectCompleted(object sender, SocketAsyncEventArgs e)
		{
			if(SocketError.Success == e.SocketError)
			{
				CustomUserToken token = new CustomUserToken();

				m_networkService.OnConnectCompleted(m_sock, token);

				if(m_connectedCallback != null)
				{
					m_connectedCallback(token);
				}
			}
			else
			{
				Console.WriteLine($"Failed to Connect : {e.SocketError}");
			}
		}
	}
}
