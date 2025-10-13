using UnityEngine;
using UnityEngine.SceneManagement;

public class ButtonWinOk : BaseButton
{
    protected override void OnClick()
    {
        SceneManager.LoadSceneAsync("UIMain", LoadSceneMode.Single);
    }
}
