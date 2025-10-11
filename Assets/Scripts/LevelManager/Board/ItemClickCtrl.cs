using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class ItemClickCtrl : MonoBehaviour
{
    [SerializeField] private FindingPath findingPath;
    //[SerializeField] Dictionary<BoardCell, int> dictIndexDirection;
    public FindingPath FindingPath { get => findingPath; set => findingPath = value; }

    private RaycastHit hit;

    private Queue<int> indexClick = new Queue<int>();

    void Start()
    {
        if (LevelManager.Instance == null || LevelManager.Instance.BoardCtrl == null)
        {
            Debug.LogError("❌ LevelManager hoặc BoardCtrl chưa được khởi tạo!");
            return;
        }

        LevelManager.Instance.BoardCtrl.MoveToCellPlayAction += MoveToCellPlay;
    }

    void Update()
    {
        _ = MouseInput(); // chạy async không cần chờ (fire and forget)
    }

    void OnDisable()
    {
        if (LevelManager.Instance != null && LevelManager.Instance.BoardCtrl != null)
        {
           LevelManager.Instance.BoardCtrl.MoveToCellPlayAction -= MoveToCellPlay;
        }
    }

    private async Task MouseInput()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out hit)) return;

        BoardCell boardCell = hit.collider.transform.parent?.GetComponent<BoardCell>();
        if (boardCell == null)
        {
            Debug.LogWarning("Không tìm thấy BoardCell cha.");
            return;
        }

        Container clicked = boardCell.Container;
        if (clicked == null)
        {
            Debug.LogWarning("Ô được click không có Container.");
            return;
        }

        List<Vector3> path = findingPath?.BFSFind(clicked);
        if (path == null || path.Count == 0)
        {
            Debug.Log("Không tìm được đường đi đến hàng cuối.");
            return;
        }

        Debug.Log($"Tìm thấy đường đi với {path.Count} bước.");

        boardCell.Container = null;
        clicked.IsContaining = false;

        // Reserve a slot for this boardCell and enqueue the returned index
        int reservedIndex = LevelManager.Instance.BoardCtrl.TryReserverSlotAction(boardCell);
        indexClick.Enqueue(reservedIndex);
        // Chờ di chuyển hoàn tất
        await MoveLeaveMatrix(path);
        LevelManager.Instance.BoardCtrl.ExcuteMoveAction.Invoke(boardCell,indexClick.Dequeue());
    }

    private async Task MoveLeaveMatrix(List<Vector3> path)
    {
        if (hit.collider == null)
        {
            Debug.LogWarning("Không có collider hợp lệ để di chuyển.");
            return;
        }

        BoardCellMovement movement = hit.collider.transform.parent?.GetComponentInChildren<BoardCellMovement>();
        if (movement == null)
        {
            Debug.LogWarning("Không tìm thấy BoardCellMovement.");
            return;
        }

        await movement.MovementMatrix(path);
    }

    private BoardCell MoveToCellPlay(Vector3 pos)
    {
        if (hit.collider == null)
        {
            Debug.LogWarning("Không có collider hợp lệ để di chuyển.");
            return null;
        }

        BoardCellMovement movement = hit.collider.transform.parent?.GetComponentInChildren<BoardCellMovement>();
        if (movement == null)
        {
            Debug.LogWarning("Không tìm thấy BoardCellMovement.");
            return null;
        }

        BoardCell boardCell = hit.collider.transform.parent?.GetComponent<BoardCell>();
        movement.MovementToCellPlay(pos);
        return boardCell;
    }
}
