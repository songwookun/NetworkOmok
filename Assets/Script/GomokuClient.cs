using UnityEngine;
using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using UnityEngine.UI;

// 오목 게임의 클라이언트 측 네트워크 및 게임 로직을 관리하는 클래스
public class GomokuClient : MonoBehaviour
{
    // 네트워크 관련 변수들
    private TcpClient client;              // TCP 클라이언트 인스턴스
    private NetworkStream stream;          // 네트워크 스트림
    private string serverIP = "127.0.0.1"; // 서버 IP (로컬호스트)
    private int serverPort = 8888;         // 서버 포트
    private bool isConnected = false;      // 서버 연결 상태
    private int playerID = -1;             // 플레이어 식별자 (-1은 미할당)

    // Unity 컴포넌트 참조
    public GameObject stonePrefab;         // 돌 프리팹
    public Transform boardTransform;       // 게임 보드 Transform
    public Text turnText;                  // 턴 표시 UI Text
    public GameBoard gameBoard;            // 게임 보드 컴포넌트
    public GameObject gameOverPanel;       // 게임 종료 패널
    public Text gameOverText;              // 게임 종료 메시지

    // 게임 상태 변수
    private int currentTurn = 1;           // 현재 턴 (1: 흑돌, 2: 백돌)

    // 게임 시작 시 호출되는 메서드
    private void Start()
    {
        ConnectToServer();                 // 서버 연결 시도
        InvokeRepeating("SendPing", 2f, 5f);  // 5초마다 연결 상태 확인
    }

    // 서버 연결을 시도하는 메서드
    private void ConnectToServer()
    {
        try
        {
            client = new TcpClient(serverIP, serverPort);
            stream = client.GetStream();
            isConnected = true;
            Debug.Log("서버 연결 성공!");

            // 메시지 수신을 위한 별도 스레드 시작
            Thread receiveThread = new Thread(ReceiveMessages);
            receiveThread.IsBackground = true;
            receiveThread.Start();
        }
        catch (Exception e)
        {
            Debug.LogError($"서버 연결 실패: {e.Message}");
            isConnected = false;
        }
    }

    // 서버로부터 메시지를 수신하는 메서드 (별도 스레드에서 실행)
    private void ReceiveMessages()
    {
        byte[] buffer = new byte[1024];
        StringBuilder messageBuffer = new StringBuilder();

        while (isConnected)
        {
            try
            {
                // 데이터 수신
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                if (bytesRead < 1)
                {
                    isConnected = false;
                    break;
                }

                // 수신된 데이터를 문자열로 변환
                string receivedData = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                messageBuffer.Append(receivedData);

                // JSON 메시지 파싱 및 처리
                while (messageBuffer.ToString().Contains("}"))
                {
                    string completeMessage = messageBuffer.ToString();
                    int endIndex = completeMessage.IndexOf("}") + 1;
                    string jsonMessage = completeMessage.Substring(0, endIndex);
                    messageBuffer.Remove(0, endIndex);

                    try
                    {
                        // JSON을 GameMessage 객체로 변환
                        GameMessage receivedMessage = JsonUtility.FromJson<GameMessage>(jsonMessage);

                        // 메인 스레드에서 메시지 처리
                        UnityMainThreadDispatcher.Instance().Enqueue(() =>
                        {
                            ProcessMessage(receivedMessage);
                        });
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"JSON 처리 실패: {e.Message}");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"메시지 수신 에러: {e.Message}");
                isConnected = false;
                break;
            }
        }
    }

