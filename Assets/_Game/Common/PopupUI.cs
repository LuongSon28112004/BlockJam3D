using System;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
enum AnimShowPopUp
{
    None,
    MoveMent,
    ScalePunch
}
public abstract class PopupUI : MonoBehaviour
{
    protected UIManager uiManager;
    public static event Action<PopupUI> OnDestroyPopup; 
    public Action OnHide;
    public Action OnHideInPlace;
    public static event Action<PopupUI> OnShow;
    public bool isCache = false;
    [SerializeField] private Button ButtonClose;
    [SerializeField] private AnimShowPopUp animType;
    [SerializeField] protected RectTransform mainPopUp;

    public bool isShowing { get; protected set; }

    private bool isSetup = false;
    public void Setup()
    {
        if(isSetup) return; 
        isSetup = true;
        Initialize(UIManager.Instance);
    }
    public virtual void Initialize(UIManager manager)
    {
        this.uiManager = manager;
        // gameObject.SetActive(false);
        isShowing = false;
        if (ButtonClose != null)
        {
            ButtonClose.onClick.AddListener(OnClickClose);
        }
    }
    public virtual void Show(Action onClose)
    {
        Setup();
        OnHide = onClose;
        isShowing = true;
        
        if (mainPopUp)
        {
            switch (animType)
            {
                case AnimShowPopUp.MoveMent:
                    mainPopUp.anchoredPosition = new Vector2(-2000, mainPopUp.anchoredPosition.y);
                    mainPopUp.DOAnchorPos(new Vector2(0, mainPopUp.anchoredPosition.y), 0.3f).SetEase(Ease.Linear).SetId(this);
                    break;
                case AnimShowPopUp.ScalePunch:
                    mainPopUp.localScale = Vector3.zero;
                    mainPopUp.DOScale(1.1f, 0.3f).OnComplete(() =>
                    {
                        mainPopUp.DOScale(1, 0.1f).SetId(this);
                    }).SetId(this);
                    break;
            }
        }
        gameObject.SetActive(true);
        OnShow?.Invoke(this);
    }
    public void SetActionOnHideInPlace(Action onHide)
    {
        OnHideInPlace = onHide;
    }
    public virtual void Hide()
    {
        isShowing = false;
        if (gameObject != null)
        {
            gameObject.SetActive(false);
        }
        OnHide?.Invoke();
        OnHide = null;
        OnHideInPlace?.Invoke();
        OnHideInPlace = null;
        if (!isCache)
        {
            OnDestroyPopup?.Invoke(this);
            OnPopupDestroyed();
        }
    }
    public virtual void OnClickClose()
    {
        Hide();
    }
    private void OnDestroy()
    {
        DOTween.Kill(this);
    }
    protected virtual void OnPopupDestroyed()
    {

    }
    public virtual void PreloadAssets(){}
    public virtual void ReleasedAssets(){}
}