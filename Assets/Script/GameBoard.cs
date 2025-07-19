using UnityEngine;  // Unity ���� ��� ����� ���� ���ӽ����̽�

// ���� ������ ���带 �����ϴ� ���� ������Ʈ
public class GameBoard : MonoBehaviour
{
    // Unity �ν����Ϳ��� ������ �����հ� ��������Ʈ��
    public GameObject cellPrefab;        // ���� ĭ�� ������ �� ����� ������
    public GameObject stonePrefab;       // ���� ������ �� ����� ������
    public Sprite blackStoneSprite;      // �浹 ��������Ʈ
    public Sprite whiteStoneSprite;      // �鵹 ��������Ʈ
    public Sprite cellSprite;            // ���� ĭ�� ��������Ʈ
    public int boardSize = 14;           // ������ ũ�� (14x14)

    // private ��� ������
    private GameObject[,] cells;         // ���� ĭ���� �����ϴ� 2���� �迭
    private GameObject[,] stones;        // ������ ������ �����ϴ� 2���� �迭
    private float cellSize;              // �� ĭ�� ũ��
    private GomokuClient client;         // ��Ʈ��ũ Ŭ���̾�Ʈ ����

    // ���� ���� �� �ʱ�ȭ
    void Start()
    {
        cellSize = 0.4f;  // ĭ ũ�� ����
        cells = new GameObject[boardSize, boardSize];   // ���� ĭ �迭 �ʱ�ȭ
        stones = new GameObject[boardSize, boardSize];  // �� �迭 �ʱ�ȭ

        // NetworkManager ã�Ƽ� Ŭ���̾�Ʈ ������Ʈ ��������
        client = GameObject.Find("NetworkManager").GetComponent<GomokuClient>();
        if (client == null)
        {
            Debug.LogError("NetworkManager�� ã�� �� �����ϴ�!");
        }
        CreateBoard();  // ���� ����
    }

    // ���� ���带 �����ϴ� �޼���
    void CreateBoard()
    {
        // ���� ĭ���� ���� �θ� ��ü ����
        GameObject cellParent = new GameObject("BoardCells");
        cellParent.transform.SetParent(transform);

        // ������ ���� ��ġ ��� (�߾� ������ ����)
        float startX = -(boardSize - 1) * cellSize / 2;
        float startY = -(boardSize - 1) * cellSize / 2;

        // ������ ��� ĭ�� ����
        for (int y = 0; y < boardSize; y++)
        {
            for (int x = 0; x < boardSize; x++)
            {
                // �� ĭ�� ��ġ ���
                Vector3 position = new Vector3(
                    startX + x * cellSize,
                    startY + y * cellSize,
                    0
                );

                // ĭ ������ ���� �� ����
                GameObject cell = Instantiate(cellPrefab, position, Quaternion.identity);
                cell.transform.SetParent(cellParent.transform);
                cell.name = $"Cell_{x}_{y}";

                // ĭ�� �ð��� ����
                SpriteRenderer renderer = cell.GetComponent<SpriteRenderer>();
                renderer.sprite = cellSprite;
                renderer.color = new Color(0.5f, 0.5f, 0.5f, 1f);  // ȸ������ ����

                // Ŭ�� ������ ���� �ݶ��̴� ����
                BoxCollider2D collider = cell.GetComponent<BoxCollider2D>();
                collider.size = new Vector2(cellSize * 0.8f, cellSize * 0.8f);

                // BoardCell ������Ʈ ����
                BoardCell boardCell = cell.GetComponent<BoardCell>();
                boardCell.x = x;
                boardCell.y = y;

                // �迭�� ����
                cells[x, y] = cell;
            }
        }
    }

    // ������ ��ġ�� ���� ���� �޼���
    public void PlaceStone(int x, int y, bool isBlack)
    {
        Debug.Log($"PlaceStone ȣ�� - X: {x}, Y: {y}, isBlack: {isBlack}");

        // �̹� ���� �ִ��� Ȯ��
        if (stones[x, y] != null)
        {
            Debug.Log("�̹� ���� �ִ� ��ġ�Դϴ�.");
            return;
        }

        // �� ���� �� ��ġ ����
        Vector3 stonePosition = cells[x, y].transform.position;
        GameObject stone = Instantiate(stonePrefab, stonePosition, Quaternion.identity);
        stone.transform.SetParent(transform);

        // ���� ũ�� ����
        float stoneSize = cellSize / 2.5f;
        stone.transform.localScale = new Vector3(stoneSize, stoneSize, 1);

        // ���� ���� ����
        SpriteRenderer renderer = stone.GetComponent<SpriteRenderer>();
        if (isBlack)
        {
            Debug.Log("�浹 ���� �õ�");
            renderer.sprite = blackStoneSprite;
        }
        else
        {
            Debug.Log("�鵹 ���� �õ�");
            renderer.sprite = whiteStoneSprite;
        }
        renderer.sortingOrder = 1;  // ���� ĭ ���� ���̵��� ���� ���� ����

        // �迭�� �� ����
        stones[x, y] = stone;
        Debug.Log($"�� ���� �Ϸ� - ��ġ: ({x}, {y}), isBlack: {isBlack}");
    }

    // ĭ�� Ŭ���Ǿ��� �� ȣ��Ǵ� �޼���
    public void OnCellClicked(int x, int y)
    {
        // �̹� ���� ������ ����
        if (stones[x, y] != null) return;

        // ��Ʈ��ũ Ŭ���̾�Ʈ�� ���� ������ �� ��ġ �޽��� ����
        if (client != null)
        {
            client.SendPlaceStoneMessage(x, y);
            Debug.Log($"Ŭ���� ��ġ: ({x}, {y})");
        }
    }
}