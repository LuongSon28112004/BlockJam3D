using System;
using UnityEngine;

public class LeaderBoardPlayerController : MonoBehaviour
{
    [Header("Refs")]
    private LeaderBoardManager leaderBoardManager;
    [SerializeField] private LeaderBoardUserInforPlayer userInforPlayer;
    [SerializeField] private LeaderBoardUserInfor userInfor;
    [SerializeField] private GameObject contentPlayer;
    [SerializeField] private GameObject LoadingPanel;

    public void LoadListPlayer()
    {
        LoadingPanel.SetActive(true);
        UserDataFirebaseManager.Instance.GetAllUsers(users =>
        {
            LoadingPanel.SetActive(false);
            if (users == null) return;

            // Clear list cũ
            foreach (Transform child in contentPlayer.transform)
            {
                Destroy(child.gameObject);
            }

            // Sort theo Level (giảm dần)
            users.Sort((a, b) =>
            {
                int levelA = a.ContainsKey("Level") ? Convert.ToInt32(a["Level"]) : 1;
                int levelB = b.ContainsKey("Level") ? Convert.ToInt32(b["Level"]) : 1;
                return levelB.CompareTo(levelA);
            });

            // Tạo UI + ranking
            for (int i = 0; i < users.Count; i++)
            {
                var user = users[i];

                GameObject item;
                //item.SetActive(true);

                string name = user.ContainsKey("Name") ? user["Name"].ToString() : "Unknown";
                int level = user.ContainsKey("Level") ? Convert.ToInt32(user["Level"]) : 1;

                int ranking = i + 1;


                //check my user
                string MycurrentUserId = PlayerPrefs.GetString("PlayerID");
                if (MycurrentUserId == null) MycurrentUserId = "";
                if (user.ContainsKey("Id") && user["Id"].ToString() == MycurrentUserId)
                {
                    item = Instantiate(userInforPlayer.gameObject, contentPlayer.transform);
                    LeaderBoardUserInforPlayer ui = item.GetComponent<LeaderBoardUserInforPlayer>();
                    ui.SetData(ranking, name, level);
                }
                else
                {
                    item = Instantiate(userInfor.gameObject, contentPlayer.transform);
                    LeaderBoardUserInfor ui = item.GetComponent<LeaderBoardUserInfor>();
                    ui.SetData(ranking, name, level);
                }
                item.SetActive(true);
            }
        });
    }

    public void ClearContent()
    {
        foreach (Transform child in contentPlayer.transform)
        {
            Destroy(child.gameObject);
        }
    }
}

