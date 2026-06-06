using UnityEngine;
using TMPro;

public class ScreenShop : ScreenUI
{
    [SerializeField] private TextMeshProUGUI _textCoin;

    private void OnEnable()
    {
        UpdateCoinText();
        if (CustomeEventSystem.Instance != null)
            CustomeEventSystem.Instance.ChangeCoinAction += OnCoinChanged;
    }

    private void OnDisable()
    {
        if (CustomeEventSystem.Instance != null)
            CustomeEventSystem.Instance.ChangeCoinAction -= OnCoinChanged;
    }

    private void OnCoinChanged(int coin)
    {
        _textCoin.text = coin.ToString();
    }

    public void UpdateCoinText()
    {
        _textCoin.text = UserData.coin.ToString();
    }
}
