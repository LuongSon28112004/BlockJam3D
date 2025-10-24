using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class ItemClickCtrl : MonoBehaviour
{
    [SerializeField] private FindingPath findingPath;
    public FindingPath FindingPath { get => findingPath; set => findingPath = value; }

    private RaycastHit hit;

    private void Start()
    {
        if (LevelManager.Instance == null || LevelManager.Instance.BoardCtrl == null)
        {
            Debug.LogError("LevelManager hoặc BoardCtrl chưa được khởi tạo!");
            return;
        }
        // Đăng ký event coroutine
    }

    private void OnDisable()
    {
        
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
             // ⚠️ CHẶN CLICK UI
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                // Debug.Log("Click UI -> bỏ qua");
                return;
            }
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
            if (boardCell == null || !boardCell.HasClick)
            {
                yield break;
            }

            if (!boardCell.IsBoosterAdd)
            {
                var (path, hasPath) = findingPath.BFSFind(boardCell.Container);
                if (!hasPath)
                {
                    Debug.Log("No path found to the bottom row.");
                    yield break;
                }


                //check and save pos
                if (LevelManager.Instance.cellPlayCtrl.BoardCells.Count == 7) yield break;
                //reset container
                Container container = boardCell.Container;
                boardCell.Container.IsContaining = false;
                boardCell.Container = null;
                LevelManager.Instance.cellPlayCtrl.CheckAndSaveBoardCell(boardCell);

                StartCoroutine(boardCell.SetActiveNeighBor());

                //spawn block

                //start Run
                path.Add(LevelManager.Instance.cellPlayCtrl.PosCell());
                StartCoroutine(boardCell.BoardCellMovement.MovementPath(path, (check) =>
                {
                    LevelManager.Instance.boosterCtrl.LastMove = (boardCell, container, path);
                    LevelManager.Instance.boosterCtrl.ContainerLastMove = container;
                    LevelManager.Instance.boosterCtrl.UndoQueue.Clear();
                    for (int i = 0; i < LevelManager.Instance.cellPlayCtrl.BoardCells.Count; i++)
                    {
                        if (LevelManager.Instance.cellPlayCtrl.BoardCells[i].TypeItem == boardCell.TypeItem && LevelManager.Instance.cellPlayCtrl.BoardCells[i] != boardCell)
                        {
                            LevelManager.Instance.boosterCtrl.UndoQueue.Enqueue(new KeyValuePair<BoardCell, Container>(LevelManager.Instance.cellPlayCtrl.BoardCells[i], LevelManager.Instance.cellPlayCtrl.CellPlays[i]));
                        }
                    }
                    if (check && LevelManager.Instance.cellPlayCtrl.HasMatch3())
                    {
                        CustomeEventSystem.Instance.CheckMatch_3();
                        LevelManager.Instance.boosterCtrl.IsMatch3 = true;
                    }
                    else
                    {
                        LevelManager.Instance.boosterCtrl.IsMatch3 = false;
                        LevelManager.Instance.cellPlayCtrl.checkLose();
                    }
                }));
                StartCoroutine(LevelManager.Instance.BoardCtrl.SpawnBlockToGSPAction.Invoke(container, null));
            }
            else
            {
                LevelManager.Instance.cellPlayCtrl.CheckAndSaveBoardCell(boardCell);
                Vector3 pos = LevelManager.Instance.cellPlayCtrl.PosCell();
                boardCell.transform.localRotation = Quaternion.Euler(0, 180, 0);
                yield return boardCell.BoardCellMovement.MovementToPos(pos);
                boardCell.transform.localRotation = Quaternion.Euler(0, 0, 0);
                if (LevelManager.Instance.cellPlayCtrl.HasMatch3())
                {
                    CustomeEventSystem.Instance.CheckMatch_3();
                    LevelManager.Instance.boosterCtrl.IsMatch3 = true;
                }
                else
                {
                    LevelManager.Instance.boosterCtrl.IsMatch3 = false;
                    LevelManager.Instance.cellPlayCtrl.checkLose();
                }
            }
        }
    }
}