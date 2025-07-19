using UnityEngine;  // Unity ������ �⺻ ��ɵ��� ����ϱ� ���� ���ӽ����̽� ����

// ���� ������ �� ĭ(��)�� ǥ���ϴ� ������Ʈ Ŭ����
// MonoBehaviour�� ��ӹ޾� Unity�� ������Ʈ�� ����
public class BoardCell : MonoBehaviour
{
    // ���忡���� x, y ��ǥ�� �����ϴ� ����
    // public���� ����Ǿ� Unity �ν����Ϳ��� ���� ���� ������ �� ����
    public int x, y;

    // �θ� ��ü�� GameBoard�� ������ ������ ����
    // private���� ����Ǿ� �ܺο��� ���� ���� �Ұ�
    private GameBoard gameBoard;

    // ������Ʈ�� Ȱ��ȭ�� �� �ڵ����� ȣ��Ǵ� Unity ����������Ŭ �޼���
    void Start()
    {
        // �θ� ��ü���� GameBoard ������Ʈ�� ã�� ����
        // GetComponentInParent�� ���� ��ü�� �θ� ���� �������� ������ Ÿ���� ������Ʈ�� ã�� �޼���
        gameBoard = GetComponentInParent<GameBoard>();
    }

    // ���콺 Ŭ���� �����ϴ� Unity�� ���� �̺�Ʈ �޼���
    // �� ��ü�� Collider�� Ŭ���Ǿ��� �� �ڵ����� ȣ���
    void OnMouseDown()
    {
        // Ŭ���� �����Ǹ� GameBoard�� OnCellClicked �޼��带 ȣ���Ͽ�
        // ���� ���� x, y ��ǥ�� ����
        gameBoard.OnCellClicked(x, y);
    }
}