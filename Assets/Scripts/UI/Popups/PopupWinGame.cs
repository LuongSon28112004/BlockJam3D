using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PopupWinGame : PopupUI
{
    [SerializeField] private Button buttonOk;

    private void Awake()
    {
        buttonOk.onClick.AddListener(OnOkClicked);
    }

    private void OnOkClicked()
    {
        Hide();
        SceneManager.LoadSceneAsync("UIMain", LoadSceneMode.Single);
    }
}
