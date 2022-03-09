using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Common
{
	class PT_CS_CHAT_REQ
	{
		UInt16 chatType;
		string chatStr;
	}

	class PT_CS_CHAT_ACK
	{
		UInt16 chatType;
		string chatStr;
	}

	class PT_CS_LOGIN_REQ
	{

	}

	class PT_SC_LOGIN_ACK
	{

	}
}
