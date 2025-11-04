using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class PopupSettings : PopupUI
{
    [SerializeField] private Button quitLevel;

    void Awake()
    {
        quitLevel.onClick.AddListener(BackToMain);
    }

    private void BackToMain()
    {
        //Audio sound
        AudioManager.Instance.PlayOneShot("BLJ_UI_Button_Default_01", 1f);
        GameManager.Instance.BackToMenu();
    }
}
