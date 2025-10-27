using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TypeItem
{
    BlueBase,
    BrownBase,
    GreenBase,
    MagentaBase,
    RedBase,
    PurpleBase,
    YellowBase,

} //=> for override

public enum DirectionNeighBor
{
    Top,
    Bottom,
    Left,
    Right,
}

[Serializable]
public class NeighBors
{
    // nó đang ở hướng nào so với gameobject hàng xóm của nó
    public DirectionNeighBor directionNeighBor;
    public BoardCell boardCell;
    public bool isActivatedByNeighbor;

    public NeighBors(BoardCell boardCell , DirectionNeighBor directionNeighBor , bool isActivatedByNeighbor)
    {
        this.boardCell = boardCell;
        this.directionNeighBor = directionNeighBor;
        this.isActivatedByNeighbor = isActivatedByNeighbor;
    }



    public override bool Equals(object obj)
    {
        if (obj is NeighBors other)
        {
            return directionNeighBor == other.directionNeighBor &&
                   boardCell == other.boardCell;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return (directionNeighBor, boardCell).GetHashCode();
    }
}

public class BoardCell : MonoBehaviour
{
    [Header("Item Settings")]
    [SerializeField] string idType;
    [SerializeField] private TypeItem typeItem;
    [SerializeField] private bool hasClick;
    [SerializeField] private bool isBoosterAdd;
    [SerializeField] private bool isInCellPlay;
    [Header("Neighbors")]
    [SerializeField] private List<NeighBors> neighbors = new List<NeighBors>();

    [Header("Other Info")]
    [SerializeField] private Vector3 pos;
    [SerializeField] private BoardCellAnimation boardCellAnimation;
    [SerializeField] private BoxCollider box;
    [SerializeField] private BoardCellMovement boardCellMovement;
    [SerializeField] private Container container;
    [Header("Barrel")]
    [SerializeField] private GameObject barrel;
    [SerializeField] private BarrelCell barrelCell;

    // === Properties ===
    public Vector3 Pos { get => pos; set => pos = value; }
    public string IdType { get => idType; set => idType = value; }
    public Container Container { get => container; set => container = value; }
    public TypeItem TypeItem { get => typeItem; set => typeItem = value; }
    public BoardCellMovement BoardCellMovement { get => boardCellMovement; set => boardCellMovement = value; }
    public bool HasClick { get => hasClick; set => hasClick = value; }
    public BoardCellAnimation BoardCellAnimation { get => boardCellAnimation; set => boardCellAnimation = value; }
    public GameObject Barrel { get => barrel; set => barrel = value; }
    public BarrelCell BarrelCell { get => barrelCell; set => barrelCell = value; }
    public bool IsBoosterAdd { get => isBoosterAdd; set => isBoosterAdd = value; }
    public List<NeighBors> Neighbors { get => neighbors; set => neighbors = value; }
    public bool IsInCellPlay { get => isInCellPlay; set => isInCellPlay = value; }

    public void AddNeighbor(BoardCell cell, DirectionNeighBor directionNeighBor)
    {
        if (cell != null && !neighbors.Contains(new NeighBors(cell, directionNeighBor, false)))
        {
            neighbors.Add(new NeighBors(cell, directionNeighBor, false));
        }
    }

    public IEnumerator SetActiveNeighBor()
    {
        for (int i = 0; i < neighbors.Count; i++)
        {
            if (neighbors[i] == null) continue;
            if (neighbors[i].boardCell.IsInCellPlay) continue;
            if (neighbors[i].boardCell.Barrel == null) continue;
            if (neighbors[i].boardCell.Barrel.activeSelf)
            {
                StartCoroutine(neighbors[i].boardCell.PlayBarrelAnimation());
            }
            if (neighbors[i].boardCell.HasClick == true) continue;
            neighbors[i].boardCell.HasClick = true;
            neighbors[i].isActivatedByNeighbor = true;
            neighbors[i].boardCell.BoardCellAnimation.SetActive();
            //neighbors[i].boardCell.RemoveNeighbor(this);
            //yield return new WaitForSeconds(0.25f);
            //if (neighbors[i].boardCell.barrel == null) yield break;
            //neighbors[i].boardCell.Barrel.SetActive(false);
        }
        yield break;
    }
    
    public void SetInActiveNeighBor()
    {
        for (int i = 0; i < neighbors.Count; i++)
        {
            if (neighbors[i].boardCell == null || neighbors[i].boardCell.IsInCellPlay || !neighbors[i].isActivatedByNeighbor) continue;
            neighbors[i].boardCell.HasClick = false;
            neighbors[i].boardCell.BoardCellAnimation.SetInActive();
        }
    }

    public void ClearNeighbors()
    {
        neighbors.Clear();
    }

    public IEnumerator PlayBarrelAnimation()
    {
        if (barrelCell != null && barrelCell.BarrelCelAnimation != null)
            StartCoroutine(barrelCell.BarrelCelAnimation.PlayBarrelAnimation());
        else
            yield break;
    }

    public void PlayBarrelAnimationDefaut()
    {
        barrelCell.BarrelCelAnimation.PlayBarrelDefault();
    }
}
