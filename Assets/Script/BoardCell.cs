using UnityEngine;  // Unity 엔진의 기본 기능들을 사용하기 위한 네임스페이스 선언

// 오목 게임의 각 칸(셀)을 표현하는 컴포넌트 클래스
// MonoBehaviour를 상속받아 Unity의 컴포넌트로 동작
public class BoardCell : MonoBehaviour
{
    // 보드에서의 x, y 좌표를 저장하는 변수
    // public으로 선언되어 Unity 인스펙터에서 직접 값을 설정할 수 있음
    public int x, y;

    // 부모 객체인 GameBoard의 참조를 저장할 변수
    // private으로 선언되어 외부에서 직접 접근 불가
    private GameBoard gameBoard;

    // 컴포넌트가 활성화될 때 자동으로 호출되는 Unity 라이프사이클 메서드
    void Start()
    {
        // 부모 객체에서 GameBoard 컴포넌트를 찾아 저장
        // GetComponentInParent는 현재 객체의 부모 계층 구조에서 지정된 타입의 컴포넌트를 찾는 메서드
        gameBoard = GetComponentInParent<GameBoard>();
    }

    // 마우스 클릭을 감지하는 Unity의 내장 이벤트 메서드
    // 이 객체의 Collider가 클릭되었을 때 자동으로 호출됨
    void OnMouseDown()
    {
        // 클릭이 감지되면 GameBoard의 OnCellClicked 메서드를 호출하여
        // 현재 셀의 x, y 좌표를 전달
        gameBoard.OnCellClicked(x, y);
    }
}