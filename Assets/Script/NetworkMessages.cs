using System;  // Serializable �Ӽ��� ����ϱ� ���� ���ӽ����̽�

// ���ӿ��� ���Ǵ� ��� �޽��� Ÿ���� �����ϴ� ������
public enum MessageType
{
    Join,       // �÷��̾ ���ӿ� ������ �� ���
    Place,      // ���� ��ġ�� �� ���
    GameState,  // ���� ���� ���¸� ������ �� ���
    GameOver,   // ������ ����Ǿ��� �� ���
    PlayerTurn, // ���� �÷��̾��� ���� �˸� �� ���
    Ping,       // ���� ���¸� Ȯ���ϱ� ���� ��û
    Pong        // Ping�� ���� ����
}

// Ŭ���̾�Ʈ�� ���� ���� �ְ�޴� �޽����� ������ �����ϴ� Ŭ����
[Serializable]  // JSON ����ȭ/������ȭ�� ���� �Ӽ�
public class GameMessage
{
    public MessageType Type;    // �޽����� ������ ��Ÿ���� ������ ��
    public int X;              // ���� ���� X ��ǥ
    public int Y;              // ���� ���� Y ��ǥ
    public int PlayerID;       // �÷��̾ �����ϴ� ID (1 �Ǵ� 2)
    public string Data;        // �߰����� �����͸� �����ϱ� ���� ���ڿ� �ʵ�
}