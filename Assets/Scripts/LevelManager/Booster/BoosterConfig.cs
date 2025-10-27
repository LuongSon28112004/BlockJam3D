using UnityEngine;
using UnityEngine.UI;

public class BoosterConfig : MonoBehaviour
{
    [SerializeField] Button buttonBooster;
    [SerializeField] Image Panel;
    [SerializeField] Image IconBooster;
    [SerializeField] Image PanelPrice;
    [SerializeField] Image IconCoin;
    [SerializeField] private Color colorInactive;
    [SerializeField] private Color colorActive;


    public void Active()
    {
        buttonBooster.interactable = true;
        Panel.color = colorActive;
        IconBooster.color = colorActive;
        PanelPrice.color = colorActive;
        IconCoin.color = colorActive;
    }

    public void Inactive()
    {
        buttonBooster.interactable = false;
        Panel.color = colorInactive;
        IconBooster.color = colorInactive;
        PanelPrice.color = colorInactive;
        IconCoin.color = colorInactive;
    }
}
