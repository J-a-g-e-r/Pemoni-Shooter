using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HeartManager : SingletonPersistent<HeartManager>
{
    private const string HeartsKey = "heart_current";
    private const string NextRefillKey = "heart_next_refill";
    private const string InfiniteEndKey = "heart_infinite_end";

    [Header("Config")]
    [SerializeField] private int _maxHearts = 5;
    [SerializeField] private int _defaultHearts = 5;
    [SerializeField] private int _refillSeconds = 900;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI _heartText;
    [SerializeField] private TextMeshProUGUI _numberHeartText;
    [SerializeField] private Color normalColor = new Color32(214, 158, 122, 255); // #D69E7A


    [SerializeField] private GameObject _normalHeartIcon;
    [SerializeField] private GameObject _infiniteHeartIcon;
    //[SerializeField] private Image[] _heartSlots;

    private int _currentHearts;
    private long _nextRefillUnix;
    private long _infiniteEndUnix;

    public int CurrentHearts => _currentHearts;
    public int MaxHearts => _maxHearts;

    public override void Awake()
    {
        base.Awake();
        LoadData();
        ApplyOfflineProgress();
        RefreshUI();
    }

    private void Update()
    {
        ApplyOfflineProgress();
        RefreshUI();
    }

    public bool CanPlay()
    {
        ApplyOfflineProgress();
        return HasInfiniteHeart() || _currentHearts > 0;
    }

    public bool HasInfiniteHeart()
    {
        return GetNowUnix() < _infiniteEndUnix;
    }

    public void ConsumeHeartOnFailOrExit()
    {
        ApplyOfflineProgress();

        if (HasInfiniteHeart())
        {
            RefreshUI();
            return;
        }

        if (_currentHearts <= 0)
        {
            RefreshUI();
            return;
        }

        bool wasFull = _currentHearts >= _maxHearts;
        _currentHearts--;

        if (wasFull && _currentHearts < _maxHearts)
            _nextRefillUnix = GetNowUnix() + _refillSeconds;

        SaveData();
        RefreshUI();
    }

    public void GrantInfiniteHeart(int durationSeconds)
    {
        long now = GetNowUnix();

        if (HasInfiniteHeart())
            _infiniteEndUnix += durationSeconds;
        else
            _infiniteEndUnix = now + durationSeconds;

        SaveData();
        RefreshUI();
    }

    public void AddHearts(int amount)
    {
        ApplyOfflineProgress();

        _currentHearts = Mathf.Clamp(_currentHearts + amount, 0, _maxHearts);

        if (_currentHearts >= _maxHearts)
            _nextRefillUnix = 0;

        SaveData();
        RefreshUI();
    }

    private void ApplyOfflineProgress()
    {
        long now = GetNowUnix();

        if (!HasInfiniteHeart() && _currentHearts < _maxHearts && _nextRefillUnix > 0)
        {
            while (_currentHearts < _maxHearts && now >= _nextRefillUnix)
            {
                _currentHearts++;

                if (_currentHearts < _maxHearts)
                    _nextRefillUnix += _refillSeconds;
                else
                    _nextRefillUnix = 0;
            }
        }

        if (_currentHearts >= _maxHearts)
            _nextRefillUnix = 0;
    }

    private void RefreshUI()
    {
        bool hasInfinite = HasInfiniteHeart();

        // Icon
        if (_normalHeartIcon != null)
            _normalHeartIcon.SetActive(!hasInfinite);

        if (_infiniteHeartIcon != null)
            _infiniteHeartIcon.SetActive(hasInfinite);

        // Number Heart
        if (_numberHeartText != null)
        {
            _numberHeartText.gameObject.SetActive(!hasInfinite);

            if (!hasInfinite)
                _numberHeartText.text = _currentHearts.ToString();
        }

        // Heart Text
        if (_heartText != null)
        {
            if (hasInfinite)
            {
                _heartText.text = FormatTime(_infiniteEndUnix - GetNowUnix());
                _heartText.color = normalColor;
            }
            else if (_currentHearts >= _maxHearts)
            {
                _heartText.text = "Full";
                _heartText.color = Color.green;
            }
            else
            {
                _heartText.text = FormatTime(_nextRefillUnix - GetNowUnix());
                _heartText.color = normalColor;
            }
        }

        //// Heart Slots
        //if (_heartSlots != null)
        //{
        //    for (int i = 0; i < _heartSlots.Length; i++)
        //    {
        //        if (_heartSlots[i] != null)
        //            _heartSlots[i].enabled = !hasInfinite && i < _currentHearts;
        //    }
        //}
    }

    private void LoadData()
    {
        _currentHearts = PlayerPrefs.GetInt(HeartsKey, _defaultHearts);
        _nextRefillUnix = long.Parse(PlayerPrefs.GetString(NextRefillKey, "0"));
        _infiniteEndUnix = long.Parse(PlayerPrefs.GetString(InfiniteEndKey, "0"));
        _currentHearts = Mathf.Clamp(_currentHearts, 0, _maxHearts);
    }

    private void SaveData()
    {
        PlayerPrefs.SetInt(HeartsKey, _currentHearts);
        PlayerPrefs.SetString(NextRefillKey, _nextRefillUnix.ToString());
        PlayerPrefs.SetString(InfiniteEndKey, _infiniteEndUnix.ToString());
        PlayerPrefs.Save();
    }

    private long GetNowUnix()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }

    private string FormatTime(long totalSeconds)
    {
        totalSeconds = Math.Max(0, totalSeconds);
        TimeSpan time = TimeSpan.FromSeconds(totalSeconds);

        if (time.TotalHours >= 1)
            return $"{(int)time.TotalHours:00}:{time.Minutes:00}:{time.Seconds:00}";

        return $"{time.Minutes:00}:{time.Seconds:00}";
    }

    public void BindUI(
    TextMeshProUGUI heartText,
    TextMeshProUGUI numberHeartText,
    GameObject normalHeartIcon,
    GameObject infiniteHeartIcon)
    {
        _heartText = heartText;
        _numberHeartText = numberHeartText;
        _normalHeartIcon = normalHeartIcon;
        _infiniteHeartIcon = infiniteHeartIcon;
        RefreshUI();
    }
    public void UnbindUI()
    {
        _heartText = null;
        _numberHeartText = null;
        _normalHeartIcon = null;
        _infiniteHeartIcon = null;
    }
}