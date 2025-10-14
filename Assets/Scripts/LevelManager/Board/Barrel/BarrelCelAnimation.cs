using System.Collections;
using UnityEngine;

public class BarrelCelAnimation : MonoBehaviour
{
    [SerializeField] private Animator animator;

    public IEnumerator PlayBarrelAnimation()
    {
        if (animator == null)
        {
            Debug.LogWarning("Animator chưa được gán!");
            yield break;
        }

        animator.SetTrigger("SetDefaultState");
        animator.SetInteger("BlockState", 1);
        yield return new WaitForSeconds(0.01f);
        animator.SetInteger("BlockState", 0);
        animator.SetInteger("PopRandom", Random.Range(1, 4));
    }
}
