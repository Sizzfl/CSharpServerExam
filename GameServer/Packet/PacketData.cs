using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Packet
{
	[Serializable]
	[StructLayout(LayoutKind.Sequential, Pack = 1)] // 마샤링을 위해 붙이는 주석 (1바이트 단위로 데이터의 크기를 맞춤)
	public class PacketData<T> where T : class
	{
		public PacketData() { }

		// 패킷 객체 -> 바이트 배열
		public byte[] Serialize()
		{
			var size = Marshal.SizeOf(typeof(T));
			var packetClone = new byte[size];
			var ptr = Marshal.AllocHGlobal(size);

			// 해당 제네릭을 상속받은 패킷을 ptr로 변환함
			Marshal.StructureToPtr(this, ptr, true);
			// 변환된 ptr를 byte배열로 복사. 시작 0, size 만큼
			Marshal.Copy(ptr, packetClone, 0, size);

			// 할당한 메모리 해제
			Marshal.FreeHGlobal(ptr);

			return packetClone;
		}

		// 바이트 배열 -> 패킷 객체
		public static T Deserialize(byte[] pack)
		{
			var size = Marshal.SizeOf(typeof(T));
			var ptr = Marshal.AllocHGlobal(size);

			// 들어온 바이트배열을 시작위치 0부터 사이즈만큼 ptr에 복사
			Marshal.Copy(pack, 0, ptr, size);
			var packetClone = (T)Marshal.PtrToStructure(ptr, typeof(T));

			Marshal.FreeHGlobal(ptr);

			return packetClone;
		}
	}
}
