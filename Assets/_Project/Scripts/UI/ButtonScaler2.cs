using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
using UnityEngine.Events;

public class ButtonScaler2 : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
{
    public Vector3 startScale = Vector3.one;
    public Vector3 endScale = new Vector3(0.95f, 0.95f, 0.95f);
    [SerializeField] Transform targetTF;
    [SerializeField] protected UnityEvent eventOnPointDown;
    [SerializeField] protected UnityEvent eventOnPointUp;
    [SerializeField] private bool isActiveSound = true;
    [SerializeField] private bool ignoreVib = false;

    private bool isPointerDown = false;
    private bool isPointerInside = false;

    private void Awake()
    {
        if (targetTF == null)
        {
            targetTF = transform;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isPointerDown = true;
        targetTF.DOScale(
    new Vector3(endScale.x, endScale.y, 1f),
    0.1f
).SetEase(Ease.OutQuad).SetUpdate(true).SetId(this);
        eventOnPointDown?.Invoke();
        isPointerInside = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!isPointerDown) return;
        isPointerDown = false;
        targetTF.DOScale(
    new Vector3(startScale.x, startScale.y, 1f),
    0.1f
).SetEase(Ease.OutQuad).SetUpdate(true).SetId(this);
        eventOnPointUp?.Invoke();
        if (eventData.dragging)
        {
            return;
        }
        if (!isPointerInside)
        {
            return;
        }
        if (isActiveSound)
        {
            AudioManager.Instance.PlayOneShot("ClickButton");
        }
        if (ignoreVib)
        {
            AudioManager.Instance.PlayVibrate();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isPointerInside = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerInside = false;
    }

    private void OnDisable()
    {
        targetTF.localScale = startScale;
        this.DOKill();
        isPointerDown = false;
        isPointerInside = false;
    }
}
