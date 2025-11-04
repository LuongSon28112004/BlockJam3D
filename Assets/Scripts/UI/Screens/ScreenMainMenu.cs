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
        //Audio sound
        AudioManager.Instance.PlayOneShot("BLJ_UI_Button_Default_01", 1f);
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
        if (UserData.level >= 7) GameManager.Instance.Level = 6;
        else GameManager.Instance.Level = UserData.level;
        AudioManager.Instance.PlayOneShot("BLJ_UI_Button_Default_01", 1f);
        GameManager.Instance.StartGame();
    }
}
