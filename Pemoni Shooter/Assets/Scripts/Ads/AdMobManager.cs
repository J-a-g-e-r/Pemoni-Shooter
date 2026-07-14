using System;
using GoogleMobileAds.Api;
using UnityEngine;

public class AdMobManager : SingletonPersistent<AdMobManager>
{
    // Google test IDs — chỉ dùng khi dev
    private const string AndroidTestRewardedId = "ca-app-pub-3940256099942544/5224354917";

    [Header("Ad Unit IDs")]
    [SerializeField] private string androidRewardedAdUnitId = AndroidTestRewardedId;

    [Header("Editor")]
    [SerializeField] private bool simulateInEditor = true;

    private RewardedAd _rewardedAd;
    private bool _isShowing;
    private bool _rewardEarned;
    private Action _pendingSuccess;
    private Action _pendingFailed;

    public bool IsRewardedReady =>
        _rewardedAd != null && _rewardedAd.CanShowAd() && !_isShowing;

    public override void Awake()
    {
        base.Awake();
        if (Instance != this) return;

#if UNITY_ANDROID && !UNITY_EDITOR
        MobileAds.Initialize(_ => LoadRewardedAd());
#else
        if (!simulateInEditor)
            MobileAds.Initialize(_ => LoadRewardedAd());
#endif
    }

    public void ShowRewarded(Action onSuccess, Action onFailed)
    {
        if (_isShowing) return;

#if UNITY_EDITOR
        if (simulateInEditor)
        {
            Debug.Log("[AdMobManager] Editor simulate rewarded ad success.");
            onSuccess?.Invoke();
            return;
        }
#endif

#if !UNITY_ANDROID
        Debug.LogWarning("[AdMobManager] Rewarded ads only supported on Android.");
        onFailed?.Invoke();
        return;
#endif

        if (!IsRewardedReady)
        {
            Debug.LogWarning("[AdMobManager] Rewarded ad not ready.");
            onFailed?.Invoke();
            LoadRewardedAd();
            return;
        }

        _pendingSuccess = onSuccess;
        _pendingFailed = onFailed;
        _isShowing = true;
        _rewardEarned = false;

        _rewardedAd.Show(_ =>
        {
            _rewardEarned = true;
            CompleteRewardedSuccess();
        });
    }

    private void LoadRewardedAd()
    {
#if UNITY_ANDROID
        if (_rewardedAd != null)
        {
            _rewardedAd.Destroy();
            _rewardedAd = null;
        }

        string adUnitId = string.IsNullOrWhiteSpace(androidRewardedAdUnitId)
            ? AndroidTestRewardedId
            : androidRewardedAdUnitId;

        RewardedAd.Load(adUnitId, new AdRequest(), (ad, error) =>
        {
            if (error != null || ad == null)
            {
                Debug.LogWarning($"[AdMobManager] Load failed: {error}");
                return;
            }

            _rewardedAd = ad;
            RegisterRewardedEvents(ad);
            Debug.Log("[AdMobManager] Rewarded ad loaded.");
        });
#endif
    }

    private void RegisterRewardedEvents(RewardedAd ad)
    {
        ad.OnAdFullScreenContentClosed += () =>
        {
            if (!_rewardEarned)
                CompleteRewardedFailed();

            _isShowing = false;
            LoadRewardedAd();
        };

        ad.OnAdFullScreenContentFailed += error =>
        {
            Debug.LogWarning($"[AdMobManager] Show failed: {error}");
            CompleteRewardedFailed();
            _isShowing = false;
            LoadRewardedAd();
        };
    }

    private void CompleteRewardedSuccess()
    {
        var cb = _pendingSuccess;
        ClearPendingCallbacks();
        cb?.Invoke();
    }

    private void CompleteRewardedFailed()
    {
        var cb = _pendingFailed;
        ClearPendingCallbacks();
        cb?.Invoke();
    }

    private void ClearPendingCallbacks()
    {
        _pendingSuccess = null;
        _pendingFailed = null;
    }
}