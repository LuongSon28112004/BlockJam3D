using UnityEngine;
using UnityEngine.SceneManagement;

public class ButtonPlay : BaseButton
{
    protected override void OnClick()
    {
        SceneManager.LoadScene("GamePlay", LoadSceneMode.Single);

    }
}