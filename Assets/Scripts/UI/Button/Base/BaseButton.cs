using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.UI;

public abstract class BaseButton : MonoBehaviour
{
    [SerializeField] Button button;

    void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();
        if (button != null)
            button.onClick.AddListener(OnClick);
    }

    protected abstract void OnClick();
}
