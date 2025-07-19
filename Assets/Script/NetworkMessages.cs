using System;  // Serializable 속성을 사용하기 위한 네임스페이스

// 게임에서 사용되는 모든 메시지 타입을 정의하는 열거형
public enum MessageType
{
    Join,       // 플레이어가 게임에 참여할 때 사용
    Place,      // 돌을 배치할 때 사용
    GameState,  // 현재 게임 상태를 전달할 때 사용
    GameOver,   // 게임이 종료되었을 때 사용
    PlayerTurn, // 현재 플레이어의 턴을 알릴 때 사용
    Ping,       // 연결 상태를 확인하기 위한 요청
    Pong        // Ping에 대한 응답
}

// 클라이언트와 서버 간에 주고받는 메시지의 구조를 정의하는 클래스
[Serializable]  // JSON 직렬화/역직렬화를 위한 속성
public class GameMessage
{
    public MessageType Type;    // 메시지의 종류를 나타내는 열거형 값
    public int X;              // 돌을 놓을 X 좌표
    public int Y;              // 돌을 놓을 Y 좌표
    public int PlayerID;       // 플레이어를 구분하는 ID (1 또는 2)
    public string Data;        // 추가적인 데이터를 전달하기 위한 문자열 필드
}