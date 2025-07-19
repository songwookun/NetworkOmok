using UnityEngine;
using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using UnityEngine.UI;

// ���� ������ Ŭ���̾�Ʈ �� ��Ʈ��ũ �� ���� ������ �����ϴ� Ŭ����
public class GomokuClient : MonoBehaviour
{
    // ��Ʈ��ũ ���� ������
    private TcpClient client;              // TCP Ŭ���̾�Ʈ �ν��Ͻ�
    private NetworkStream stream;          // ��Ʈ��ũ ��Ʈ��
    private string serverIP = "127.0.0.1"; // ���� IP (����ȣ��Ʈ)
    private int serverPort = 8888;         // ���� ��Ʈ
    private bool isConnected = false;      // ���� ���� ����
    private int playerID = -1;             // �÷��̾� �ĺ��� (-1�� ���Ҵ�)

    // Unity ������Ʈ ����
    public GameObject stonePrefab;         // �� ������
    public Transform boardTransform;       // ���� ���� Transform
    public Text turnText;                  // �� ǥ�� UI Text
    public GameBoard gameBoard;            // ���� ���� ������Ʈ
    public GameObject gameOverPanel;       // ���� ���� �г�
    public Text gameOverText;              // ���� ���� �޽���

    // ���� ���� ����
    private int currentTurn = 1;           // ���� �� (1: �浹, 2: �鵹)

    // ���� ���� �� ȣ��Ǵ� �޼���
    private void Start()
    {
        ConnectToServer();                 // ���� ���� �õ�
        InvokeRepeating("SendPing", 2f, 5f);  // 5�ʸ��� ���� ���� Ȯ��
    }

    // ���� ������ �õ��ϴ� �޼���
    private void ConnectToServer()
    {
        try
        {
            client = new TcpClient(serverIP, serverPort);
            stream = client.GetStream();
            isConnected = true;
            Debug.Log("���� ���� ����!");

            // �޽��� ������ ���� ���� ������ ����
            Thread receiveThread = new Thread(ReceiveMessages);
            receiveThread.IsBackground = true;
            receiveThread.Start();
        }
        catch (Exception e)
        {
            Debug.LogError($"���� ���� ����: {e.Message}");
            isConnected = false;
        }
    }

    // �����κ��� �޽����� �����ϴ� �޼��� (���� �����忡�� ����)
    private void ReceiveMessages()
    {
        byte[] buffer = new byte[1024];
        StringBuilder messageBuffer = new StringBuilder();

        while (isConnected)
        {
            try
            {
                // ������ ����
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                if (bytesRead < 1)
                {
                    isConnected = false;
                    break;
                }

                // ���ŵ� �����͸� ���ڿ��� ��ȯ
                string receivedData = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                messageBuffer.Append(receivedData);

                // JSON �޽��� �Ľ� �� ó��
                while (messageBuffer.ToString().Contains("}"))
                {
                    string completeMessage = messageBuffer.ToString();
                    int endIndex = completeMessage.IndexOf("}") + 1;
                    string jsonMessage = completeMessage.Substring(0, endIndex);
                    messageBuffer.Remove(0, endIndex);

                    try
                    {
                        // JSON�� GameMessage ��ü�� ��ȯ
                        GameMessage receivedMessage = JsonUtility.FromJson<GameMessage>(jsonMessage);

                        // ���� �����忡�� �޽��� ó��
                        UnityMainThreadDispatcher.Instance().Enqueue(() =>
                        {
                            ProcessMessage(receivedMessage);
                        });
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"JSON ó�� ����: {e.Message}");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"�޽��� ���� ����: {e.Message}");
                isConnected = false;
                break;
            }
        }
    }

