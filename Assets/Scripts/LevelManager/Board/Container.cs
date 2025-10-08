using UnityEngine;

public class Container : MonoBehaviour
{
    [SerializeField] private bool isContaining;
    [SerializeField] private Vector3 pos;

    public bool IsContaining { get => isContaining; set => isContaining = value; }
    public Vector3 Pos { get => pos; set => pos = value; }
}
