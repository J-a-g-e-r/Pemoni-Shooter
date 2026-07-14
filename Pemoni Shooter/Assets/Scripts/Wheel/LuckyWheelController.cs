using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using RewardSystem;
using System.Collections.Generic;
using TMPro;

public class LuckyWheelController : MonoBehaviour
{
    [Serializable]
    public class WheelSlot
    {
        public RewardType type;
        //public string rewardId;      // e.g. "coins", "x2_multiplier", "gem"
        public int amount;           // e.g. 250, 2, 1
        public Sprite icon;
        [Tooltip("Relative weight for random selection. Higher = more likely.")]
        public float weight = 1f;

        //Neeus la rýõng
        public Sprite closedChestIcon;
        public List<RewardEntry> chestContents = new();

    }

    [Header("Slots (clockwise order, starting at Marker's 0° position)")]
    public WheelSlot[] slots;

    [Header("References")]
    [Tooltip("The RectTransform of the rotating marker/needle (the object that visually points to a wedge).")]
    public RectTransform marker;

    [Tooltip("Optional: auto-disabled while spinning, re-enabled after.")]
    public Button spinButton;

    [Header("Spin Animation")]
    [Tooltip("How long the spin animation lasts, in seconds.")]
    public float spinDuration = 4f;

    [Tooltip("How many full 360° rotations to add before landing, purely for visual effect.")]
    public int extraFullRounds = 5;

    [Tooltip("Random jitter inside the winning wedge so it doesn't always land dead-center (0 = always center).")]
    [Range(0f, 0.49f)]
    public float landingJitterRatio = 0.3f;

    [Header("Calibration")]
    [Tooltip("Marker's Z rotation (degrees) that corresponds to slots[0]. Usually 0 unless your art has an offset.")]
    public float calibrationOffsetZ = 0f;

    [Tooltip("Set true if increasing marker rotation Z visually moves it CLOCKWISE on screen. " +
             "In Unity's default UI space, increasing Z rotates counter-clockwise, so this is usually FALSE. " +
             "Flip this if slots land in the wrong wedge or spin the wrong way.")]
    public bool clockwisePositive = false;

    [Header("Events")]
    public Action<WheelSlot> OnRewardGranted;

    private bool isSpinning = false;

    [Header("Cooldown")]
    [SerializeField] private float freeSpinCooldown = 1800f; //30 phút

    [SerializeField] private TMP_Text timerText;

    [SerializeField] private TMP_Text buttonText;

    private const string NextFreeSpinKey = "LuckyWheel_NextFreeSpin";


    private void Update()
    {
        RefreshUI();
    }

    private DateTime GetNextFreeSpinTime()
    {
        if (!PlayerPrefs.HasKey(NextFreeSpinKey))
            return DateTime.MinValue;

        long binary = Convert.ToInt64(PlayerPrefs.GetString(NextFreeSpinKey));
        return DateTime.FromBinary(binary);
    }

    private void SaveNextFreeSpinTime(DateTime time)
    {
        PlayerPrefs.SetString(NextFreeSpinKey, time.ToBinary().ToString());
        PlayerPrefs.Save();
    }

    private bool IsFreeSpinReady()
    {
        return DateTime.UtcNow >= GetNextFreeSpinTime();
    }

    private void RefreshUI()
    {
        if (IsFreeSpinReady())
        {
            timerText.text = "FREE";
            buttonText.text = "SPIN";
        }
        else
        {
            TimeSpan remain = GetNextFreeSpinTime() - DateTime.UtcNow;

            timerText.text =
                $"{remain.Minutes:00}:{remain.Seconds:00}";

            buttonText.text = "Ads";
        }
    }
    /// <summary>
    /// Call this from the Spin button's OnClick().
    /// </summary>
    public void OnClickSpin()
    {
        if (isSpinning) return;

        if (IsFreeSpinReady())
        {
            StartSpin();
        }
        else
        {
            AdMobManager.Instance?.ShowRewarded(
                onSuccess: StartSpin,
                onFailed: () => { });
        }

    }

