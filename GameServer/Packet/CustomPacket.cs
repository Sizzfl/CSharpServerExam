using GameServer.Common;
using GameServer.Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Packet
{
	// byte배열 버퍼를 참조로 보관 후 Pop~ 메서드 호출 순서대로 변환 수행
	public class CustomPacket
	{
		public IPeer m_owner { get; private set; }
		public byte[] m_buffer { get; private set; }
		public int m_position { get; private set; }
		public Int16 m_protocolID { get; private set; }

		public static CustomPacket Create(Int16 protocolID)
		{
			//CustomPacket packet = new CustomPacket();
			CustomPacket packet = PacketBufferManager.Pop();
			packet.SetProtocol(protocolID);
			return packet;
		}

		public static void Destory(CustomPacket packet)
		{
			PacketBufferManager.Push(packet);
		}

		public CustomPacket(byte[] buffer, IPeer owner)
		{
			// 참조로 작업
			// 복사로 작업할 시 별도 구현 필요
			m_buffer = buffer;

			// 헤더는 읽을 필요 없으므로 헤더 다음 포지션부터 시작
			m_position = Defines.HEADER_SIZE;
			m_owner = owner;
		}

		public CustomPacket()
		{
			m_buffer = new byte[1024];
		}

		public Int16 PopProtocalID()
		{
			return PopInt16();
		}

		public void CopyTo(CustomPacket targetPacket)
		{
			targetPacket.SetProtocol(m_protocolID);
			targetPacket.Overwrite(m_buffer, m_position);
		}

		public void Overwrite(byte[] source, int position)
		{
			Array.Copy(source, m_buffer, source.Length);
			m_position = position;
		}

		public byte PopByte()
		{
			byte data = (byte)BitConverter.ToInt16(m_buffer, m_position);
			m_position += sizeof(byte);
			return data;
		}

		public Int16 PopInt16()
		{
			Int16 data = BitConverter.ToInt16(m_buffer, m_position);
			m_position += sizeof(Int16);
			return data;
		}

		public Int32 PopInt32()
		{
			Int32 data = BitConverter.ToInt32(m_buffer, m_position);
			m_position += sizeof(Int32);
			return data;
		}

		public string PopString()
		{
			// 문자열 길이 --> 최대 2바이트
			// 문자열의 길이를 재는 것이므로 음수값 없음. Unsigned로 잡았음
			UInt16 len = BitConverter.ToUInt16(m_buffer, m_position);
			m_position += sizeof(UInt16);

			// 인코딩은 UTF8로
			string data = Encoding.UTF8.GetString(m_buffer, m_position, len);
			m_position += len;

			return data;
		}

		public void SetProtocol(Int16 protocolID)
		{
			m_protocolID = protocolID;

			// 데이터부터 넣을 수 있도록 포지션을 헤더 다음으로 잡음
			m_position = Defines.HEADER_SIZE;

			PushInt16(protocolID);
		}

		public void RecordSizeToHeader()
		{
			// 헤더를 제외한 원본 사이즈
			Int16 bodySize = (Int16)(m_position - Defines.HEADER_SIZE);

			byte[] header = BitConverter.GetBytes(bodySize);
			header.CopyTo(m_buffer, 0);
		}

		public void PushInt16(Int16 data)
		{
			byte[] tempBuffer = BitConverter.GetBytes(data);
			tempBuffer.CopyTo(m_buffer, m_position);
			m_position += tempBuffer.Length;
		}

		public void Push(byte data)
		{
			byte[] tempBuffer = BitConverter.GetBytes(data);
			tempBuffer.CopyTo(m_buffer, m_position);
			m_position += sizeof(byte);
		}

		public void Push(Int16 data)
		{
			byte[] tempBuffer = BitConverter.GetBytes(data);
			tempBuffer.CopyTo(m_buffer, m_position);
			m_position += tempBuffer.Length;
		}

		public void Push(Int32 data)
		{
			byte[] tempBuffer = BitConverter.GetBytes(data);
			tempBuffer.CopyTo(m_buffer, m_position);
			m_position += tempBuffer.Length;
		}

		public void Push(string data)
		{
			byte[] tempBuffer = Encoding.UTF8.GetBytes(data);

			UInt16 len = (UInt16)tempBuffer.Length;

			byte[] lenBuffer = BitConverter.GetBytes(len);
			lenBuffer.CopyTo(m_buffer, m_position);
			m_position += sizeof(UInt16);

			tempBuffer.CopyTo(m_buffer, m_position);
			m_position += tempBuffer.Length;
		}

		public void Push(byte[] data)
		{
			byte[] tempBuffer = data;
			tempBuffer.CopyTo(m_buffer, m_position);
			m_position += tempBuffer.Length;
		}
	}
}
