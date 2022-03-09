using GameServer.Packet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Common
{
	[Serializable]
	[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Unicode)]
	public class PT_CS_CHAT_REQ : PacketData<PT_CS_CHAT_REQ>
	{
		public UInt16 chatType;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
		public string chatStr;

		public PT_CS_CHAT_REQ() { }
	}

	[Serializable]
	[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Unicode)]
	public class PT_SC_CHAT_ACK : PacketData<PT_SC_CHAT_ACK>
	{
		public UInt16 chatType;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
		public string chatStr;

		public PT_SC_CHAT_ACK() { }
	}

	[Serializable]
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public class PT_CS_LOGIN_REQ : PacketData<PT_CS_LOGIN_REQ>
	{

	}

	[Serializable]
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public class PT_SC_LOGIN_ACK : PacketData<PT_SC_LOGIN_ACK>
	{

	}
}
