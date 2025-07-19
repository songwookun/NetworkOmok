using System;  // Action ��������Ʈ�� ����ϱ� ���� ���ӽ����̽�
using System.Collections.Generic;  // Queue �ڷᱸ���� ����ϱ� ���� ���ӽ����̽�
using UnityEngine;  // Unity ���� ����� ����ϱ� ���� ���ӽ����̽�

// �ٸ� �����忡�� Unity ���� ������� �۾��� ����ġ(����)�ϴ� Ŭ����
// Unity UI�� ���� ������Ʈ ������ ���� �����忡���� �����ϹǷ� �ʿ�
public class UnityMainThreadDispatcher : MonoBehaviour
{
    // ������ �۾����� �����ϴ� ť
    // static readonly�� �����Ͽ� �� �� �ʱ�ȭ �� ���� �Ұ����ϰ� ����
    private static readonly Queue<Action> executionQueue = new Queue<Action>();

    // �̱��� ������ ���� �ν��Ͻ� ����
    private static UnityMainThreadDispatcher instance;

    // �̱��� �ν��Ͻ��� �������ų� �����ϴ� �޼���
    public static UnityMainThreadDispatcher Instance()
    {
        if (instance == null)
        {
            // ����ó�� ���� ���ο� ���� ������Ʈ ����
            var dispatcherGameObject = new GameObject("UnityMainThreadDispatcher");
            // ���� ������Ʈ�� ����ó ������Ʈ �߰�
            instance = dispatcherGameObject.AddComponent<UnityMainThreadDispatcher>();
            // �� ��ȯ �ÿ��� �ı����� �ʵ��� ����
            DontDestroyOnLoad(dispatcherGameObject);
        }
        return instance;
    }

    // ������ �۾��� ť�� �߰��ϴ� �޼���
    public void Enqueue(Action action)
    {
        if (action == null) return;  // null �۾��� ����

        // ������ �������� ���� lock ���
        lock (executionQueue)
        {
            executionQueue.Enqueue(action);
        }
    }

    // Unity ������Ʈ �������� ť�� �ִ� �۾����� ����
    // �� �޼���� ���� �����忡�� �����
    private void Update()
    {
        // ������ �������� ���� lock ���
        lock (executionQueue)
        {
            // ť�� ����� ������ �۾� ����
            while (executionQueue.Count > 0)
            {
                var action = executionQueue.Dequeue();
                action?.Invoke();  // null üũ�� �Բ� �۾� ����
            }
        }
    }
}