using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MissionTaskRow : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private Image progressBarFill;
    [SerializeField] private Button claimButton;
    [SerializeField] private TextMeshProUGUI claimButtonLabel;
    [SerializeField] private GameObject claimedOverlay;
    [SerializeField] private TextMeshProUGUI rewardText;

    private string currentMissionId;

    private void Awake()
    {
        if (claimButton != null)
        {
            claimButton.onClick.RemoveAllListeners();
            claimButton.onClick.AddListener(OnClickClaim);
        }
    }

    public void Bind(DailyMissionData data, MissionTaskProgress prog)
    {
        currentMissionId = prog != null ? prog.missionId : null;

        if (data == null || prog == null)
        {
            if (titleText != null) titleText.text = "?";
            if (progressText != null) progressText.text = "0/0";
            if (claimButton != null) claimButton.interactable = false;
            if (claimedOverlay != null) claimedOverlay.SetActive(false);
            if (progressBarFill != null) progressBarFill.fillAmount = 0f;
            return;
        }

        if (iconImage != null)
        {
            if (data.icon != null)
            {
                iconImage.sprite = data.icon;
                iconImage.enabled = true;
            }
            // Nếu không có icon, để nguyên ảnh placeholder.
        }

        if (titleText != null)
        {
            titleText.text = Loc.Get(data.titleKey, data.targetCount);
        }

        // Cho phép mock qua mockStatus trong SO khi test.
        int effective = data.GetEffectiveProgress(prog.currentCount);
        int cur = Mathf.Min(effective, data.targetCount);
        if (progressText != null)
        {
            progressText.text = Loc.Get("daily_mission_progress_fmt", cur, data.targetCount);
        }
        if (progressBarFill != null)
        {
            progressBarFill.fillAmount = data.targetCount > 0 ? (float)cur / data.targetCount : 0f;
        }
        if (rewardText != null)
        {
            rewardText.text = data.coinReward.ToString();
        }

        bool isDone = effective >= data.targetCount;
        bool isClaimed = data.GetEffectiveClaimed(prog.claimed);

        if (claimButton != null)
        {
            claimButton.interactable = isDone && !isClaimed;
        }
        if (claimButtonLabel != null)
        {
            claimButtonLabel.text = isClaimed ? Loc.Get("daily_mission_claimed") : Loc.Get("daily_mission_claim");
        }
        if (claimedOverlay != null)
        {
            claimedOverlay.SetActive(isClaimed);
        }
    }

    private void OnClickClaim()
    {
        if (string.IsNullOrEmpty(currentMissionId)) return;
        if (DailyMissionManager.Instance == null) return;
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayOneShot("BLJ_UI_Button_Default_01", 1f);
        }
        DailyMissionManager.Instance.ClaimReward(currentMissionId);
    }
}
