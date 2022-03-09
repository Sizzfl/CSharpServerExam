using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Common
{
	public enum PT : short
	{
		PT_LOGIN = 0,
		PT_CS_CHAT_REQ = 1,
		PT_SC_CHAT_ACK = 2,
		PT_CS_LOGIN_REQ = 3,
		PT_SC_LOGIN_ACK = 4,

		PT_MAX
	}
}