    // ���ŵ� �޽����� ó���ϴ� �޼���
    private void ProcessMessage(GameMessage message)
    {
        Debug.Log($"[�޽��� ����] Type: {message.Type}, PlayerID: {message.PlayerID}");

        switch (message.Type)
        {
            case MessageType.Join:         // ���� �޽��� ó��
                playerID = message.PlayerID;
                Debug.Log($"[Join] �÷��̾� ID�� {playerID}�� �Ҵ��");
                break;

            case MessageType.Place:        // �� ��ġ �޽��� ó��
                Debug.Log($"[Place] �÷��̾� {message.PlayerID}�� ({message.X}, {message.Y})�� ���� ���� - �� ID: {playerID}");
                if (gameBoard != null)
                {
                    bool isBlack = (message.PlayerID == 1);
                    Debug.Log($"[Place] �� ���� �õ� - isBlack: {isBlack}, PlayerID: {message.PlayerID}");
                    gameBoard.PlaceStone(message.X, message.Y, isBlack);
                }
                break;

            case MessageType.PlayerTurn:   // �� ���� �޽��� ó��
                currentTurn = message.PlayerID;
                Debug.Log($"[Turn] �� ����� - ���� ��: {currentTurn}, �� ID: {playerID}");
                UpdateTurnUI();
                break;

            case MessageType.GameOver:     // ���� ���� �޽��� ó��
                HandleGameOver(message);
                break;

            case MessageType.Pong:         // Pong ���� ó��
                Debug.Log($"[Pong] ���� ���� ���� - �� ID: {playerID}");
                break;
        }
    }

    // ���� ���� ó�� �޼���
    private void HandleGameOver(GameMessage message)
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            if (gameOverText != null)
            {
                // ���� ����� ���� UI ������Ʈ
                if (message.PlayerID == playerID)
                {
                    gameOverText.text = "�¸��߽��ϴ�!";
                    gameOverText.color = Color.green;
                }
                else
                {
                    gameOverText.text = "�й��߽��ϴ�.";
                    gameOverText.color = Color.red;
                }
            }
        }

        // �� ǥ�� UI ������Ʈ
        if (turnText != null)
        {
            turnText.text = message.PlayerID == playerID ? "�¸�!" : "�й�!";
            turnText.color = message.PlayerID == playerID ? Color.green : Color.red;
        }
    }

    // �� UI ������Ʈ �޼���
    private void UpdateTurnUI()
    {
        if (turnText != null)
        {
            if (currentTurn == playerID)
            {
                turnText.text = "����� �����Դϴ�";
                turnText.color = Color.green;
            }
            else
            {
                turnText.text = "������ �����Դϴ�";
                turnText.color = Color.red;
            }
        }
    }

    // ������ �޽����� �����ϴ� �޼���
    public void SendMessage(GameMessage message)
    {
        if (!isConnected) return;

        try
        {
            string jsonMessage = JsonUtility.ToJson(message);
            byte[] buffer = Encoding.UTF8.GetBytes(jsonMessage);
            stream.Write(buffer, 0, buffer.Length);
            Debug.Log($"�޽��� ���۵�: {message.Type}");
        }
        catch (Exception e)
        {
            Debug.LogError($"�޽��� ���� ����: {e.Message}");
            isConnected = false;
        }
    }

    // ���� ���� Ȯ���� ���� Ping ����
    void SendPing()
    {
        if (!isConnected) return;

        GameMessage pingMessage = new GameMessage
        {
            Type = MessageType.Ping,
            Data = "ping"
        };
        SendMessage(pingMessage);
        Debug.Log("Ping ����");
    }

    // �� ��ġ ��û�� ������ �����ϴ� �޼���
    public void SendPlaceStoneMessage(int x, int y)
    {
        if (playerID == -1) return;        // ID ���Ҵ� �� ����
        if (currentTurn != playerID)       // �ڽ��� ���� �ƴ� ��� ����
        {
            Debug.Log("����� ���ʰ� �ƴմϴ�!");
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

    // ������Ʈ�� �ı��� �� ȣ��Ǵ� �޼���
    private void OnDestroy()
    {
        isConnected = false;
        if (client != null)
        {
            client.Close();
        }
    }

    // ���� ���¸� ��ȯ�ϴ� public �޼���
    public bool IsConnected()
    {
        return isConnected;
    }
}