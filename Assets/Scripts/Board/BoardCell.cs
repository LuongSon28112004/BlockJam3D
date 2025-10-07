using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public enum TypeItem
{
    TypeOne,
    TypeTwo,
    TypeThree,
}

public class BoardCell : MonoBehaviour
{
    [SerializeField] TypeItem typeItem;
    [SerializeField] List<BoardCell> neighBor;
    [SerializeField] bool hasClick;
    [SerializeField] Vector3 pos;
    [SerializeField] Animator anim;
    [SerializeField] BoxCollider box;

    public Vector3 Pos { get => pos; set => pos = value; }
    public List<BoardCell> NeighBor { get => neighBor; set => neighBor = value; }
}
