using GameServer.Packet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Common
{
	/* 
	 * 서버가 사용할 경우 : 하나의 클라이언트 객체.
	 * 이 인터페이스로 구현한 객체를 세션 생성 콜백 호출시에 생성하여 리턴
	 * 풀링 여부는 개발자 마음
	*/
	/*
	 * 클라이언트가 사용할 경우 : 접속한 서버 객체를 의미.
	*/

	// 공유자원으로 접근 할 시 동기화 처리 필요 (락 걸어줘야됨)
	public interface IPeer
	{
		// 소켓 버퍼로부터 데이터 수신 후 패킷 하나를 온전히 완성했을 시 호출
		// 호출스택 ==> ReceiveAsync -> CustomUserToken.OnReceive -> Peer.OnPacket
		// 소켓 버퍼로부터 복사된 CustomUserToken의 버퍼를 참조한다.
		/*
		 * TCP 패킷 순서
		 * - 닷넷의 스레드풀에 의해 작동되므로 어느 스레드에서 호출되는지는 알 수 없으나,
		 * - 하나의 객체에 대해서는 이 메소드의 호출이 완료된 이후에 다음 패킷이 불리도록 되어있으므로
		 * - 순서가 보장이 된다. 클라이언트에서 보낸 순서대로 처리되니 안심.
		 */
		/*
		 * 주의점
		 * - 다른 Peer 객체를 참조하거나 공유자원에 접근할 경우 동기화 처리 (== 락 처리) 를 해줘야된다.
		 * - 메소드 리턴 시 버퍼가 비워지고 다음 패킷을 담을 준비를 하므로, 리턴 전에 사용할 데이터를 다 빼내줘야된다.
		 * - 메서드가 리턴된 이후에 버퍼 참조를 해서는 안됨. 참조 말고 복사를 해서 처리할 것.
		 */
		void OnPacket(byte[] buffer);
		
		// 원격 연결이 끊어진 경우 호출
		// 이 메서드가 호출된다면, 그 이후부터는 데이터 송수신 불가
		void OnRemove();
		void OnSend(CustomPacket packet);
		void OnDisconnect();
		void OnProcessUserOperation(CustomPacket packet);
	}
}
