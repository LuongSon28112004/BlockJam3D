using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class PopupSendGilf : PopupUI
{
    private string idUser;

    [Header("Ref Button")]
    [SerializeField] private Button btnFreeze;
    [SerializeField] private Transform Tick_Freeze;
    [SerializeField] private Button btnHammer;
    [SerializeField] private Transform Tick_Hammer;
    [SerializeField] private Button btnBomb;
    [SerializeField] private Transform Tick_Bomb;
    [SerializeField] private Button btnHeart;
    [SerializeField] private Transform Tick_Heart;
    [SerializeField] private Button btnSend;

    private Button currentSelected;

    private Dictionary<Button, Transform> tickMap;
    private Dictionary<Button, string> giftMap; // 👈 map button -> loại gift

    private void Awake()
    {
        tickMap = new Dictionary<Button, Transform>()
        {
            { btnFreeze, Tick_Freeze },
            { btnHammer, Tick_Hammer },
            { btnBomb, Tick_Bomb },
            { btnHeart, Tick_Heart }
        };

        // 👇 map loại quà
        giftMap = new Dictionary<Button, string>()
        {
            { btnFreeze, "Undo" },
            { btnHammer, "Add" },
            { btnBomb, "Shuffle" },
            { btnHeart, "Heart" }
        };

        btnFreeze.onClick.AddListener(() => SelectGift(btnFreeze));
        btnHammer.onClick.AddListener(() => SelectGift(btnHammer));
        btnBomb.onClick.AddListener(() => SelectGift(btnBomb));
        btnHeart.onClick.AddListener(() => SelectGift(btnHeart));

        btnSend.onClick.AddListener(OnClickSend); // 👈 nút gửi

        foreach (var tick in tickMap.Values)
        {
            tick.gameObject.SetActive(false);
        }
    }

    public void SetIdUser(string id)
    {
        this.idUser = id;
    }

    private void SelectGift(Button selectedBtn)
    {
        if (currentSelected != null)
        {
            SetNormal(currentSelected);
        }

        currentSelected = selectedBtn;
        SetSelected(currentSelected);
    }

    private void SetSelected(Button btn)
    {
        if (tickMap.ContainsKey(btn))
        {
            tickMap[btn].gameObject.SetActive(true);
        }
    }

    private void SetNormal(Button btn)
    {
        if (tickMap.ContainsKey(btn))
        {
            tickMap[btn].gameObject.SetActive(false);
        }
    }

    // =====================================================
    // 🚀 SEND LOGIC
    // =====================================================
    private void OnClickSend()
    {
        if (currentSelected == null)
        {
            UIManager.Instance.NotifyContent(Loc.Get("gift_no_selection"));
            return;
        }

        if (string.IsNullOrEmpty(idUser))
        {
            UIManager.Instance.NotifyContent(Loc.Get("gift_no_recipient"));
            return;
        }

        string giftName = giftMap[currentSelected];

        string myId = UserDataFirebaseManager.Instance.CurrentUserId;

        UserDataFirebaseManager.Instance.SendBooster(
            myId,
            idUser,
            giftName,
            1, // 👈 mặc định gửi 1
            success =>
            {
                if (success)
                {
                    UIManager.Instance.NotifyContent(Loc.Get("gift_sent_fmt", giftName));
                    Hide();
                }
                else
                {
                    UIManager.Instance.NotifyContent(Loc.Get("gift_daily_limit_fmt", 3));
                }
            }
        );
    }
}