using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace GameServer.MySQL
{
	class DBInfo
	{
		// Example
		public string host { get; private set; }
		public int port { get; private set; }
		public string id { get; private set; }
		public string password { get; private set; }
		public string connectAddress { get; private set; }

		public DBInfo()
		{
			host = "localhost";
			port = 3308;
			id = "root";
			password = "root";

			connectAddress = $"host={host}, port={port}, UID={id}, Passward={password}";
		}

		public void SelectTable(string queryStr)
		{
			using (MySqlConnection conn = new MySqlConnection(connectAddress))
			{
				conn.Open();

				MySqlCommand query = new MySqlCommand(queryStr, conn);
				MySqlDataReader queryResult = query.ExecuteReader();
			}
		}
	}
}
