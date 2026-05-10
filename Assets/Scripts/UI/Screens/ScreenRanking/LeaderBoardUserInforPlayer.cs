using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LeaderBoardUserInforPlayer : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] private Text txtRanking;
    [SerializeField] private Text txtName;
    [SerializeField] private Text txtLevel;
    [SerializeField] private List<Transform> ListRankingIcons;


    public void SetData(int ranking, string name, int level)
    {
        SetRankingIcon(ranking);
        txtName.text = name;
        txtLevel.text = level.ToString();
    }

    private void SetRankingIcon(int ranking)
    {
        if (ranking > 3)
        {
            txtRanking.text = ranking.ToString();
            return;
        }
        txtRanking.text = "";
        for (int i = 0; i < ListRankingIcons.Count; i++)
        {
            ListRankingIcons[i].gameObject.SetActive(i == ranking - 1);
        }
    }
}
