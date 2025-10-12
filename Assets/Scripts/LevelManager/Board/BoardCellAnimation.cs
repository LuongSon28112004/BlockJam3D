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

    
}
