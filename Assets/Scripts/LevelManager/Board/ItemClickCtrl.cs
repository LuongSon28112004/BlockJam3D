using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemClickCtrl : MonoBehaviour
{
    [SerializeField] private FindingPath findingPath;
    public FindingPath FindingPath { get => findingPath; set => findingPath = value; }

    private RaycastHit hit;
    private Queue<BoardCell> queBoardCellsClick;
    private Queue<BoardCell> queBoardCellsMoveToPos;

    [SerializeField] private bool isProcessingClick = false;

    private void Start()
    {
        if (LevelManager.Instance == null || LevelManager.Instance.BoardCtrl == null)
        {
            Debug.LogError("LevelManager hoặc BoardCtrl chưa được khởi tạo!");
            return;
        }

        queBoardCellsClick = new Queue<BoardCell>();
        queBoardCellsMoveToPos = new Queue<BoardCell>();

        // Đăng ký event coroutine
        LevelManager.Instance.BoardCtrl.MoveToPosAction += MoveToPos;
    }

    private void OnDisable()
    {
        if (LevelManager.Instance != null && LevelManager.Instance.BoardCtrl != null)
        {
            LevelManager.Instance.BoardCtrl.MoveToPosAction -= MoveToPos;
        }
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
            StartCoroutine(OnClickItem());
    }

    private IEnumerator OnClickItem()
    {
        if (isProcessingClick) yield break; // Nếu đang xử lý, bỏ qua lần click mới
        isProcessingClick = true; // Đánh dấu đang xử lý
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out hit))
        {
            if(hit.collider == null) 
            {
                isProcessingClick = false; // Kết thúc xử lý
                yield break; 
            }
            Debug.Log(hit.collider.gameObject.name);
            if(hit.transform.parent == null)
            {
                isProcessingClick = false; // Kết thúc xử lý
                yield break; 
            }
            BoardCell boardCell = hit.transform.parent.GetComponent<BoardCell>();
            if (boardCell == null || !boardCell.HasClick)
            {
                isProcessingClick = false; // Kết thúc xử lý
                yield break;
            }

            queBoardCellsClick.Enqueue(boardCell);
            queBoardCellsMoveToPos.Enqueue(boardCell);

            var (path, hasPath) = findingPath.BFSFind(boardCell.Container);
            if (!hasPath)
            {
                Debug.Log("No path found to the bottom row.");
                isProcessingClick = false; // Kết thúc xử lý
                yield break;
            }

            boardCell.BoardCellAnimation.SetRunning();

            // Gọi coroutine trong BoardCtrl
            if (LevelManager.Instance.BoardCtrl.checkAndSavePosAction != null)
                StartCoroutine(LevelManager.Instance.BoardCtrl.checkAndSavePosAction.Invoke(boardCell));

            // Active neighbor
            StartCoroutine(boardCell.SetActiveNeighBor());

            Container container = boardCell.Container;

            boardCell.Container.IsContaining = false;
            boardCell.Container = null;

            StartCoroutine(LevelManager.Instance.BoardCtrl.SpawnBlockToGSPAction.Invoke(container,null)); 

            // Rời khỏi matrix
            yield return StartCoroutine(MoveLeaveMatrix(path));

            // Di chuyển đến cell play
            if (LevelManager.Instance.BoardCtrl.MoveToCellPlay != null)
            yield return StartCoroutine(LevelManager.Instance.BoardCtrl.MoveToCellPlay.Invoke(container,path)); // yield return 
        }
        isProcessingClick = false; // Kết thúc xử lý
    }

    private IEnumerator MoveLeaveMatrix(List<Vector3> path)
    {
        if (queBoardCellsClick.Count == 0) yield break;

        BoardCell boardCell = queBoardCellsClick.Dequeue();
        if (boardCell == null) yield break;

        BoardCellMovement movement = boardCell.GetComponentInChildren<BoardCellMovement>();
        if (movement == null) yield break;

        yield return StartCoroutine(movement.MovementMatrix(path));
    }

    private IEnumerator MoveToPos(Vector3 pos)
    {
        if (hit.collider == null)
        {
            Debug.LogWarning("Không có collider hợp lệ để di chuyển.");
            yield break;
        }

        if (queBoardCellsMoveToPos.Count == 0)
        {
            Debug.LogWarning("Hàng đợi di chuyển trống.");
            yield break;
        }

        BoardCell boardCell = queBoardCellsMoveToPos.Dequeue();
        if (boardCell == null)
        {
            Debug.LogWarning("Không tìm thấy BoardCell.");
            yield break;
        }

        BoardCellMovement movement = boardCell.GetComponentInChildren<BoardCellMovement>();
        if (movement == null)
        {
            Debug.LogWarning("Không tìm thấy BoardCellMovement.");
            yield break;
        }

        yield return StartCoroutine(movement.MovementToCellPlay(pos));
    }
}
