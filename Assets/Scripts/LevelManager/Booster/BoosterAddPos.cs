using System;
using System.Collections.Generic;
using UnityEngine;

public class BoosterAddPos : MonoBehaviour
{
    [Header("Booster Add Pos Components")]
    [SerializeField] private List<Vector3> listPosBoosterAdd;
    [SerializeField] private List<Container> containers;
    [SerializeField] private List<Transform> listTransformBoossterAdd;
    [SerializeField] private List<BoardCell> boardCells;

    public List<Vector3> ListPosBoosterAdd { get => listPosBoosterAdd; set => listPosBoosterAdd = value; }
    public List<Container> Containers { get => containers; set => containers = value; }
    public List<BoardCell> BoardCells { get => boardCells; set => boardCells = value; }

    public void Start()
    {
        listPosBoosterAdd = new List<Vector3>();
        boardCells = new List<BoardCell>();
        InitPosBoosterAdd();
    }

    private void InitPosBoosterAdd()
    {
        for (int i = 0; i < listTransformBoossterAdd.Count; i++)
        {
            listPosBoosterAdd.Add(listTransformBoossterAdd[i].position);
            containers[i].Pos = listTransformBoossterAdd[i].position;
        }
    }
    
    private void SortBoardCell(int index)
    {
        for (int i = index; i < boardCells.Count && i < listPosBoosterAdd.Count; i++)
        {
            var bc = boardCells[i].BoardCellMovement;
            if (bc == null) continue;
            StartCoroutine(bc.MovementToPos(listPosBoosterAdd[i]));
        }
    }

    public void RemoveBoardCell(BoardCell boardCell)
    {
        int index = boardCells.IndexOf(boardCell);
        if (index < 0) return;
        
        containers[boardCells.Count - 1].IsContaining = false;
        boardCells.RemoveAt(index);
        SortBoardCell(index);
    }

   

}
