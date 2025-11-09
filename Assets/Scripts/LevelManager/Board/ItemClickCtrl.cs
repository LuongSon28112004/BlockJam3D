using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class ItemClickCtrl : MonoBehaviour
{
    [SerializeField] private FindingPath findingPath;
    public FindingPath FindingPath { get => findingPath; set => findingPath = value; }

    private RaycastHit hit;
    public bool isStart = false;

    private void Start()
    {
        if (LevelManager.Instance == null || LevelManager.Instance.BoardCtrl == null)
        {
            Debug.LogError("LevelManager hoặc BoardCtrl chưa được khởi tạo!");
            return;
        }
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // CHẶN CLICK UI
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                // Debug.Log("Click UI -> bỏ qua");
                return;
            }
            if (LevelManager.Instance.boosterCtrl.IsBusy) return;
            StartCoroutine(OnClickItem());
        }
    }

    private IEnumerator OnClickItem()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out hit))
        {
            if (hit.collider == null)
            {
                yield break;
            }
            Debug.Log(hit.collider.gameObject.name);
            if (hit.transform.parent == null)
            {
                yield break;
            }
            BoardCell boardCell = hit.transform.parent.GetComponent<BoardCell>();
            if (boardCell == null)
            {
                yield break;
            }
            if (LevelManager.Instance.boosterCtrl.IsBusy)
            {
                yield break;
            }
            if (!boardCell.HasClick)
            {
                // Nếu không thể click -> phản hồi (hiệu ứng xoay)
                if (!boardCell.IsInCellPlay)
                {
                    Sequence seq = DOTween.Sequence();
                    seq.Append(boardCell.transform.DOLocalRotate(new Vector3(0, 90, 0), 0.1f))
                    .Append(boardCell.transform.DOLocalRotate(Vector3.zero, 0.1f));
                }

                yield break;
            }


            if (!boardCell.HasClick)
            {
                yield break;
            }


            if (!boardCell.IsBoosterAdd)
            {
                StartCoroutine(LeaderBoardClick(boardCell));
            }
            else
            {
                StartCoroutine(BoosterAddClick(boardCell));
            }
        }
    }

    public IEnumerator LeaderBoardClick(BoardCell boardCell)
    {
        //xoa quan khoi board
        //LevelManager.Instance.BoardCtrl.RemoveContainer(index);
        var (path, hasPath) = findingPath.BFSFind(boardCell.Container);
        if (!hasPath)
        {
            Debug.Log("No path found to the bottom row.");
            yield break;
        }
        if (!isStart)
        {
            isStart = true;
            CustomeEventSystem.Instance.ActiveBooster(new List<int> { 1, 1, 1, 1 });
        }

        int index = LevelManager.Instance.BoardCtrl.boardAlls.IndexOf(boardCell.transform.gameObject);
        LevelManager.Instance.BoardCtrl.boardAlls.Remove(boardCell.transform.gameObject);
        if (!boardCell.HasSpawn && index != -1)
        {
            LevelManager.Instance.BoardCtrl.boardAlls.Insert(index, boardCell.Container.gameObject);
        }
        //check and save pos
        if (LevelManager.Instance.cellPlayCtrl.BoardCells.Count == 7) yield break;
        //reset container
        Container container = boardCell.Container;
        boardCell.Container.IsContaining = false;
        //boardCell.Container = null;
        AudioManager.Instance.PlayOneShot("BLJ_Game_Blockies_Click_01", 1f);
        LevelManager.Instance.cellPlayCtrl.CheckAndSaveBoardCell(boardCell);

        StartCoroutine(boardCell.SetActiveNeighBor());

        //start Run
        LevelManager.Instance.cellPlayCtrl.PosCell();
        StartCoroutine(LevelManager.Instance.BoardCtrl.SpawnBlockToGSPAction.Invoke(container, null));
        StartCoroutine(boardCell.BoardCellMovement.MovementPath(path, (check) =>
        {
            LevelManager.Instance.boosterCtrl.LastMove.Push((boardCell, container, path));
            Queue<KeyValuePair<BoardCell, Container>> temp = new Queue<KeyValuePair<BoardCell, Container>>();
            for (int i = 0; i < LevelManager.Instance.cellPlayCtrl.BoardCells.Count; i++)
            {
                if (LevelManager.Instance.cellPlayCtrl.BoardCells[i].TypeItem == boardCell.TypeItem && LevelManager.Instance.cellPlayCtrl.BoardCells[i] != boardCell)
                {
                    temp.Enqueue(new KeyValuePair<BoardCell, Container>(LevelManager.Instance.cellPlayCtrl.BoardCells[i], LevelManager.Instance.cellPlayCtrl.CellPlays[i]));
                }
            }
            if (temp.Count != 0 && temp.Count == 2) LevelManager.Instance.boosterCtrl.UndoQueue.Push(temp);
            if (check && LevelManager.Instance.cellPlayCtrl.HasMatch3(boardCell.TypeItem))
            {
                CustomeEventSystem.Instance.CheckMatch_3(boardCell.TypeItem);
                LevelManager.Instance.boosterCtrl.IsMatch3s.Push(true);
            }
            else
            {
                LevelManager.Instance.boosterCtrl.IsMatch3s.Push(false);
                StartCoroutine(LevelManager.Instance.cellPlayCtrl.checkLose());
            }
        }));
    }

    public IEnumerator BoosterAddClick(BoardCell boardCell)
    {
        if (!isStart)
        {
            isStart = true;
            CustomeEventSystem.Instance.ActiveBooster(new List<int> { 1, 1, 1, 1 });
        }
        AudioManager.Instance.PlayOneShot("BLJ_Game_Blockies_Click_01", 1f);
        // lấy data của BoardCell cũ
        Vector3 PosBoardCel = boardCell.Pos;
        LevelManager.Instance.cellPlayCtrl.CheckAndSaveBoardCell(boardCell);
        Vector3 pos = LevelManager.Instance.cellPlayCtrl.PosCell();
        boardCell.transform.localRotation = Quaternion.Euler(0, 180, 0);
        // xoa data khỏi hàng chờ add
        LevelManager.Instance.boosterCtrl.BoosterAddPos.RemoveBoardCell(boardCell);
        yield return boardCell.BoardCellMovement.MovementToPos(pos);
        // xét boardcell này bằng true để phục vị cho việc match_3
        boardCell.IsInCellPlay = true;
        boardCell.transform.localRotation = Quaternion.Euler(0, 0, 0);
        LevelManager.Instance.boosterCtrl.LastMove.Push((boardCell, boardCell.Container, new List<Vector3> { PosBoardCel }));
        Queue<KeyValuePair<BoardCell, Container>> temp = new Queue<KeyValuePair<BoardCell, Container>>();
        for (int i = 0; i < LevelManager.Instance.cellPlayCtrl.BoardCells.Count; i++)
        {
            if (LevelManager.Instance.cellPlayCtrl.BoardCells[i].TypeItem == boardCell.TypeItem && LevelManager.Instance.cellPlayCtrl.BoardCells[i] != boardCell)
            {
                temp.Enqueue(new KeyValuePair<BoardCell, Container>(LevelManager.Instance.cellPlayCtrl.BoardCells[i], LevelManager.Instance.cellPlayCtrl.CellPlays[i]));
            }
        }
        if (temp.Count != 0 && temp.Count == 2) LevelManager.Instance.boosterCtrl.UndoQueue.Push(temp);
        if (LevelManager.Instance.cellPlayCtrl.HasMatch3(boardCell.TypeItem))
        {
            CustomeEventSystem.Instance.CheckMatch_3(boardCell.TypeItem);
            LevelManager.Instance.boosterCtrl.IsMatch3s.Push(true);
        }
        else
        {
            LevelManager.Instance.boosterCtrl.IsMatch3s.Push(false);
            StartCoroutine(LevelManager.Instance.cellPlayCtrl.checkLose());
        }
    }
}