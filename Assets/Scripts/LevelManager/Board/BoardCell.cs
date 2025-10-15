using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TypeItem
{
    BlueBase,
    BrownBase,
    GreenBase,
    MagentaBase

} //=> for override

public class BoardCell : MonoBehaviour
{
    [Header("Item Settings")]
    [SerializeField] string idType;
    [SerializeField] private TypeItem typeItem;
    [SerializeField] private bool hasClick;

    // [Header("Visual References")]
    // [SerializeField] private MeshFilter meshFilter;
    // [SerializeField] private MeshRenderer meshRenderer;

    // [Header("Variants")]
    // [SerializeField] private List<Mesh> meshTypes;
    // [SerializeField] private List<Material> materialTypes;

    [Header("Neighbors")]
    [SerializeField] private List<BoardCell> neighbors = new List<BoardCell>();

    [Header("Other Info")]
    [SerializeField] private Vector3 pos;
    [SerializeField] private BoardCellAnimation boardCellAnimation;
    [SerializeField] private BoxCollider box;
    [SerializeField] private BoardCellMovement boardCellMovement;
    [SerializeField] private Container container;// container init start contains it
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


    // public List<BoardCell> Neighbors => neighbors;
    // public TypeItem TypeItem { get => typeItem; set => typeItem = value; }

    // === Methods ===
    // public void ChangItemFromId(Dictionary<string, TypeItem> idTypes)
    // {
    //     ChangeItem(idTypes[idType]);
    // }
    // public void ChangeItem(TypeItem newTypeItem)
    // {
    //     typeItem = newTypeItem;

    //     int index = (int)newTypeItem;

    //     // An toàn tránh lỗi out of range
    //     if (index < 0 || index >= materialTypes.Count || index >= meshTypes.Count)
    //     {
    //         Debug.LogWarning($"Index {index} vượt quá giới hạn Mesh/Material trong {name}");
    //         return;
    //     }

    //     // Gán lại mesh & material
    //     if (meshFilter != null)
    //         meshFilter.mesh = meshTypes[index];

    //     if (meshRenderer != null)
    //         meshRenderer.material = materialTypes[index];
    // }

    public void AddNeighbor(BoardCell cell)
    {
        if (cell != null && !neighbors.Contains(cell))
            neighbors.Add(cell);
    }

    public void RemoveNeighbor(BoardCell cell)
    {
        neighbors.Remove(cell);
    }

    public IEnumerator SetActiveNeighBor()
    {
        for (int i = 0; i < neighbors.Count; i++)
        {
            if (neighbors[i].Barrel.activeSelf)
            {
                StartCoroutine(neighbors[i].PlayBarrelAnimation());
            }
            neighbors[i].HasClick = true;
            neighbors[i].BoardCellAnimation.SetActive();
            neighbors[i].RemoveNeighbor(this);
            yield return new WaitForSeconds(0.25f);
            neighbors[i].Barrel.SetActive(false);
        }
        yield break;
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
}
