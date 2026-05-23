using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PopupAddHeart : PopupUI
{
    public const int COIN_COST_PER_HEART = 500;

    [Header("Buy with coins")]
    [SerializeField] private Button buttonBuyWithCoins;
    [SerializeField] private TextMeshProUGUI textCoinCost;

    [Header("Other sources")]
    [SerializeField] private Button buttonRequestFromFriend;
    [SerializeField] private Button buttonWatchAd;

    [Header("Status")]
    [SerializeField] private TextMeshProUGUI textHeartStatus;

    private void Awake()
    {
        if (buttonBuyWithCoins != null) buttonBuyWithCoins.onClick.AddListener(OnBuyWithCoinsClicked);
        if (buttonRequestFromFriend != null) buttonRequestFromFriend.onClick.AddListener(OnRequestFromFriendClicked);
        if (buttonWatchAd != null) buttonWatchAd.onClick.AddListener(OnWatchAdClicked);
        if (textCoinCost != null) textCoinCost.text = COIN_COST_PER_HEART.ToString();
    }

    private HeartManager heartManagerRef;

    public override void Show(Action onClose)
    {
        base.Show(onClose);
        heartManagerRef = HeartManager.Instance;
        if (heartManagerRef != null) heartManagerRef.OnHeartsChanged += Refresh;
        Loc.OnLanguageChanged += Refresh;
        Refresh();
    }

    public override void Hide()
    {
        if (heartManagerRef != null) heartManagerRef.OnHeartsChanged -= Refresh;
        heartManagerRef = null;
        Loc.OnLanguageChanged -= Refresh;
        base.Hide();
    }

    private void Refresh()
    {
        var hm = HeartManager.Instance;
        if (textHeartStatus != null)
        {
            if (hm.IsFull) textHeartStatus.text = $"{hm.Hearts}/{hm.MaxHearts}  FULL";
            else
            {
                int s = hm.SecondsUntilNextHeart;
                textHeartStatus.text = $"{hm.Hearts}/{hm.MaxHearts}  next in {s / 60}m{(s % 60):00}";
            }
        }

        // Để giống flow mua booster trong gameplay: nút luôn bấm được khi tim chưa đầy,
        // thiếu xu sẽ show toast "not_enough_coins" trong OnBuyWithCoinsClicked.
        if (buttonBuyWithCoins != null)
            buttonBuyWithCoins.interactable = !hm.IsFull;
        if (buttonWatchAd != null) buttonWatchAd.interactable = !hm.IsFull;
        if (buttonRequestFromFriend != null) buttonRequestFromFriend.interactable = !hm.IsFull;
    }

    private void OnBuyWithCoinsClicked()
    {
        AudioManager.Instance.PlayOneShot("BLJ_UI_Button_Default_01", 1f);
        var hm = HeartManager.Instance;
        if (hm.IsFull)
        {
            UIManager.Instance.NotifyContent(Loc.Get("hearts_full"));
            return;
        }
        if (UserData.coin < COIN_COST_PER_HEART)
        {
            UIManager.Instance.NotifyContent(Loc.Get("not_enough_coins"));
            return;
        }
        UserData.coin -= COIN_COST_PER_HEART;
        hm.Add(1);
        SaveDataManager.Save();
        // Phát event để HUD coin lobby refresh + đẩy snapshot lên Firestore.
        CustomeEventSystem.Instance?.ChangeCoin(UserData.coin);
        UserDataFirebaseManager.Instance?.PushCoinSnapshot();
    }

    private void OnRequestFromFriendClicked()
    {
        AudioManager.Instance.PlayOneShot("BLJ_UI_Button_Default_01", 1f);
        Hide();
        var tabPopup = UIManager.Instance.GetPopupActive<PopupTab>();
        if (tabPopup != null)
        {
            tabPopup.SelectTab(StatusChoice.League);
        }
        else
        {
            UIManager.Instance.ShowScreen<ScreenLeague>();
        }
    }

    private void OnWatchAdClicked()
    {
        AudioManager.Instance.PlayOneShot("BLJ_UI_Button_Default_01", 1f);
        // TODO: replace with AdsHelper.Instance.ShowRewarded(state => { if (rewarded) GrantHeart(); });
#if UNITY_EDITOR
        GrantHeart();
#else
        UIManager.Instance.NotifyContent(Loc.Get("ads_not_available"));
#endif
    }

    private void GrantHeart()
    {
        HeartManager.Instance.Add(1);
    }
}
