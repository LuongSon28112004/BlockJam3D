using UnityEngine;
using TMPro;

public class ScreenShop : ScreenUI
{
    [SerializeField] private TextMeshProUGUI _textCoin;

    private void OnEnable()
    {
        UpdateCoinText();
    }

    public void UpdateCoinText()
    {
        _textCoin.text = UserData.coin.ToString();
    }
}