    // 수신된 메시지를 처리하는 메서드
    private void ProcessMessage(GameMessage message)
    {
        Debug.Log($"[메시지 수신] Type: {message.Type}, PlayerID: {message.PlayerID}");

        switch (message.Type)
        {
            case MessageType.Join:         // 입장 메시지 처리
                playerID = message.PlayerID;
                Debug.Log($"[Join] 플레이어 ID가 {playerID}로 할당됨");
                break;

            case MessageType.Place:        // 돌 배치 메시지 처리
                Debug.Log($"[Place] 플레이어 {message.PlayerID}가 ({message.X}, {message.Y})에 돌을 놓음 - 내 ID: {playerID}");
                if (gameBoard != null)
                {
                    bool isBlack = (message.PlayerID == 1);
                    Debug.Log($"[Place] 돌 생성 시도 - isBlack: {isBlack}, PlayerID: {message.PlayerID}");
                    gameBoard.PlaceStone(message.X, message.Y, isBlack);
                }
                break;

            case MessageType.PlayerTurn:   // 턴 변경 메시지 처리
                currentTurn = message.PlayerID;
                Debug.Log($"[Turn] 턴 변경됨 - 현재 턴: {currentTurn}, 내 ID: {playerID}");
                UpdateTurnUI();
                break;

            case MessageType.GameOver:     // 게임 종료 메시지 처리
                HandleGameOver(message);
                break;

            case MessageType.Pong:         // Pong 응답 처리
                Debug.Log($"[Pong] 서버 연결 정상 - 내 ID: {playerID}");
                break;
        }
    }

    // 게임 종료 처리 메서드
    private void HandleGameOver(GameMessage message)
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            if (gameOverText != null)
            {
                // 승패 결과에 따른 UI 업데이트
                if (message.PlayerID == playerID)
                {
                    gameOverText.text = "승리했습니다!";
                    gameOverText.color = Color.green;
                }
                else
                {
                    gameOverText.text = "패배했습니다.";
                    gameOverText.color = Color.red;
                }
            }
        }

        // 턴 표시 UI 업데이트
        if (turnText != null)
        {
            turnText.text = message.PlayerID == playerID ? "승리!" : "패배!";
            turnText.color = message.PlayerID == playerID ? Color.green : Color.red;
        }
    }

    // 턴 UI 업데이트 메서드
    private void UpdateTurnUI()
    {
        if (turnText != null)
        {
            if (currentTurn == playerID)
            {
                turnText.text = "당신의 차례입니다";
                turnText.color = Color.green;
            }
            else
            {
                turnText.text = "상대방의 차례입니다";
                turnText.color = Color.red;
            }
        }
    }

    // 서버로 메시지를 전송하는 메서드
    public void SendMessage(GameMessage message)
    {
        if (!isConnected) return;

        try
        {
            string jsonMessage = JsonUtility.ToJson(message);
            byte[] buffer = Encoding.UTF8.GetBytes(jsonMessage);
            stream.Write(buffer, 0, buffer.Length);
            Debug.Log($"메시지 전송됨: {message.Type}");
        }
        catch (Exception e)
        {
            Debug.LogError($"메시지 전송 실패: {e.Message}");
            isConnected = false;
        }
    }

    // 연결 상태 확인을 위한 Ping 전송
    void SendPing()
    {
        if (!isConnected) return;

        GameMessage pingMessage = new GameMessage
        {
            Type = MessageType.Ping,
            Data = "ping"
        };
        SendMessage(pingMessage);
        Debug.Log("Ping 전송");
    }

    // 돌 배치 요청을 서버로 전송하는 메서드
    public void SendPlaceStoneMessage(int x, int y)
    {
        if (playerID == -1) return;        // ID 미할당 시 무시
        if (currentTurn != playerID)       // 자신의 턴이 아닐 경우 무시
        {
            Debug.Log("당신의 차례가 아닙니다!");
            return;
        }

        GameMessage message = new GameMessage
        {
            Type = MessageType.Place,
            X = x,
            Y = y,
            PlayerID = playerID
        };
        SendMessage(message);
    }

    // 컴포넌트가 파괴될 때 호출되는 메서드
    private void OnDestroy()
    {
        isConnected = false;
        if (client != null)
        {
            client.Close();
        }
    }

    // 연결 상태를 반환하는 public 메서드
    public bool IsConnected()
    {
        return isConnected;
    }
}