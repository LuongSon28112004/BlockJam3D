using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class PopupLoseGame : PopupUI
{
    [SerializeField] private Button buttonTryAgain;
    [SerializeField] private Button buttonClose;
    [SerializeField] private GameObject HeartBeat;
    [SerializeField] private float beatScale = 1.2f;
    [SerializeField] private float beatDuration = 0.3f;
    [SerializeField] private Ease beatEase = Ease.InOutSine;

    private Tween heartbeatTween;

    private void Awake()
    {
        buttonTryAgain.onClick.AddListener(OnTryAgainClicked);
        buttonClose.onClick.AddListener(OnCloseClicked);

    }

    public override void Show(Action onClose)
    {
        base.Show(onClose);
        StartHeartBeat();
        HeartManager.Instance.TryConsume(1);
    }

    private void StartHeartBeat()
    {
        if (heartbeatTween != null && heartbeatTween.IsActive())
            heartbeatTween.Kill();

        Vector3 originalScale = HeartBeat.transform.localScale;

        heartbeatTween = HeartBeat.transform.DOScale(originalScale * beatScale, beatDuration)
            .SetEase(beatEase)
            .SetLoops(-1, LoopType.Yoyo)
            .SetAutoKill(false)
            .SetUpdate(true); // chạy kể cả khi pause game
    }

    private void OnTryAgainClicked()
    {
        AudioManager.Instance.PlayOneShot("BLJ_UI_Button_Default_01", 1f);
        if (HeartManager.Instance.Hearts <= 0)
        {
            Hide();
            if (Resources.Load<PopupAddHeart>("UI/Popups/PopupAddHeart") != null)
                UIManager.Instance.ShowPopup<PopupAddHeart>(null);
            else
                UIManager.Instance.NotifyContent(Loc.Get("no_hearts_wait_regen"));
            return;
        }
        Hide();
        GameManager.Instance.StartGame();
    }

    private void OnCloseClicked()
    {
        Hide();
        AudioManager.Instance.PlayOneShot("BLJ_UI_Button_Default_01", 1f);
        GameManager.Instance.BackToMenu();
    }
}
