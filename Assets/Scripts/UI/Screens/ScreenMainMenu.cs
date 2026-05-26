using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ScreenMainMenu : ScreenUI
{
    [SerializeField] private Button buttonPlay;
    [SerializeField] private TextMeshProUGUI textLevel;
    [SerializeField] private TextMeshProUGUI textCoin;
    [SerializeField] private Button setting;

    [Header("Heart HUD")]
    [SerializeField] private TextMeshProUGUI textHeartCount;
    [SerializeField] private TextMeshProUGUI textHeartTimer;
    [SerializeField] private Button buttonHeart;

    [Header("Daily Mission")]
    [SerializeField] private Button buttonDailyMission;
    [SerializeField] private GameObject dailyMissionBadge;

    private HeartManager heartManagerRef;
    private DailyMissionManager dailyMissionRef;

    private void Awake()
    {
        buttonPlay.onClick.AddListener(OnPlayClicked);
        setting.onClick.AddListener(OnSettingClicked);
        AutoDiscoverHeartUI();
        if (buttonHeart != null) buttonHeart.onClick.AddListener(OnHeartClicked);
        if (buttonDailyMission != null) buttonDailyMission.onClick.AddListener(OnDailyMissionClicked);
    }

    private void AutoDiscoverHeartUI()
    {
        Transform heathTf = FindHeartPill(transform);
        if (heathTf == null) return;

        if (textHeartCount == null)
        {
            Transform countTf = heathTf.Find("Count");
            if (countTf != null) textHeartCount = countTf.GetComponent<TextMeshProUGUI>();
        }
        if (textHeartTimer == null)
        {
            Transform timerTf = heathTf.parent != null ? heathTf.parent.Find("TextHeath") : null;
            if (timerTf == null) timerTf = FindDeep(transform, "TextHeath");
            if (timerTf != null) textHeartTimer = timerTf.GetComponent<TextMeshProUGUI>();
        }
        if (buttonHeart == null)
        {
            buttonHeart = heathTf.GetComponent<Button>();
            if (buttonHeart == null) buttonHeart = heathTf.gameObject.AddComponent<Button>();
        }
    }

    private static Transform FindHeartPill(Transform root)
    {
        if (root.name == "Heath" && root.Find("Count") != null) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindHeartPill(root.GetChild(i));
            if (found != null) return found;
        }
        return null;
    }

    private static Transform FindDeep(Transform root, string name)
    {
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDeep(root.GetChild(i), name);
            if (found != null) return found;
        }
        return null;
    }

    private void OnEnable()
    {
        heartManagerRef = HeartManager.Instance;
        if (heartManagerRef != null) heartManagerRef.OnHeartsChanged += RefreshHeartUI;
        Loc.OnLanguageChanged += RefreshLocalizedDynamicText;
        RefreshHeartUI();

        dailyMissionRef = DailyMissionManager.Instance;
        if (dailyMissionRef != null) dailyMissionRef.OnMissionProgressChanged += RefreshDailyMissionBadge;
        RefreshDailyMissionBadge();

        // Cập nhật text coin khi UserData.coin thay đổi (claim mission, mua heart, win level...).
        if (CustomeEventSystem.Instance != null) CustomeEventSystem.Instance.ChangeCoinAction += RefreshCoinText;
        RefreshCoinText(UserData.coin);
    }

    private void OnDisable()
    {
        if (heartManagerRef != null) heartManagerRef.OnHeartsChanged -= RefreshHeartUI;
        heartManagerRef = null;
        Loc.OnLanguageChanged -= RefreshLocalizedDynamicText;

        if (dailyMissionRef != null) dailyMissionRef.OnMissionProgressChanged -= RefreshDailyMissionBadge;
        dailyMissionRef = null;

        if (CustomeEventSystem.Instance != null) CustomeEventSystem.Instance.ChangeCoinAction -= RefreshCoinText;
    }

    private void RefreshCoinText(int coin)
    {
        if (textCoin != null) textCoin.text = coin.ToString();
    }

    private void OnDailyMissionClicked()
    {
        AudioManager.Instance.PlayOneShot("BLJ_UI_Button_Default_01", 1f);
        UIManager.Instance.ShowPopup<PopupDailyMission>(null);
    }

    private void RefreshDailyMissionBadge()
    {
        if (dailyMissionBadge == null) return;
        bool show = DailyMissionManager.Instance != null && DailyMissionManager.Instance.HasClaimableMission;
        dailyMissionBadge.SetActive(show);
    }

    private void RefreshLocalizedDynamicText()
    {
        if (textLevel != null) textLevel.text = Loc.Get("main_play_level_fmt", UserData.level);
    }

    private void OnSettingClicked()
    {
        //Audio sound
        AudioManager.Instance.PlayOneShot("BLJ_UI_Button_Default_01", 1f);
        UIManager.Instance.ShowPopup<PopupSettingsUIMain>(null);
    }

    void Start()
    {
        textLevel.text = Loc.Get("main_play_level_fmt", UserData.level);
        textCoin.text = UserData.coin.ToString();
    }

    private void Update()
    {
        RefreshTimerLabel();
    }

    private void RefreshHeartUI()
    {
        if (textHeartCount != null) textHeartCount.text = HeartManager.Instance.Hearts.ToString();
        RefreshTimerLabel();
    }

    private void RefreshTimerLabel()
    {
        if (textHeartTimer == null || HeartManager.Instance == null) return;
        if (HeartManager.Instance.IsFull) { textHeartTimer.text = Loc.Get("heart_full_label"); return; }
        int s = HeartManager.Instance.SecondsUntilNextHeart;
        textHeartTimer.text = $"{s / 60}m{(s % 60):00}";
    }

    private void OnHeartClicked()
    {
        AudioManager.Instance.PlayOneShot("BLJ_UI_Button_Default_01", 1f);
        if (Resources.Load<PopupAddHeart>("UI/Popups/PopupAddHeart") == null)
        {
            UIManager.Instance.NotifyContent(Loc.Get("addheart_popup_missing"));
            return;
        }
        UIManager.Instance.ShowPopup<PopupAddHeart>(null);
    }

    private void OnPlayClicked()
    {
        if (HeartManager.Instance != null && HeartManager.Instance.Hearts <= 0)
        {
            AudioManager.Instance.PlayOneShot("BLJ_UI_Button_Default_01", 1f);
            UIManager.Instance.NotifyContent(Loc.Get("no_hearts_wait_regen"));
            return;
        }
        if (UserData.level >= 11) GameManager.Instance.Level = 10;
        else GameManager.Instance.Level = UserData.level;
        AudioManager.Instance.PlayOneShot("BLJ_UI_Button_Default_01", 1f);
        GameManager.Instance.StartGame();
    }
}