    private void StartSpin()
    {
        int targetIndex = ChooseWeightedRandomIndex();
        SpinMarkerTo(targetIndex);
    }
    /// <summary>
    /// Weighted random selection. Swap the body of this method for a server-authoritative
    /// call if you want the backend to decide the result (recommended for anti-cheat).
    /// </summary>
    private int ChooseWeightedRandomIndex()
    {
        float total = 0f;
        foreach (var s in slots) total += Mathf.Max(0f, s.weight);

        if (total <= 0f)
        {
            // Fallback: uniform random if weights are all zero/misconfigured.
            return UnityEngine.Random.Range(0, slots.Length);
        }

        float rand = UnityEngine.Random.Range(0f, total);
        float cumulative = 0f;

        for (int i = 0; i < slots.Length; i++)
        {
            cumulative += Mathf.Max(0f, slots[i].weight);
            if (rand <= cumulative) return i;
        }

        return slots.Length - 1; // safety fallback
    }

    /// <summary>
    /// Starts the spin animation, landing the marker on targetIndex.
    /// Call this directly if the server tells you which index won.
    /// </summary>
    public void SpinMarkerTo(int targetIndex)
    {
        if (marker == null)
        {
            Debug.LogError("[LuckyWheelController] Marker reference not set.");
            return;
        }

        targetIndex = Mathf.Clamp(targetIndex, 0, slots.Length - 1);

        isSpinning = true;
        if (spinButton != null) spinButton.interactable = false;

        StartCoroutine(SpinRoutine(targetIndex));
    }

    private IEnumerator SpinRoutine(int targetIndex)
    {
        float anglePerSlot = 360f / slots.Length;

        // Center angle of the winning wedge, plus a small random jitter so it
        // doesn't always stop exactly in the middle of the wedge.
        float jitter = UnityEngine.Random.Range(-anglePerSlot * landingJitterRatio,
                                                  anglePerSlot * landingJitterRatio);
        float targetAngle = targetIndex * anglePerSlot + jitter;

        float direction = clockwisePositive ? 1f : -1f;

        float startZ = marker.eulerAngles.z;
        float totalSpin = 360f * extraFullRounds + targetAngle;
        float finalZ = calibrationOffsetZ + direction * totalSpin;

        // Preserve current visual position while unwrapping rotation smoothly
        // (avoids a big jump if startZ is e.g. 350 and we want to go to 10).
        float startUnwrapped = startZ;
        float endUnwrapped = startUnwrapped + Mathf.DeltaAngle(startUnwrapped, finalZ) + direction * 360f * extraFullRounds;

        float t = 0f;
        while (t < spinDuration)
        {
            t += Time.deltaTime;
            float ratio = Mathf.Clamp01(t / spinDuration);
            float eased = 1f - Mathf.Pow(1f - ratio, 3f); // ease-out cubic
            float z = Mathf.Lerp(startUnwrapped, endUnwrapped, eased);
            marker.eulerAngles = new Vector3(0f, 0f, z);
            yield return null;
        }

        // Snap to the exact final angle to avoid floating point drift.
        float normalizedFinal = NormalizeAngle(calibrationOffsetZ + direction * targetAngle);
        marker.eulerAngles = new Vector3(0f, 0f, normalizedFinal);

        isSpinning = false;
        if (spinButton != null) spinButton.interactable = true;

        GiveReward(targetIndex);
    }

    private float NormalizeAngle(float angle)
    {
        angle %= 360f;
        if (angle < 0f) angle += 360f;
        return angle;
    }

    private void GiveReward(int index)
    {
        WheelSlot slot = slots[index];
        var reward = new RewardData(slot.type, slot.amount, slot.icon);


        if (slot.type == RewardType.Chest)
        {
            reward.closedChestIcon = slot.closedChestIcon ?? slot.icon;
            reward.chestContents = new List<RewardEntry>(slot.chestContents);
        }
        RewardData.Sanitize(reward);
        RewardManager.Instance?.GrantReward(reward);
        OnRewardGranted?.Invoke(slot);
        SaveNextFreeSpinTime(DateTime.UtcNow.AddSeconds(freeSpinCooldown));
    }

#if UNITY_EDITOR
    [ContextMenu("Debug: Spin To Slot 0")]
    private void DebugSpinToSlot0() => SpinMarkerTo(0);

    [ContextMenu("Debug: Random Spin")]
    private void DebugRandomSpin() => OnClickSpin();
#endif
}