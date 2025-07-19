using UnityEngine;  // Unity 엔진 기능 사용을 위한 네임스페이스

// 오목 게임의 보드를 관리하는 메인 컴포넌트
public class GameBoard : MonoBehaviour
{
    // Unity 인스펙터에서 설정할 프리팹과 스프라이트들
    public GameObject cellPrefab;        // 보드 칸을 생성할 때 사용할 프리팹
    public GameObject stonePrefab;       // 돌을 생성할 때 사용할 프리팹
    public Sprite blackStoneSprite;      // 흑돌 스프라이트
    public Sprite whiteStoneSprite;      // 백돌 스프라이트
    public Sprite cellSprite;            // 보드 칸의 스프라이트
    public int boardSize = 14;           // 보드의 크기 (14x14)

    // private 멤버 변수들
    private GameObject[,] cells;         // 보드 칸들을 저장하는 2차원 배열
    private GameObject[,] stones;        // 놓여진 돌들을 저장하는 2차원 배열
    private float cellSize;              // 각 칸의 크기
    private GomokuClient client;         // 네트워크 클라이언트 참조

    // 게임 시작 시 초기화
    void Start()
    {
        cellSize = 0.4f;  // 칸 크기 설정
        cells = new GameObject[boardSize, boardSize];   // 보드 칸 배열 초기화
        stones = new GameObject[boardSize, boardSize];  // 돌 배열 초기화

        // NetworkManager 찾아서 클라이언트 컴포넌트 가져오기
        client = GameObject.Find("NetworkManager").GetComponent<GomokuClient>();
        if (client == null)
        {
            Debug.LogError("NetworkManager를 찾을 수 없습니다!");
        }
        CreateBoard();  // 보드 생성
    }

    // 오목 보드를 생성하는 메서드
    void CreateBoard()
    {
        // 보드 칸들을 담을 부모 객체 생성
        GameObject cellParent = new GameObject("BoardCells");
        cellParent.transform.SetParent(transform);

        // 보드의 시작 위치 계산 (중앙 정렬을 위해)
        float startX = -(boardSize - 1) * cellSize / 2;
        float startY = -(boardSize - 1) * cellSize / 2;

        // 보드의 모든 칸을 생성
        for (int y = 0; y < boardSize; y++)
        {
            for (int x = 0; x < boardSize; x++)
            {
                // 각 칸의 위치 계산
                Vector3 position = new Vector3(
                    startX + x * cellSize,
                    startY + y * cellSize,
                    0
                );

                // 칸 프리팹 생성 및 설정
                GameObject cell = Instantiate(cellPrefab, position, Quaternion.identity);
                cell.transform.SetParent(cellParent.transform);
                cell.name = $"Cell_{x}_{y}";

                // 칸의 시각적 설정
                SpriteRenderer renderer = cell.GetComponent<SpriteRenderer>();
                renderer.sprite = cellSprite;
                renderer.color = new Color(0.5f, 0.5f, 0.5f, 1f);  // 회색으로 설정

                // 클릭 감지를 위한 콜라이더 설정
                BoxCollider2D collider = cell.GetComponent<BoxCollider2D>();
                collider.size = new Vector2(cellSize * 0.8f, cellSize * 0.8f);

                // BoardCell 컴포넌트 설정
                BoardCell boardCell = cell.GetComponent<BoardCell>();
                boardCell.x = x;
                boardCell.y = y;

                // 배열에 저장
                cells[x, y] = cell;
            }
        }
    }

    // 지정된 위치에 돌을 놓는 메서드
    public void PlaceStone(int x, int y, bool isBlack)
    {
        Debug.Log($"PlaceStone 호출 - X: {x}, Y: {y}, isBlack: {isBlack}");

        // 이미 돌이 있는지 확인
        if (stones[x, y] != null)
        {
            Debug.Log("이미 돌이 있는 위치입니다.");
            return;
        }

        // 돌 생성 및 위치 설정
        Vector3 stonePosition = cells[x, y].transform.position;
        GameObject stone = Instantiate(stonePrefab, stonePosition, Quaternion.identity);
        stone.transform.SetParent(transform);

        // 돌의 크기 설정
        float stoneSize = cellSize / 2.5f;
        stone.transform.localScale = new Vector3(stoneSize, stoneSize, 1);

        // 돌의 색상 설정
        SpriteRenderer renderer = stone.GetComponent<SpriteRenderer>();
        if (isBlack)
        {
            Debug.Log("흑돌 생성 시도");
            renderer.sprite = blackStoneSprite;
        }
        else
        {
            Debug.Log("백돌 생성 시도");
            renderer.sprite = whiteStoneSprite;
        }
        renderer.sortingOrder = 1;  // 돌이 칸 위에 보이도록 정렬 순서 설정

        // 배열에 돌 저장
        stones[x, y] = stone;
        Debug.Log($"돌 생성 완료 - 위치: ({x}, {y}), isBlack: {isBlack}");
    }

    // 칸이 클릭되었을 때 호출되는 메서드
    public void OnCellClicked(int x, int y)
    {
        // 이미 돌이 있으면 무시
        if (stones[x, y] != null) return;

        // 네트워크 클라이언트를 통해 서버에 돌 배치 메시지 전송
        if (client != null)
        {
            client.SendPlaceStoneMessage(x, y);
            Debug.Log($"클릭한 위치: ({x}, {y})");
        }
    }
}