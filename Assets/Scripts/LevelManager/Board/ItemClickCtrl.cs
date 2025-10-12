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
    private bool isProcessing = false;
    //private SemaphoreSlim clickLock = new SemaphoreSlim(1, 1);


    void Start()
    {
        if (LevelManager.Instance == null || LevelManager.Instance.BoardCtrl == null)
        {
            Debug.LogError("❌ LevelManager hoặc BoardCtrl chưa được khởi tạo!");
            return;
        }

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
            if (isProcessing) return;
           // if (!await clickLock.WaitAsync(0)) return; // đang bận thì bỏ qua

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out hit))
            {
                isProcessing = true;
                Debug.Log(hit.collider.gameObject.name);
                BoardCell boardCell = hit.transform.parent.GetComponent<BoardCell>();
                if(!boardCell.HasClick)
                {
                    isProcessing = false;
                    return;
                }
                if (boardCell == null) return;
                var (path, hasPath) = FindingPath.BFSFind(boardCell.Container);
                if (!hasPath)
                {
                    isProcessing = false;
                    return;
                }
                LevelManager.Instance.BoardCtrl.checkAndSavePosAction.Invoke(boardCell);
                //setActive cac NeighBor
                boardCell.SetActiveNeighBor();

                boardCell.Container.IsContaining = false;
                boardCell.Container = null;

                await MoveLeaveMatrix(path, boardCell);

                await LevelManager.Instance.BoardCtrl.MoveToCellPlay.Invoke();
                isProcessing = false;
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

        BoardCell boardCell = hit.collider.transform.parent?.GetComponent<BoardCell>();
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
