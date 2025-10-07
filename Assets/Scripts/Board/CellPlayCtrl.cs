using System.Collections.Generic;
using UnityEngine;

public class CellPlayCtrl : MonoBehaviour
{
    [SerializeField] List<BoardCell> boardCells;
    [SerializeField] List<Transform> CellPlayPos;

    public void SortTheCell()
    {
        // For Override
    }

    public void CombineCell()
    {
        // For Override
    }

    public void Explode()
    {
        // For Override
    }
}
