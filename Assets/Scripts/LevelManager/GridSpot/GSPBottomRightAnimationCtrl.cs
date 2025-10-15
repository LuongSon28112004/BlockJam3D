using UnityEngine;

public class GSPBottomRightAnimationCtrl : BaseGridSpotAnimation
{
    public override void SetAnimationExit(int direction)
    {
        base.SetAnimationExit(direction);
        if (animator == null)
        {
            Debug.LogWarning("Animator chưa được gán!");
            return;
        }
        animator.SetTrigger("blockExit");
        animator.SetInteger("ExitDirection", direction);
    }
}
