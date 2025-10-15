using UnityEngine;

public class GSPLeftAnimationCtrl : BaseGridSpotAnimation
{
    public override void SetAnimationExit()
    {
        if (animator == null)
        {
            Debug.LogWarning("Animator chưa được gán!");
            return;
        }
        animator.SetTrigger("blockExit");
        animator.SetInteger("ExitDirection", Random.Range(1, 4));
    }
}
