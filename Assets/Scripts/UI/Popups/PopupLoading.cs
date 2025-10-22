using UnityEngine;

public class PopupLoading : PopupUI
{
    [SerializeField] GameObject Loading;


    public void ShowLoading()
    {
        Loading.SetActive(true);
    }

    void OnEnable()
    {
        CustomeEventSystem.Instance.ShowLoadingAction += ShowLoading;
    }

    void OnDisable()
    {
        CustomeEventSystem.Instance.ShowLoadingAction -= ShowLoading;
    }


}
