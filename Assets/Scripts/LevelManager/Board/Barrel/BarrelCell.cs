using UnityEngine;

public class BarrelCell : MonoBehaviour
{
    [SerializeField] private BarrelCelAnimation barrelCelAnimation;
    public BarrelCelAnimation BarrelCelAnimation { get => barrelCelAnimation; set => barrelCelAnimation = value; }
}
