using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Util
{
	public class Singleton<T> where T : Singleton<T>, new()
	{
		static T m_instance;

		public static T GetInstance
		{
			get
			{
				if (null == m_instance)
					m_instance = new T();

				return m_instance;
			}
		}
	}
}
