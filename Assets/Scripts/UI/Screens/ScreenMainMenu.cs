using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ScreenMainMenu : ScreenUI
{
    [SerializeField] private Button buttonPlay;
    [SerializeField] private TextMeshProUGUI textLevel;
    [SerializeField] private TextMeshProUGUI textCoin;
    [SerializeField] private Button setting;

    private void Awake()
    {
        buttonPlay.onClick.AddListener(OnPlayClicked);
        setting.onClick.AddListener(OnSettingClicked);
    }

    private void OnSettingClicked()
    {
        UIManager.Instance.ShowPopup<PopupSettingsUIMain>(null);
    }

    void Start()
    {
        textLevel.text = " Play\n" +
        "Level " + UserData.level.ToString();
        textCoin.text = UserData.coin.ToString();
    }

    private void OnPlayClicked()
    {
        GameManager.Instance.Level = UserData.level;
        GameManager.Instance.StartGame();
    }
}
