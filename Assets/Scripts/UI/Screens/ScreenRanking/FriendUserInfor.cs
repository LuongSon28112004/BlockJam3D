using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FriendUserInfor : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] private Text txtRanking;
    [SerializeField] private Text txtName;
    [SerializeField] private Text txtLevel;
    [SerializeField] private List<Transform> ListRankingIcons;
    [SerializeField] private Button btnSendGilf;

    private string userId;

    public void SetData(int rank, string name, int level, string id)
    {
        SetRankingIcon(rank);
        txtName.text = name;
        txtLevel.text = level.ToString();
        this.userId = id;
        string myId = PlayerPrefs.GetString("PlayerID", "-1");
        if (!id.Contains(myId))
        {
            btnSendGilf.onClick.RemoveAllListeners();
            btnSendGilf.onClick.AddListener(SendGilfClick);

        }
        else
        {
            btnSendGilf.onClick.RemoveAllListeners();
            btnSendGilf.gameObject.SetActive(false);
        }
    }

    private void SendGilfClick()
    {
        var UiSendGilf = UIManager.Instance.ShowPopup<PopupSendGilf>(null);
        UiSendGilf.SetIdUser(userId);

    }

    private void SetRankingIcon(int rank)
    {
        if (rank > 3)
        {
            txtRanking.text = rank.ToString();
            return;
        }
        txtRanking.text = "";
        for (int i = 0; i < ListRankingIcons.Count; i++)
        {
            ListRankingIcons[i].gameObject.SetActive(i == rank - 1);
        }
    }
}
