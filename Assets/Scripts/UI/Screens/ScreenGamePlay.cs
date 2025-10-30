using System;
using System.Collections.Generic;
using DG.Tweening;
using NUnit.Framework;
using TMPro;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class ScreenGamePlay : ScreenUI
{
    [Header("Screen Gameplay Components")]
    [SerializeField] private Button UndoButton;
    [SerializeField] private Button AddButton;
    [SerializeField] private Button ShuffleButton;
    [SerializeField] private Button MagnetButton;
    [SerializeField] private Button SettingButton;
    [Header("Round")]
    [SerializeField] private Sprite IconRoundDo;
    [SerializeField] private Image Round_1;
    [SerializeField] private Image Round_2;
    [SerializeField] private Image Round_3;
    [SerializeField] private TextMeshProUGUI textLevel;

    [Header("Coin")]
    [SerializeField] private TextMeshProUGUI textCoin;

    [Header("Booster")]
    [SerializeField] private Image iconUndo;
    [SerializeField] private Image iconAdd;
    [SerializeField] private Image iconShuffle;
    [SerializeField] private Image iconMagnet;

    [SerializeField] private TextMeshProUGUI textUndo;
    [SerializeField] private TextMeshProUGUI textAdd;
    [SerializeField] private TextMeshProUGUI textShuffle;
    [SerializeField] private TextMeshProUGUI textMagnet;

    [SerializeField] List<BoosterConfig> listBoosterConfigs;

    [SerializeField] private List<BoosterData> boosterDatas;


    private void OnEnable()
    {
        UndoButton.onClick.AddListener(OnClickUndo);
        AddButton.onClick.AddListener(OnClickAdd);
        ShuffleButton.onClick.AddListener(OnClickShuffle);
        MagnetButton.onClick.AddListener(OnClickMagnet);
        SettingButton.onClick.AddListener(OnClickSetting);
        listBoosterConfigs[0].Inactive();
        listBoosterConfigs[1].Inactive();
        CustomeEventSystem.Instance.ChangeRoundAction += ChangeRound;
        CustomeEventSystem.Instance.ChangeCoinAction += ChangeTextCoin;
        CustomeEventSystem.Instance.ActiveBoosterAction += ActiveBooster;
        CustomeEventSystem.Instance.InactiveBoosterAction += InActiveBooster;
    }

    private void OnDisable()
    {
        UndoButton.onClick.RemoveListener(OnClickUndo);
        AddButton.onClick.RemoveListener(OnClickAdd);
        ShuffleButton.onClick.RemoveListener(OnClickShuffle);
        MagnetButton.onClick.RemoveListener(OnClickMagnet);
        CustomeEventSystem.Instance.ChangeRoundAction -= ChangeRound;
        CustomeEventSystem.Instance.ChangeCoinAction -= ChangeTextCoin;
        CustomeEventSystem.Instance.ActiveBoosterAction -= ActiveBooster;
        CustomeEventSystem.Instance.InactiveBoosterAction -= InActiveBooster;
    }

    public void ActiveBooster()
    {
        listBoosterConfigs[0].Active();
        listBoosterConfigs[1].Active();
    }

    public void InActiveBooster()
    {
        listBoosterConfigs[0].Inactive();
        listBoosterConfigs[1].Inactive();
    }


    void Start()
    {
        addAnimationIcon();
        this.InitCoinText();
        this.InitPriceBooster();
        this.LoadTextLevel();
    }

    private void LoadTextLevel()
    {
        textLevel.text = "Level "+  UserData.level.ToString();
    }

    private void InitPriceBooster()
    {
        textUndo.text = boosterDatas[0].price.ToString();
        textAdd.text = boosterDatas[1].price.ToString();
        textShuffle.text = boosterDatas[2].price.ToString();
        textMagnet.text = boosterDatas[3].price.ToString();
    }

    private void InitCoinText()
    {
        textCoin.text = UserData.coin.ToString();
    }

    private void addAnimationIcon()
    {
        float moveAmount = 15f; // pixel di chuyển, UI dùng pixel chứ không dùng đơn vị world

        iconUndo.rectTransform.DOAnchorPosY(iconUndo.rectTransform.anchoredPosition.y + moveAmount, 0.5f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);

        iconAdd.rectTransform.DOAnchorPosY(iconAdd.rectTransform.anchoredPosition.y + moveAmount, 0.5f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);

        iconShuffle.rectTransform.DOAnchorPosY(iconShuffle.rectTransform.anchoredPosition.y + moveAmount, 0.5f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);

        iconMagnet.rectTransform.DOAnchorPosY(iconMagnet.rectTransform.anchoredPosition.y + moveAmount, 0.5f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }


   private void FlashButtonRed(Button button)
    {
        // Image img = button.GetComponent<Image>();
        // if (img == null) return;

        // Color originalColor = img.color;
        // img.DOColor(Color.red, 0.15f)
        //     .SetLoops(2, LoopType.Yoyo)
        //     .OnComplete(() => img.color = originalColor);
    }

    private void OnClickMagnet()
    {
        if (UserData.coin < boosterDatas[3].price)
        {
            FlashButtonRed(MagnetButton);
            return;
        }

        Debug.Log("Magnet Clicked");
        StartCoroutine(LevelManager.Instance.boosterCtrl.Magnet());
        // UserData.coin -= boosterDatas[3].price;
        // CustomeEventSystem.Instance.ChangeCoinAction(UserData.coin);
        // SaveDataManager.Save();
    }

    private void OnClickShuffle()
    {
        if (UserData.coin < boosterDatas[2].price)
        {
            FlashButtonRed(ShuffleButton);
            return;
        }

        Debug.Log("Shuffle Clicked");
        StartCoroutine(LevelManager.Instance.boosterCtrl.Shuffle(LevelManager.Instance.BoardCtrl.boardAlls));
        UserData.coin -= boosterDatas[2].price;
        CustomeEventSystem.Instance.ChangeCoinAction(UserData.coin);
        SaveDataManager.Save();
    }

    private void OnClickAdd()
    {
        if (UserData.coin < boosterDatas[1].price)
        {
            FlashButtonRed(AddButton);
            return;
        }

        StartCoroutine(LevelManager.Instance.boosterCtrl.Add());
        UserData.coin -= boosterDatas[1].price;
        CustomeEventSystem.Instance.ChangeCoinAction(UserData.coin);
        SaveDataManager.Save();
    }

    private void OnClickUndo()
    {
        if (UserData.coin < boosterDatas[0].price)
        {
            FlashButtonRed(UndoButton);
            return;
        }

        StartCoroutine(LevelManager.Instance.boosterCtrl.Undo());
        UserData.coin -= boosterDatas[0].price;
        CustomeEventSystem.Instance.ChangeCoinAction(UserData.coin);
        SaveDataManager.Save();
    }

    private void OnClickSetting()
    {
        //Audio sound
        AudioManager.Instance.PlayOneShot("BLJ_Game_Blockies_Click_01", 1f);
        UIManager.Instance.ShowPopup<PopupSettings>(null);
    }


    //Round
    public void ChangeRound(int round)
    {
        if (round == 1)
        {
            Round_2.sprite = IconRoundDo;
        }

        if (round == 2)
        {
            Round_3.sprite = IconRoundDo;
        }
        listBoosterConfigs[0].Inactive();
        listBoosterConfigs[1].Inactive();
        LevelManager.Instance.BoardCtrl.itemClickCtrl.isStart = false;
    }

    public void ChangeTextCoin(int coin)
    {
        textCoin.text = coin.ToString();
    }
}


