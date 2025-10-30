using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum StatusChoice {
    Shop,
    Mission,
    MainMenu,
    League,
    Collection,
}

public class PopupTab : PopupUI
{
    [Header("Buttons")]
    [SerializeField] private Button ShopButton;
    [SerializeField] private Button MissionButton;
    [SerializeField] private Button MainMenuButton;
    [SerializeField] private Button LeagueButton;
    [SerializeField] private Button CollectionButton;
    [Header("TextName")]
    [SerializeField] private TextMeshProUGUI TextShop;
    [SerializeField] private TextMeshProUGUI TextMission;
    [SerializeField] private TextMeshProUGUI TextMainMenu;
    [SerializeField] private TextMeshProUGUI TextLeague;
    [SerializeField] private TextMeshProUGUI TextCollection;

    [Header("Parents")]
    [SerializeField] private GameObject ChoicePanel;
    [SerializeField] private Transform Parent;

    private StatusChoice currentStatus;

    void Start()
    {
        AddButtonListeners();
        currentStatus = StatusChoice.MainMenu; // hoặc cái nào là mặc định hiển thị
    }

    private void AddButtonListeners()
    {
        ShopButton.onClick.AddListener(() => ChangeStatusChoicePanel(StatusChoice.Shop));
        MissionButton.onClick.AddListener(() => ChangeStatusChoicePanel(StatusChoice.Mission));
        MainMenuButton.onClick.AddListener(() => ChangeStatusChoicePanel(StatusChoice.MainMenu));
        LeagueButton.onClick.AddListener(() => ChangeStatusChoicePanel(StatusChoice.League));
        CollectionButton.onClick.AddListener(() => ChangeStatusChoicePanel(StatusChoice.Collection));
    }

    private void ChangeStatusChoicePanel(StatusChoice newChoice)
    {
        if (newChoice == currentStatus) return;


        //Sound
        AudioManager.Instance.PlayOneShot("BLJ_UI_Tab_04", 1f);
        // Đưa tab cũ về parent gốc
        ExitChild(currentStatus);

        // Gán tab mới vào ChoicePanel
        Button chosenButton = GetButton(newChoice);
        chosenButton.transform.SetParent(ChoicePanel.transform, false);
        Button oldButton = GetButton(currentStatus);
        chosenButton.transform.DOScale(new Vector3(1.2f, 1.2f, 1.2f), 0.1f);
        oldButton.transform.DOScale(Vector3.one, 0.1f);
        ActiveText(newChoice);
        InactiveOldText(currentStatus);
        ChoicePanel.transform.SetSiblingIndex(GetSiblingIndex(newChoice));


        // Reset vị trí, anchor trung tâm
        RectTransform rt = chosenButton.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;

        currentStatus = newChoice;
        ShowPopUpTab(currentStatus);
    }

    private void InactiveOldText(StatusChoice currentStatus)
    {
        switch (currentStatus)
        {
            case StatusChoice.Shop:
                TextShop.gameObject.SetActive(false);
                break;
            case StatusChoice.Mission:
                TextMission.gameObject.SetActive(false);
                break;
            case StatusChoice.MainMenu:
                TextMainMenu.gameObject.SetActive(false);
                break;
            case StatusChoice.League:
                TextLeague.gameObject.SetActive(false);
                break;
            case StatusChoice.Collection:
                TextCollection.gameObject.SetActive(false);
                break;
        }
    }

    private void ActiveText(StatusChoice currentStatus)
    {
        switch (currentStatus)
        {
            case StatusChoice.Shop:
                TextShop.gameObject.SetActive(true);
                break;
            case StatusChoice.Mission:
                TextMission.gameObject.SetActive(true);
                break;
            case StatusChoice.MainMenu:
                TextMainMenu.gameObject.SetActive(true);
                break;
            case StatusChoice.League:
                TextLeague.gameObject.SetActive(true);
                break;
            case StatusChoice.Collection:
                TextCollection.gameObject.SetActive(true);
                break;
        }
    }

    private void ShowPopUpTab(StatusChoice currentStatus)
    {
        switch (currentStatus)
        {
            case StatusChoice.Shop:
                UIManager.Instance.ShowScreen<ScreenShop>();
                break;
            case StatusChoice.Mission:
                UIManager.Instance.ShowScreen<ScreenMission>();
                break;
            case StatusChoice.MainMenu:
                UIManager.Instance.ShowScreen<ScreenMainMenu>();
                break;
            case StatusChoice.League:
                UIManager.Instance.ShowScreen<ScreenLeague>();
                break;
            case StatusChoice.Collection:
                UIManager.Instance.ShowScreen<ScreenCollection>();
                break;
        }
    }

    private void ExitChild(StatusChoice choice)
    {
        Button button = GetButton(choice);
        if (button == null) return;

        // Trả về parent gốc
        button.transform.SetParent(Parent, false);

        // Sắp xếp lại đúng thứ tự
        button.transform.SetSiblingIndex(GetSiblingIndex(choice));
    }

    private Button GetButton(StatusChoice choice)
    {
        return choice switch
        {
            StatusChoice.Shop => ShopButton,
            StatusChoice.Mission => MissionButton,
            StatusChoice.MainMenu => MainMenuButton,
            StatusChoice.League => LeagueButton,
            StatusChoice.Collection => CollectionButton,
            _ => null
        };
    }

    private int GetSiblingIndex(StatusChoice choice)
    {
        return choice switch
        {
            StatusChoice.Shop => 0,
            StatusChoice.Mission => 1,
            StatusChoice.MainMenu => 2,
            StatusChoice.League => 3,
            StatusChoice.Collection => 4,
            _ => 0
        };
    }
}
