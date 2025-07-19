using System;  // Action 델리게이트를 사용하기 위한 네임스페이스
using System.Collections.Generic;  // Queue 자료구조를 사용하기 위한 네임스페이스
using UnityEngine;  // Unity 엔진 기능을 사용하기 위한 네임스페이스

// 다른 스레드에서 Unity 메인 스레드로 작업을 디스패치(전달)하는 클래스
// Unity UI나 게임 오브젝트 조작은 메인 스레드에서만 가능하므로 필요
public class UnityMainThreadDispatcher : MonoBehaviour
{
    // 실행할 작업들을 저장하는 큐
    // static readonly로 선언하여 한 번 초기화 후 변경 불가능하게 설정
    private static readonly Queue<Action> executionQueue = new Queue<Action>();

    // 싱글톤 패턴을 위한 인스턴스 변수
    private static UnityMainThreadDispatcher instance;

    // 싱글톤 인스턴스를 가져오거나 생성하는 메서드
    public static UnityMainThreadDispatcher Instance()
    {
        if (instance == null)
        {
            // 디스패처를 담을 새로운 게임 오브젝트 생성
            var dispatcherGameObject = new GameObject("UnityMainThreadDispatcher");
            // 게임 오브젝트에 디스패처 컴포넌트 추가
            instance = dispatcherGameObject.AddComponent<UnityMainThreadDispatcher>();
            // 씬 전환 시에도 파괴되지 않도록 설정
            DontDestroyOnLoad(dispatcherGameObject);
        }
        return instance;
    }

    // 실행할 작업을 큐에 추가하는 메서드
    public void Enqueue(Action action)
    {
        if (action == null) return;  // null 작업은 무시

        // 스레드 안전성을 위해 lock 사용
        lock (executionQueue)
        {
            executionQueue.Enqueue(action);
        }
    }

    // Unity 업데이트 루프에서 큐에 있는 작업들을 실행
    // 이 메서드는 메인 스레드에서 실행됨
    private void Update()
    {
        // 스레드 안전성을 위해 lock 사용
        lock (executionQueue)
        {
            // 큐가 비워질 때까지 작업 실행
            while (executionQueue.Count > 0)
            {
                var action = executionQueue.Dequeue();
                action?.Invoke();  // null 체크와 함께 작업 실행
            }
        }
    }
}