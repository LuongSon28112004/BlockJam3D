using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ScreenMainMenu : ScreenUI
{
    [SerializeField] private Button buttonPlay;
    [SerializeField] private TextMeshProUGUI textLevel;
    [SerializeField] private TextMeshProUGUI textCoin;

    private void Awake()
    {
        buttonPlay.onClick.AddListener(OnPlayClicked);
    }

    void Start()
    {
        textLevel.text = " Play\n" +
        "Level " + UserData.level.ToString();
        textCoin.text = UserData.coin.ToString();
    }

    private void OnPlayClicked()
    {
        GameManager.Instance.StartGame();
    }
}
