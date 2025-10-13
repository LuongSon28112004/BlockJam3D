using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class ItemClickCtrl : MonoBehaviour
{
    [SerializeField] private FindingPath findingPath;
    public FindingPath FindingPath { get => findingPath; set => findingPath = value; }

    private RaycastHit hit;
    private Queue<BoardCell> queBoardCellsClick;


    void Start()
    {
        if (LevelManager.Instance == null || LevelManager.Instance.BoardCtrl == null)
        {
            Debug.LogError("❌ LevelManager hoặc BoardCtrl chưa được khởi tạo!");
            return;
        }
        queBoardCellsClick = new Queue<BoardCell>();
        LevelManager.Instance.BoardCtrl.MoveToPosAction += MoveToPos;
    }

    void OnDisable()
    {
        if (LevelManager.Instance != null && LevelManager.Instance.BoardCtrl != null)
        {
            LevelManager.Instance.BoardCtrl.MoveToPosAction -= MoveToPos;
        }
    }

    void Update()
    {
        
        _ = OnClickItem();
    }


   private async Task OnClickItem()
    {
        if (Input.GetMouseButtonDown(0))
        {

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out hit))
            {
                Debug.Log(hit.collider.gameObject.name);
                BoardCell boardCell = hit.transform.parent.GetComponent<BoardCell>();
                if (boardCell == null) return;
                if(!boardCell.HasClick) return;
                queBoardCellsClick.Enqueue(boardCell);
                var (path, hasPath) = await findingPath.BFSFind(boardCell.Container);
                if (!hasPath) return;
                boardCell.BoardCellAnimation.SetRunning();
                await LevelManager.Instance.BoardCtrl.checkAndSavePosAction.Invoke(boardCell);
                //setActive cac NeighBor
                boardCell.SetActiveNeighBor();

                boardCell.Container.IsContaining = false;
                boardCell.Container = null;

                await MoveLeaveMatrix(path, boardCell);
                await LevelManager.Instance.BoardCtrl.MoveToCellPlay.Invoke(boardCell);
            }
        }
}

    private async Task MoveLeaveMatrix(List<Vector3> path, BoardCell boardCell)
    {
        if (boardCell == null)
        {
            Debug.LogWarning("Không có BoardCell hợp lệ để di chuyển.");
            return;
        }

        BoardCellMovement movement = boardCell.GetComponentInChildren<BoardCellMovement>();
        if (movement == null)
        {
            Debug.LogWarning("Không tìm thấy BoardCellMovement.");
            return;
        }

        await movement.MovementMatrix(path);
    }

    private async Task MoveToPos(Vector3 pos)
    {
        if (hit.collider == null)
        {
            Debug.LogWarning("Không có collider hợp lệ để di chuyển.");
            return;
        }

        BoardCell boardCell = queBoardCellsClick.Dequeue();
        Debug.Log("okok move" + boardCell.TypeItem);
        if (boardCell == null)
        {
            Debug.LogWarning("Không tìm thấy BoardCell.");
            return;
        }

        BoardCellMovement movement = boardCell.GetComponentInChildren<BoardCellMovement>();
        if (movement == null)
        {
            Debug.LogWarning("Không tìm thấy BoardCellMovement.");
            return;
        }

        await movement.MovementToCellPlay(pos);
    }
}
