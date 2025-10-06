using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class NotifyPanel : MonoBehaviour
{
    [SerializeField] RectTransform rect;
    [SerializeField] Text text;
    [SerializeField] Outline outline;
    [SerializeField] Shadow shadow;

    private void Start()
    {
        rect.gameObject.SetActive(false);
    }

    public void ShowNotify(string content, string key = "", float number = 0)
    {
        DOTween.Kill(this);

        if (string.IsNullOrEmpty(key))
        {
            text.text = content;
        }
        else
        {
           // text.SetText(key, stateFormat: mygame.sdk.FormatText.F_String, obFormat: number);
        }

        anim();
    }

    public void ShowNotify(string content, string key = "", string objFormat = "")
    {
        DOTween.Kill(this);

        if (string.IsNullOrEmpty(key))
        {
            text.text = content;
        }
        else
        {
            //text.SetText(key, stateFormat: mygame.sdk.FormatText.F_String, obFormat: objFormat);
        }

        anim();
    }

    private void anim()
    {
        rect.gameObject.SetActive(true);
        var color = text.color;
        color.a = 0;
        text.color = color;
        rect.anchoredPosition = new Vector2(0, -200f);
        this.DOKill();
        
        var sq = DOTween.Sequence();
        sq.SetId(this);
        text.transform.localScale = Vector3.one * 0.1f;
        sq.Insert(0, text.DOFade(1, 0.1f).OnUpdate(() =>
        {
            var shadowColor = shadow.effectColor;
            shadowColor.a = text.color.a;
            var outlineColor = outline.effectColor;
            outlineColor.a = text.color.a;
            shadow.effectColor = shadowColor;
            outline.effectColor = outlineColor;
        }).SetEase(Ease.OutQuad)).Join(text.transform.DOScale(Vector3.one, 0.3f));
        sq.Insert(0, rect.DOAnchorPosY(-100, 2f).SetEase(Ease.OutQuad));
        sq.AppendInterval(text.text.Length < 30 ? 0.6f : 1f);
        sq.Append(text.DOFade(0, 0.3f).OnUpdate(() =>
        {
            var shadowColor = shadow.effectColor;
            shadowColor.a = text.color.a / 3;
            var outlineColor = outline.effectColor;
            outlineColor.a = text.color.a / 3;
            shadow.effectColor = shadowColor;
            outline.effectColor = outlineColor;
        }));
        sq.OnComplete(() => { rect.gameObject.SetActive(false); });
    }

    private void OnDisable()
    {
        DOTween.Kill(this);
    }
}