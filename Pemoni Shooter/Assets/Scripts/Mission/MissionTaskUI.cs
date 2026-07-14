using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MissionTaskUI : MonoBehaviour
{
    [SerializeField] private int missionIndex;

    [Header("UI")]
    [SerializeField] private Slider progressSlider;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Image iconImage;

    [Header("Claim")]
    [SerializeField] private GameObject indexNumberRoot;   // ô vàng hiện số 1/2/3/4
    [SerializeField] private Button claimButton;
    [SerializeField] private Animator highlightAnimator;   // Animator trên Task (optional)

    private void OnEnable()
    {
        if (MissionManager.Instance != null)
            MissionManager.Instance.OnStateChanged += Refresh;

        if (claimButton != null)
        {
            claimButton.onClick.RemoveAllListeners();
            claimButton.onClick.AddListener(OnClaimClicked);
        }

        Refresh();
    }

    private void OnDisable()
    {
        if (MissionManager.Instance != null)
            MissionManager.Instance.OnStateChanged -= Refresh;
    }

    public void Refresh()
    {
        var mgr = MissionManager.Instance;
        if (mgr == null || missionIndex >= mgr.MissionCount) return;

        var cfg = mgr.GetConfig(missionIndex);

        if (descriptionText != null)
            descriptionText.text = cfg.description;

        if (iconImage != null && cfg.icon != null)
            iconImage.sprite = cfg.icon;

        if (progressSlider != null)
            progressSlider.value = mgr.GetProgress01(missionIndex);

        if (progressText != null)
            progressText.text = mgr.GetProgressText(missionIndex);

        bool canClaim = mgr.CanClaim(missionIndex);

        if (indexNumberRoot != null)
            indexNumberRoot.SetActive(!canClaim);

        if (claimButton != null)
            claimButton.gameObject.SetActive(canClaim);

        if (highlightAnimator != null)
            highlightAnimator.enabled = canClaim;
    }

    private void OnClaimClicked()
    {
        MissionManager.Instance?.TryClaim(missionIndex);
    }
}