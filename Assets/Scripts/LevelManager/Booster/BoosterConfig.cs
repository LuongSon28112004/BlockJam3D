using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class BoosterConfig : MonoBehaviour
{
    [Header("Booster Component")]
    [SerializeField] Button buttonBooster;
    [SerializeField] Image Panel;
    [SerializeField] Image IconBooster;

    // Lock
    [SerializeField] GameObject Locked;
    [SerializeField] bool isLocked;

    // price
    [Header("Price")]
    [SerializeField] GameObject Price;
    [SerializeField] Image PanelPrice;
    [SerializeField] Image IconCoin;
    // Counter
    [Header("Counter")]
    [SerializeField] GameObject Counter;
    [SerializeField] Image PanelCounter;
    [SerializeField] TextMeshProUGUI textCounter;
    // color
    [Header("Color")]
    [SerializeField] private Color colorInactive;
    [SerializeField] private Color colorActive;

    public void SetTextCounter(int count)
    {
        textCounter.text = count.ToString();
    }

    public void SetCounter(int count)
    {
        Price.SetActive(false);
        Counter.SetActive(true);
        textCounter.text = count.ToString();
    }

    public void SetPrice()
    {
        Counter.SetActive(false);
        Price.SetActive(true);
    }


    public void Active()
    {
        if (!isLocked) buttonBooster.interactable = true;
        Panel.color = colorActive;
        IconBooster.color = colorActive;
        PanelPrice.color = colorActive;
        IconCoin.color = colorActive;
        PanelCounter.color = colorActive;
    }

    public void Inactive()
    {
        buttonBooster.interactable = false;
        Panel.color = colorInactive;
        IconBooster.color = colorInactive;
        PanelPrice.color = colorInactive;
        IconCoin.color = colorInactive;
        PanelCounter.color = colorInactive;
    }

    public void LockedBooster()
    {
        isLocked = true;
        Panel.gameObject.SetActive(false);
        Locked.SetActive(true);
        buttonBooster.interactable = false;
    }

    public void Unlock()
    {
        isLocked = false;
        Locked.SetActive(false);
        Panel.gameObject.SetActive(true);
        buttonBooster.interactable = true;
    }
}
