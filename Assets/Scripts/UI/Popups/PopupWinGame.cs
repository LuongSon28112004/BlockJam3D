using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;
using System;

public class PopupWinGame : PopupUI
{
    [Header("UI References")]
    [SerializeField] private Button buttonTapToContinue;
    [SerializeField] private Transform source;           // vị trí bắt đầu coin
    [SerializeField] private Transform destination;      // vị trí đích coin
    [SerializeField] private Sprite spriteCoin;
    [SerializeField] private TextMeshProUGUI totalCountText;
    [SerializeField] private Transform coinParent;
    [SerializeField] private TextMeshProUGUI textLevelPass;       // parent riêng để chứa coin tạm thời

    [Header("Coin Settings")]
    [SerializeField] private int addCoin = 20;               // số coin nhận được
    [SerializeField] private float coinFlyDuration = 0.4f;   // thời gian bay
    [SerializeField] private float spawnDelay = 0.05f;       // delay giữa các coin
    [SerializeField] private float delayBeforeShowButton = 0.3f;
    [SerializeField, Range(0.1f, 1f)] private float coinStartScale = 0.25f; // scale coin ban đầu = 1/4

    private void Awake()
    {
        buttonTapToContinue.onClick.AddListener(OnOkClicked);
    }

    public override void Show(Action onClose = null)
    {
        base.Show(onClose);
        StartCoroutine(PlayWinEffect());
    }

    private IEnumerator PlayWinEffect()
    {
        buttonTapToContinue.gameObject.SetActive(false);
        int currentCoin = UserData.coin;
        totalCountText.text = currentCoin.ToString();
        textLevelPass.text = "Level " + UserData.level.ToString() + " Passed";
        yield return new WaitForSeconds(2f);


        AudioManager.Instance.PlayOneShot("BLJ_Legacy_CoinCollect", 1f);

        // Tạo hiệu ứng coin bay
        for (int i = 0; i < addCoin; i++)
        {
            GameObject coin = new GameObject($"FlyingCoin_{i}");
            Image coinImage = coin.AddComponent<Image>();
            coinImage.sprite = spriteCoin;
            coinImage.SetNativeSize();

            // Đặt parent để không bị ảnh hưởng layout UI
            coin.transform.SetParent(coinParent != null ? coinParent : transform, false);
            coin.transform.position = source.position;
            coin.transform.localScale = Vector3.one * coinStartScale; // coin nhỏ 1/4 kích thước gốc

            // Random hướng bay nhẹ cho tự nhiên
            Vector3 randomOffset = new Vector3(
                UnityEngine.Random.Range(-50f, 50f),
                UnityEngine.Random.Range(30f, 80f),
                0f
            );

            // Tạo sequence di chuyển + scale
            Sequence seq = DOTween.Sequence();
            seq.Append(coin.transform.DOMove(destination.position + randomOffset, coinFlyDuration * 0.6f)
                .SetEase(Ease.OutQuad));
            seq.Append(coin.transform.DOMove(destination.position, coinFlyDuration * 0.4f)
                .SetEase(Ease.InQuad));
            seq.Join(coin.transform.DOScale(Vector3.one * coinStartScale * 0.2f, coinFlyDuration)
                .SetEase(Ease.InQuad));
            seq.OnComplete(() => Destroy(coin, 0.05f));
            seq.Play();

            yield return new WaitForSeconds(spawnDelay * 0.8f);
        }

        // Chờ animation kết thúc
        yield return new WaitForSeconds(coinFlyDuration + 0.3f);

        // Cập nhật số coin người chơi
        int targetCoin = currentCoin + 4;// để tạm là 4
        int displayedCoin = currentCoin;

        DOTween.To(() => displayedCoin, x =>
        {
            displayedCoin = x;
            totalCountText.text = displayedCoin.ToString();
        }, targetCoin, 0.5f)
        .SetEase(Ease.OutCubic)
        .OnComplete(
            () =>
            {
                UserData.coin = targetCoin;
                SaveDataManager.Save();
            }
        );


        // Hiện nút "Tap to Continue" sau delay
        yield return new WaitForSeconds(delayBeforeShowButton);
        buttonTapToContinue.gameObject.SetActive(true);
        buttonTapToContinue.transform.DOPunchScale(Vector3.one * 0.1f, 0.3f, 5, 0.5f);
    }

    private void OnOkClicked()
    {
        GameManager.Instance.BackToMenu();
        //Hide();
    }
}
