using GameServer.main;
using GameServer.Manager;
using GameServer.Network;
using GameServer.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GameServer
{
	static class Program
	{
		/// <summary>
		/// 해당 애플리케이션의 주 진입점입니다.
		/// </summary>

		static List<GameUser> m_userList;
		static readonly object m_lock = new object();
		public static GameServer mainGame = new GameServer();

		static void Main()
		{
			PacketBufferManager.Init(2000);

			m_userList = new List<GameUser>();

			NetworkService mainService = new NetworkService();

			// 콜백 설정 및 초기화
			mainService.m_sessionCreateCallback += OnSessionCreated;
			mainService.Init();
			mainService.Listen("127.0.0.1", 15000, 100);

			Console.WriteLine("Server start");
			while(true)
			{
				Thread.Sleep(1000);
			}

			Console.ReadKey();
		}

		static void OnSessionCreated(CustomUserToken token)
		{
			GameUser user = new GameUser(token);
			lock(m_lock)
			{
				m_userList.Add(user);
			}
		}

		public static void RemoveUser(GameUser user)
		{
			lock(m_lock)
			{
				m_userList.Remove(user);
				mainGame.UserDisconnect(user);
			}
		}
	}
}
