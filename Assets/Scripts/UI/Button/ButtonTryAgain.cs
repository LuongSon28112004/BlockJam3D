using UnityEngine;
using UnityEngine.SceneManagement;

public class ButtonTryAgain : BaseButton
{
    protected override void OnClick()
    {
       SceneManager.LoadScene("GamePlay", LoadSceneMode.Single);

    }
}
