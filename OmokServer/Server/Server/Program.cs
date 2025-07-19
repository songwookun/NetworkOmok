using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;


//게임에서 사용되는 메시지 타입 정의하는 열거형
public enum MessageType
{
    Join,       //플레이어 입장
    Place,      //돌 배치
    GameState,  //게임 상태
    GameOver,   //게임 종료
    PlayerTurn, //플레이어 턴
    Ping,       //연결 확인 요청
    Pong        //연결 확인 응답
}

//클라이언트와 서버 간 주고 받는 메시지의 구조를 정의하는 클래스
public class GameMessage
{
    public MessageType Type { get; set; } //메시지 타입
    public int X { get; set; }           //돌을 놓을 X좌표
    public int Y { get; set; }           //돌을 놓을 Y좌표
    public int PlayerID { get; set; }    //플레이어 식별자
    public string Data { get; set; }     //추가 데이터
}

class Program
{
    static void Main(string[] args)
    {
        //서버 인스턴스 생성
        GomokuServer server = new GomokuServer();
        //서버 시작
        server.Start();
    }
}


//오목 게임 서버의 핵심 기능을 구현하는 클래스
public class GomokuServer
{
    //TCP연결을 수신하는 리스너
    private TcpListener server;
    
    //연결된 클라이언트 목록
    private List<ClientHandler> clients = new List<ClientHandler> (); //연결된 클라이언트 목록
    
    //서버 포트 번호
    private readonly int port = 8888;
    
    //15*15 오목 게임판
    private int[,] board = new int[15, 15];
    
    //현재 연결된 총 플레이어 수
    private int totalPlayers = 0;

    //현재 턴(1 또는 2)
    public int currentTurn = 1;

    //서버 인스턴스를 초기화하는 생성자
    public GomokuServer()
    {
        server = new TcpListener(IPAddress.Any, port); //모든 IP에서 접속 가능한 서버 생성

    }

    //서버 시작하고 클라이언트 연결을 처리하는 메서드
    public void Start()
    {
       server.Start ();

        Console.WriteLine($"서버가 포트 {port}에서 시작되었습니다.");

        while (true)
        {
            TcpClient client = server.AcceptTcpClient(); //클라이언트 연결 대기
            if(totalPlayers < 2) // 최대 2명까지만 입장 가능
            {
                totalPlayers++;
                int playerID = totalPlayers;
                Console.WriteLine($"새로운 클라이언트가 연결되었습니다. (Player {playerID})");

                //클라이언트 핸들러 생성 및 시작
                ClientHandler handler = new ClientHandler(client, this, playerID);
                clients.Add(handler);

                Thread clientThread = new Thread(handler.HandleClient);
                clientThread.Start();

                //입장 메시지 전송
                var joinmessage = new GameMessage
                {
                    Type = MessageType.Join,
                    PlayerID = playerID,
                    Data = $"당신은 플레이어 {playerID}입니다."
                };
                Console.WriteLine($"[서버] 플레이어 {playerID} join 메시지 전송");
                handler.SendMessage(JsonSerializer.Serialize(joinmessage));

                //두번쨰 플레이어 입장 시 게임 시작
                if(playerID == 2)
                {
                    var turnMessage = new GameMessage()
                    { 
                        Type = MessageType.PlayerTurn,
                        PlayerID = currentTurn,
                        Data = $"플레이어 {currentTurn}의 차례입니다."
                    };
                    BroadcastMessage(JsonSerializer.Serialize(turnMessage));
                }
            }
            else
            {
                client.Close(); //게임이 시작된 후 추가 연결 거부
                Console.WriteLine("게임이 이미 시작되어 새로운 연결이 거부되었습니다.");
            }
        }
    }

    //모든 클라이언트에게 메시지를 브로드캐스트하는 메서드
    public void BroadcastMessage(string message, ClientHandler excludeClient = null)
    {
        foreach (var client in clients)
        {
            if (client != excludeClient && client.IsConnected)
            {
                client.SendMessage(message);
            }
        }
    }

    //게임판에 돌을 놓는 메서드
    public bool PlaceStone(int x, int y, int player)
    {
        //좌표가 유효하고 빈 칸인 경우에만 돌을 놓을수 있음
        if(x < 0 || x>= 15 || y < 0 || y >= 15 || board[x,y] != 0)
        {
            return false;
        }

        board[x, y] = player;

        return true;
    }

