using System;
using DG.Tweening;
using Unity.VisualScripting;
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

    [SerializeField] private Image iconUndo;
    [SerializeField] private Image iconAdd;
    [SerializeField] private Image iconShuffle;
    [SerializeField] private Image iconMagnet;


    private void OnEnable()
    {
        UndoButton.onClick.AddListener(OnClickUndo);
        AddButton.onClick.AddListener(OnClickAdd);
        ShuffleButton.onClick.AddListener(OnClickShuffle);
        MagnetButton.onClick.AddListener(OnClickMagnet);
        SettingButton.onClick.AddListener(OnClickSetting);
        CustomeEventSystem.Instance.ChangeRoundAction += ChangeRound;
    }

    private void OnDisable()
    {
        UndoButton.onClick.RemoveListener(OnClickUndo);
        AddButton.onClick.RemoveListener(OnClickAdd);
        ShuffleButton.onClick.RemoveListener(OnClickShuffle);
        MagnetButton.onClick.RemoveListener(OnClickMagnet);
        CustomeEventSystem.Instance.ChangeRoundAction -= ChangeRound;
    }


    void Start()
    {
        addAnimationIcon();
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


    private void OnClickMagnet()
    {
        Debug.Log("Magnet Clicked");
    }

    private void OnClickShuffle()
    {
        Debug.Log("Shuffle Clicked");
    }

    private void OnClickAdd()
    {
        StartCoroutine(LevelManager.Instance.boosterCtrl.Add());
    }

    private void OnClickUndo()
    {
        StartCoroutine(LevelManager.Instance.boosterCtrl.Undo());
    }

    private void OnClickSetting()
    {
        UIManager.Instance.ShowPopup<PopupSettings>(null);
    }


    //Round
    public void ChangeRound(int round)
    {
        if (round == 1)
        {
            Round_2.sprite = IconRoundDo;
        }
        
        if(round == 2)
        {
            Round_3.sprite = IconRoundDo;
        }
    }



}
