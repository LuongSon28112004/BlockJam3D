using UnityEngine;
using UnityEngine.UI;

public class ScreenMainMenu : ScreenUI
{
    [SerializeField] private Button buttonPlay;

    private void Awake()
    {
        buttonPlay.onClick.AddListener(OnPlayClicked);
    }

    private void OnPlayClicked()
    {
        GameManager.Instance.StartGame();
    }
}