    //승리 조건을 체크하는 메서드
    public bool CheckWin(int x, int y, int player)
    {
        //8방향 체크를 4방향으로 최적화 (양방향을 한번에 체크)
        int[] dx = { 1, 0, 1, 1 }; //가로, 세로, 대각선 우상, 대각선 우하
        int[] dy = { 0, 1, 1, -1 };

        for(int dir = 0; dir < 4; dir++)
        {
            int count = 1; //현재 위치의 돌을 포함하여 시작

            //정방향 체크
            for(int i = 1; i < 5; i++)
            {
                int nx = x + dx[dir] * i;
                int ny = y + dy[dir] * i;

                if(nx < 0 || nx >= 15 || ny < 0 || ny >= 15 || board[nx, ny] != player)
                    break;
                count++;
            }

            //역방향 체크
            for (int i = 1; i < 5; i++)
            {
                int nx = x - dx[dir] * i;
                int ny = y - dy[dir] * i;

                if (nx < 0 || nx >= 15 || ny < 0 || ny >= 15 || board[nx, ny] != player)
                    break;
                count++;
            }
            if(count >= 5) return true; // 5개 이상 연속되면 승리
        }
        return false;
    }
}

//개별 클라이언트 연결을 처리하는 클래스
public class ClientHandler
{ 
    private TcpClient client; //클라이언트 연결
    private NetworkStream stream; //데이터 스트림
    private GomokuServer server; //서버 인스턴스 참조

    public bool IsConnected { get; private set; } //연결 상태
    private int playerID; //플레이어 식별자

    //클라이언트 핸들러 초기화
    public ClientHandler(TcpClient client, GomokuServer server, int playerID)
    {
        this.client = client;
        this.server = server;
        this.playerID = playerID;
        stream = client.GetStream();
        IsConnected = true;
    }

    //클라이언트 메시지 처리하는 메서드
    public void HandleClient()
    {
        byte[] buffer = new byte[1024];
        while(IsConnected)
        {
            try
            {
                //클라이언트로부터 메시지 수신
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                if (bytesRead == 0) 
                {
                    IsConnected = false;
                    break;
                }
                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                ProcessMessage(message);
            }
            catch
            {
                IsConnected=false;
                break;
            }
        }
        client.Close();
        Console.WriteLine($"Player {playerID} 연결이 종료되었습니다.");
    }

    //클라이언트 메시지를 전송하는 메서드
    public void SendMessage(string message)
    {
        try
        {
            byte[] buffer = Encoding.UTF8.GetBytes(message);
            stream.Write(buffer, 0, buffer.Length);
        }
        catch 
        {
            IsConnected = false;    
        }
    }

    //수신한 메시지를 처리하는 메서드
   private void ProcessMessage(string message)
    {
        try
        {
            var gameMessage = JsonSerializer.Deserialize<GameMessage>(message);
            Console.WriteLine($"받은 메시지 - 타입: {gameMessage.Type},위치: ({gameMessage.X},{gameMessage.Y}), 플레이어: {gameMessage.PlayerID}");
            
            switch(gameMessage.Type)
            {
                case MessageType.Place: //돌 배치 요청 처리
                    Console.WriteLine($"턴 체크 - 현재 턴: {server.currentTurn}, 요청 플레이어: {gameMessage.PlayerID}");
                    //자신의 턴이 아닌 경우 무시
                    if(gameMessage.PlayerID != server.currentTurn)
                    {
                        Console.WriteLine($"턴 위반 : 현재 턴은 플레이어 {server.currentTurn}입니다");
                        return;
                    }
                    Console.WriteLine($"플레이어 {gameMessage.PlayerID}가 ({gameMessage.X},{gameMessage.Y})에 돌을 놓았습니다.");

                    if(server.PlaceStone(gameMessage.X,gameMessage.Y, gameMessage.PlayerID))
                    {
                        //성공적으로 돌을 놓은 경우, 모든 클라이언트에 알림
                        server.BroadcastMessage(message);

                        //승리 조건 체크
                        if(server.CheckWin(gameMessage.X, gameMessage.Y, gameMessage.PlayerID))
                        {
                            var winMessage = new GameMessage 
                            {
                                Type = MessageType.GameOver,
                                PlayerID = gameMessage.PlayerID,
                                Data = $"Player{gameMessage.PlayerID} wins"
                            };
                            server.BroadcastMessage(JsonSerializer.Serialize(winMessage));
                            return;
                        }

                        //다음 턴으로 변경
                        server.currentTurn = (server.currentTurn==1) ? 2 : 1;
                        var turnMessage = new GameMessage
                        {
                            Type = MessageType.PlayerTurn,
                            PlayerID = server.currentTurn,
                            Data = $"플레이어 {server.currentTurn}의 차례입니다."
                        };
                        server.BroadcastMessage(JsonSerializer.Serialize(turnMessage));


                    }
                    break;

                case MessageType.Ping: //연결 상태 확인 요청 처리
                    var pongMessage = new GameMessage
                    {
                        Type = MessageType.Pong,
                        Data = "pong"
                    };
                   SendMessage(JsonSerializer.Serialize(pongMessage));
                   Console.WriteLine($"Player{playerID}에게 Pong 전송");
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"메시지 처리 오류: {ex.Message}");
        }
    }
}