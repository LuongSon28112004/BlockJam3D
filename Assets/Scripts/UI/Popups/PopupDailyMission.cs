using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class PopupDailyMission : PopupUI
{
    [Header("Daily Mission UI")]
    [SerializeField] private TextMeshProUGUI textTitle;
    [SerializeField] private TextMeshProUGUI textResetIn;
    [SerializeField] private MissionTaskRow[] rows;

    private Coroutine countdownCo;

    public override void Show(Action onClose)
    {
        base.Show(onClose);
        if (DailyMissionManager.Instance != null)
        {
            DailyMissionManager.Instance.OnMissionProgressChanged += Refresh;
        }
        Loc.OnLanguageChanged += Refresh;
        Refresh();
        if (countdownCo != null) StopCoroutine(countdownCo);
        countdownCo = StartCoroutine(CountdownTick());
    }

    public override void Hide()
    {
        if (DailyMissionManager.Instance != null)
        {
            DailyMissionManager.Instance.OnMissionProgressChanged -= Refresh;
        }
        Loc.OnLanguageChanged -= Refresh;
        if (countdownCo != null)
        {
            StopCoroutine(countdownCo);
            countdownCo = null;
        }
        base.Hide();
    }

    private void Refresh()
    {
        if (textTitle != null) textTitle.text = Loc.Get("daily_mission_title");
        RefreshCountdown();

        var mgr = DailyMissionManager.Instance;
        var progress = UserData.dailyMissionProgress;
        if (rows == null) return;

        // Có thể tasks null/empty nếu chưa reset
        if (mgr != null) mgr.CheckAndResetIfNeeded();

        var tasks = progress != null ? progress.tasks : null;
        for (int i = 0; i < rows.Length; i++)
        {
            if (rows[i] == null) continue;
            if (tasks != null && i < tasks.Count)
            {
                var task = tasks[i];
                var data = mgr != null ? mgr.GetMissionData(task.missionId) : null;
                rows[i].gameObject.SetActive(true);
                rows[i].Bind(data, task);
            }
            else
            {
                rows[i].gameObject.SetActive(false);
            }
        }
    }

    private IEnumerator CountdownTick()
    {
        var wait = new WaitForSeconds(1f);
        while (true)
        {
            RefreshCountdown();
            yield return wait;
        }
    }

    private void RefreshCountdown()
    {
        if (textResetIn == null) return;
        var mgr = DailyMissionManager.Instance;
        if (mgr == null)
        {
            textResetIn.text = string.Empty;
            return;
        }
        // Nếu qua mốc reset trong lúc popup đang mở → trigger reset.
        if (mgr.TimeUntilNextReset() <= TimeSpan.Zero)
        {
            mgr.CheckAndResetIfNeeded();
        }
        var ts = mgr.TimeUntilNextReset();
        int hours = ts.Hours + ts.Days * 24;
        int minutes = ts.Minutes;
        textResetIn.text = Loc.Get("daily_mission_resets_in_fmt", hours, minutes);
    }
}
