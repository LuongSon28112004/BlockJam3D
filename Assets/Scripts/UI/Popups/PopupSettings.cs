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
        GameManager.Instance.BackToMenu();
    }
}
