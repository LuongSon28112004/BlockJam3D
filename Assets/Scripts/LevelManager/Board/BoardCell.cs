using System.Collections.Generic;
using UnityEngine;

public enum TypeItem
{
    TypeOne,   // Đỏ
    TypeTwo,   // Vàng
    TypeThree, // Xanh
} //=> for override

public class BoardCell : MonoBehaviour
{
    [Header("Item Settings")]
    [SerializeField] string idType;
    [SerializeField] private TypeItem typeItem;

    [Header("Visual References")]
    [SerializeField] private MeshFilter meshFilter;
    [SerializeField] private MeshRenderer meshRenderer;

    [Header("Variants")]
    [SerializeField] private List<Mesh> meshTypes;
    [SerializeField] private List<Material> materialTypes;

    [Header("Neighbors")]
    [SerializeField] private List<BoardCell> neighbors = new List<BoardCell>();

    [Header("Other Info")]
    [SerializeField] private bool hasClick;
    [SerializeField] private Vector3 pos;
    [SerializeField] private Animator anim;
    [SerializeField] private BoxCollider box;
    [SerializeField] private BoardCellMovement boardCellMovement;

    // === Properties ===
    public Vector3 Pos { get => pos; set => pos = value; }
    public string IdType { get => idType; set => idType = value; }

    // public List<BoardCell> Neighbors => neighbors;
    // public TypeItem TypeItem { get => typeItem; set => typeItem = value; }

    // === Methods ===
    public void ChangItemFromId(Dictionary<string, TypeItem> idTypes)
    {
        ChangeItem(idTypes[idType]);
    }
    public void ChangeItem(TypeItem newTypeItem)
    {
        typeItem = newTypeItem;

        int index = (int)newTypeItem;

        // An toàn tránh lỗi out of range
        if (index < 0 || index >= materialTypes.Count || index >= meshTypes.Count)
        {
            Debug.LogWarning($"Index {index} vượt quá giới hạn Mesh/Material trong {name}");
            return;
        }

        // Gán lại mesh & material
        if (meshFilter != null)
            meshFilter.mesh = meshTypes[index];

        if (meshRenderer != null)
            meshRenderer.material = materialTypes[index];
    }

    public void AddNeighbor(BoardCell cell)
    {
        if (cell != null && !neighbors.Contains(cell))
            neighbors.Add(cell);
    }

    public void ClearNeighbors()
    {
        neighbors.Clear();
    }
}
