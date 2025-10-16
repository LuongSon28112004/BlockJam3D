using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PopupLoseGame : PopupUI
{
    [SerializeField] private Button buttonTryAgain;

    private void Awake()
    {
        buttonTryAgain.onClick.AddListener(OnOkClicked);
    }

    private void OnOkClicked()
    {
        Hide();
        GameManager.Instance.StartGame();
    }
}
