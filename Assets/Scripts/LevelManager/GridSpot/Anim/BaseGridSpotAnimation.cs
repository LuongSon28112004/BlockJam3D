using UnityEngine;

public class BaseGridSpotAnimation : MonoBehaviour
{
    [SerializeField] protected Animator animator;

    public virtual void SetAnimationExit() { }
    public virtual void SetAnimationExit(int direction) { }
}
