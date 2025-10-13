using UnityEngine;

public class BoardCellAnimation : MonoBehaviour
{
    [SerializeField] private Animator anim;

    void Start()
    {
        //anim.Set
    }

    public void SetActive()
    {
        anim.SetBool("IsActive", true);
    }

    public void SetRunning()
    {
        anim.SetTrigger("StartRunTrigger");
        anim.SetFloat("Speed", 1f);
    }

    public void SetIdle()
    {
        anim.SetFloat("Speed", 0f);
    }

    public void SetRaise()
    {
        anim.SetTrigger("RaiseTrigger");
        Debug.Log("RaiseTrigger is done");
    }

    
}
