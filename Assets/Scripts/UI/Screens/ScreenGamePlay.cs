using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
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

    private Tween undoLoopTween;
    private Tween addLoopTween;
    private Tween shuffleLoopTween;
    private Tween magnetLoopTween;



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
    }

    private void OnDisable()
    {
        UndoButton.onClick.RemoveListener(OnClickUndo);
        AddButton.onClick.RemoveListener(OnClickAdd);
        ShuffleButton.onClick.RemoveListener(OnClickShuffle);
        MagnetButton.onClick.RemoveListener(OnClickMagnet);
        SettingButton.onClick.RemoveListener(OnClickSetting);
        CustomeEventSystem.Instance.ChangeRoundAction -= ChangeRound;
        CustomeEventSystem.Instance.ChangeCoinAction -= ChangeTextCoin;
        CustomeEventSystem.Instance.ActiveBoosterAction -= ActiveBooster;
    }

    public void ActiveBooster(List<int> lists)
    {
        for (int i = 0; i < lists.Count; i++)
        {
            if (lists[i] != -1)
            {
                listBoosterConfigs[i].Active();
            }
            else listBoosterConfigs[i].Inactive();
        }
    }

    void Start()
    {
        AddAnimationIcons();
        SetCounterOrPrice();
        InitCoinText();
        InitPriceBooster();
        LoadTextLevel();
        CheckLocked();
    }

    private void CheckLocked()
    {
        if (GameManager.Instance.Level == 1)
        {
            foreach (var i in listBoosterConfigs)
            {
                i.LockedBooster();
            }
        }
    }

    private void SetCounterOrPrice()
    {
        foreach (var boosterCounter in UserData.listBoosterCounters)
        {
            if (boosterCounter.count > 0)
            {
                if (boosterCounter.name == "Undo")
                {
                    listBoosterConfigs[0].SetCounter(boosterCounter.count);
                }
                else if (boosterCounter.name == "Add")
                {
                    listBoosterConfigs[1].SetCounter(boosterCounter.count);
                }
                else if (boosterCounter.name == "Shuffle")
                {
                    listBoosterConfigs[2].SetCounter(boosterCounter.count);
                }
                else
                {
                    listBoosterConfigs[3].SetCounter(boosterCounter.count);
                }

            }
        }
    }

    private void LoadTextLevel()
    {
        textLevel.text = "Level " + UserData.level.ToString();
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

    private void AddAnimationIcons()
    {
        float moveAmount = 15f;
        float duration = 0.5f;

        // Lưu tween để dùng lại sau
        undoLoopTween = iconUndo.rectTransform.DOAnchorPosY(iconUndo.rectTransform.anchoredPosition.y + moveAmount, duration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .Pause(); // Dừng trước, để tất cả khởi động cùng lúc

        addLoopTween = iconAdd.rectTransform.DOAnchorPosY(iconAdd.rectTransform.anchoredPosition.y + moveAmount, duration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .Pause();

        shuffleLoopTween = iconShuffle.rectTransform.DOAnchorPosY(iconShuffle.rectTransform.anchoredPosition.y + moveAmount, duration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .Pause();

        magnetLoopTween = iconMagnet.rectTransform.DOAnchorPosY(iconMagnet.rectTransform.anchoredPosition.y + moveAmount, duration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .Pause();

        // Cho tất cả bắt đầu cùng lúc (đồng pha)
        undoLoopTween.Play();
        addLoopTween.Play();
        shuffleLoopTween.Play();
        magnetLoopTween.Play();
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

    public void OnClickMagnet()
    {
        StartCoroutine(ClickMagnet());
    }

    private IEnumerator ClickMagnet()
    {
        if (LevelManager.Instance.boosterCtrl.IsBusy || LevelManager.Instance.BoardCtrl.BoardCells.Count == 0)
        {
            if (UserData.coin < boosterDatas[3].price && UserData.listBoosterCounters[3].count == 0)
            {
                FlashButtonRed(MagnetButton);
            }
            yield break;
        }
        PlayMagnetEffect();
        Debug.Log("Magnet Clicked");
        AudioManager.Instance.PlayOneShot("BLJ_Boosters_Magnet_01", 1f);
        StartCoroutine(LevelManager.Instance.boosterCtrl.BoosterMagnet.Magnet());
        if (UserData.listBoosterCounters[3].count <= 0)
        {
            UserData.coin -= boosterDatas[3].price;
            CustomeEventSystem.Instance.ChangeCoin(UserData.coin);
        }
        else
        {
            UserData.listBoosterCounters[3].count -= 1;
            if (UserData.listBoosterCounters[3].count <= 0)
            {
                listBoosterConfigs[3].SetPrice();
            }
            listBoosterConfigs[3].SetTextCounter(UserData.listBoosterCounters[3].count);
        }
        SaveDataManager.Save();
    }

    private void PlayMagnetEffect()
    {
        // Hủy tween cũ để tránh chồng hiệu ứng
        iconMagnet.rectTransform.DOKill();
        iconMagnet.rectTransform.localScale = Vector3.one;
        iconMagnet.rectTransform.rotation = Quaternion.identity;

        Image magnetImage = iconMagnet.GetComponent<Image>();
        Color originalColor = magnetImage.color;

        // Lưu vị trí ban đầu
        Vector2 originalPos = iconMagnet.rectTransform.anchoredPosition;

        // Tạo sequence
        DG.Tweening.Sequence magnetSeq = DOTween.Sequence();

        // Nhảy lên cao hơn (tầm 40–50px)
        magnetSeq.Append(iconMagnet.rectTransform.DOAnchorPosY(originalPos.y + 45f, 0.25f).SetEase(Ease.OutQuad));

        // Xoay nhẹ về hướng 11h (nghiêng ngược chiều kim đồng hồ)
        magnetSeq.Join(iconMagnet.rectTransform.DORotate(new Vector3(0, 0, 35f), 0.25f).SetEase(Ease.OutBack));
        // 330° tương đương -30°, tức là xoay về phía "11h"

        // Phóng to + sáng mạnh trong khi ở tư thế nghiêng
        magnetSeq.Join(iconMagnet.rectTransform.DOScale(1.4f, 0.25f).SetEase(Ease.OutBack));
        magnetSeq.Join(magnetImage.DOColor(Color.cyan, 0.25f));

        // Thực hiện "hiệu ứng hút" — rung và giật nhẹ (giống phát lực)
        magnetSeq.Append(iconMagnet.rectTransform.DOShakePosition(0.5f, 15f, 10, 100, false, true));

        // Quay ngược lại tư thế thẳng + thu nhỏ + hạ xuống
        magnetSeq.Append(iconMagnet.rectTransform.DORotate(Vector3.zero, 0.3f).SetEase(Ease.InOutBack));
        magnetSeq.Join(iconMagnet.rectTransform.DOScale(1f, 0.3f).SetEase(Ease.InBack));
        magnetSeq.Join(iconMagnet.rectTransform.DOAnchorPosY(originalPos.y, 0.3f).SetEase(Ease.InBack));
        magnetSeq.Join(magnetImage.DOColor(originalColor, 0.3f));

        magnetSeq.Play();
    }
    private void OnClickShuffle()
    {
        if (LevelManager.Instance.boosterCtrl.IsBusy || LevelManager.Instance.BoardCtrl.BoardCells.Count == 0)
        {
            if (UserData.coin < boosterDatas[2].price && UserData.listBoosterCounters[2].count == 0)
            {
                FlashButtonRed(MagnetButton);
            }
            return;
        }

        Debug.Log("Shuffle Clicked");
        AudioManager.Instance.PlayOneShot("BLJ_Boosters_Shuffle_01", 1f);
        StartCoroutine(LevelManager.Instance.boosterCtrl.BoosterShuffle.Shuffle(LevelManager.Instance.BoardCtrl.boardAlls));
        if (UserData.listBoosterCounters[2].count <= 0)
        {
            UserData.coin -= boosterDatas[2].price;
            CustomeEventSystem.Instance.ChangeCoin(UserData.coin);
        }
        else
        {
            UserData.listBoosterCounters[2].count -= 1;
            if (UserData.listBoosterCounters[2].count <= 0)
            {
                listBoosterConfigs[2].SetPrice();
            }
            listBoosterConfigs[2].SetTextCounter(UserData.listBoosterCounters[2].count);
        }
        SaveDataManager.Save();
    }

    private void OnClickAdd()
    {
        if (LevelManager.Instance.boosterCtrl.IsBusy || LevelManager.Instance.BoardCtrl.BoardCells.Count == 0)
        {
            if (UserData.coin < boosterDatas[1].price && UserData.listBoosterCounters[1].count == 0)
            {
                FlashButtonRed(MagnetButton);
            }
            return;
        }
        AudioManager.Instance.PlayOneShot("BLJ_Boosters_Continue_01", 1f);
        StartCoroutine(LevelManager.Instance.boosterCtrl.BoosterAdd.Add());
        if (UserData.listBoosterCounters[1].count <= 0)
        {
            UserData.coin -= boosterDatas[1].price;
            CustomeEventSystem.Instance.ChangeCoin(UserData.coin);
        }
        else
        {
            UserData.listBoosterCounters[1].count -= 1;
            if (UserData.listBoosterCounters[1].count <= 0)
            {
                listBoosterConfigs[1].SetPrice();
            }
            listBoosterConfigs[1].SetTextCounter(UserData.listBoosterCounters[1].count);
        }
        SaveDataManager.Save();
    }

    private void OnClickUndo()
    {
        if (LevelManager.Instance.boosterCtrl.IsBusy || LevelManager.Instance.BoardCtrl.BoardCells.Count == 0)
        {
            if (UserData.coin < boosterDatas[0].price && UserData.listBoosterCounters[0].count == 0)
            {
                FlashButtonRed(MagnetButton);
            }
            return;
        }
        PlayUndoEffect();
        //Audio sound
        AudioManager.Instance.PlayOneShot("BLJ_Boosters_Undo_01", 1f);
        StartCoroutine(LevelManager.Instance.boosterCtrl.BoosterUndo.Undo());
        if (UserData.listBoosterCounters[0].count <= 0)
        {
            UserData.coin -= boosterDatas[0].price;
            CustomeEventSystem.Instance.ChangeCoin(UserData.coin);
        }
        else
        {
            UserData.listBoosterCounters[0].count -= 1;
            if (UserData.listBoosterCounters[0].count <= 0)
            {
                listBoosterConfigs[0].SetPrice();
            }
            listBoosterConfigs[0].SetTextCounter(UserData.listBoosterCounters[0].count);
        }
        SaveDataManager.Save();
    }


    private void PlayUndoEffect()
    {
        if (iconUndo == null) return;

        RectTransform rt = iconUndo.rectTransform;

        // Dừng tween nhấp nhô đang chạy
        undoLoopTween.Pause();
        addLoopTween.Pause();
        shuffleLoopTween.Pause();
        magnetLoopTween.Pause();

        Vector2 originalPos = rt.anchoredPosition;
        // rt.DOKill();

        var sc = DOTween.Sequence();

        // Nâng lên
        sc.Append(rt.DOAnchorPosY(originalPos.y + 100f, 0.15f).SetEase(Ease.OutQuad));

        // Xoay trong lúc đang trên cao
        sc.Join(rt.DOLocalRotate(new Vector3(0f, 0f, 360f), 0.3f, RotateMode.FastBeyond360)
            .SetEase(Ease.Linear));

        // Hạ xuống
        sc.Append(rt.DOAnchorPosY(originalPos.y, 0.15f).SetEase(Ease.InQuad));

        // Reset xoay
        sc.OnComplete(() =>
        {
            rt.localRotation = Quaternion.identity;

            // Khi hiệu ứng xong -> bật lại tween loop gốc, đồng bộ pha với các icon khác
            undoLoopTween.Restart();
            addLoopTween.Restart();
            shuffleLoopTween.Restart();
            magnetLoopTween.Restart();
        });
    }




    private void OnClickSetting()
    {
        //Audio sound
        AudioManager.Instance.PlayOneShot("BLJ_UI_Button_Default_01", 1f);
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
        ActiveBooster(new List<int> { 1, 1, 0, 0 });
        LevelManager.Instance.BoardCtrl.itemClickCtrl.isStart = false;
    }

    public void ChangeTextCoin(int coin)
    {
        textCoin.text = coin.ToString();
    }
}


