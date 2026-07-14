using System;
using AudioSystem;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class MoneyManager : SingletonPersistent<MoneyManager>
{
    private const string MoneyKey = "player_coins";

    [Header("Config")]
    [SerializeField] private int _defaultMoney = 0;
    [SerializeField] private int _moneyPerTray = 10;

    [Header("Fly Anim")]
    [SerializeField] private float _flyDuration = 0.45f;
    [SerializeField] private Ease _flyEase = Ease.InQuad;

    [Header("Feedback")]
    [SerializeField] private float _shakeDuration = 0.35f;
    [SerializeField] private float _shakeStrength = 20f;

    // HUD (bind từ scene)
    private TextMeshProUGUI _moneyText;
    private RectTransform _coinTarget;
    private RectTransform _moneyPanel;
    private Color _defaultTextColor = Color.white;

    // Fly anim (bind từ gameplay scene)
    private Canvas _canvas;
    private RectTransform _flyParent;
    private RectTransform _coinFlyPrefab;
    private ParticleSystem _collectEffect;

    private int _totalMoney;
  private Camera _worldCamera;

    public int TotalMoney => _totalMoney;
    public event Action<int> OnMoneyChanged;

    public override void Awake()
    {
        base.Awake();
        LoadData();
        RefreshUI();
    }

    // ===================== PUBLIC API =====================

    public bool CanAfford(int amount) => _totalMoney >= amount;

    public bool TrySpend(int amount)
    {
        if (!CanAfford(amount))
        {
            PlayNotEnoughFeedback();
            return false;
        }

        _totalMoney -= amount;
        SaveData();
        RefreshUI();
        AudioManager.Instance.PlaySFX("UseCoin");
        return true;
    }

    public void AddMoney(int amount)
    {
        if (amount <= 0) return;

        _totalMoney += amount;
        SaveData();
        RefreshUI();
        PlayCollectFeedback();
    }

    /// <summary>
    /// Gọi khi 1 khay vừa lấp đầy.
    /// </summary>
    public void OnTrayCompleted(Transform trayTransform, int amount = -1)
    {
        int reward = amount > 0 ? amount : _moneyPerTray;
        MissionManager.Instance?.AddProgress(MissionType.CollectCoins, reward);


        if (CanPlayFlyAnim())
            SpawnAndFlyCoin(trayTransform.position, reward);
        else
            AddMoney(reward);
    }

    // ===================== UI BINDING =====================

    public void BindHUD(
        TextMeshProUGUI moneyText,
        RectTransform coinTarget,
        RectTransform moneyPanel = null)
    {
        _moneyText = moneyText;
        _coinTarget = coinTarget;
        _moneyPanel = moneyPanel;

        if (_moneyText != null)
            _defaultTextColor = _moneyText.color;

        RefreshUI();
    }

    public void BindFlyAnim(
        Canvas canvas,
        RectTransform flyParent,
        RectTransform coinFlyPrefab,
        ParticleSystem collectEffect = null)
    {
        _canvas = canvas;
        _flyParent = flyParent;
        _coinFlyPrefab = coinFlyPrefab;
        _collectEffect = collectEffect;
        _worldCamera = Camera.main;
    }

    public void UnbindUI()
    {
        _moneyText = null;
        _coinTarget = null;
        _moneyPanel = null;

        _canvas = null;
        _flyParent = null;
        _coinFlyPrefab = null;
        _collectEffect = null;
        _worldCamera = null;
    }

    // ===================== DATA =====================

    private void LoadData()
    {
        if (!PlayerPrefs.HasKey(MoneyKey))
        {
            _totalMoney = _defaultMoney;
            SaveData();
            return;
        }

        _totalMoney = PlayerPrefs.GetInt(MoneyKey, _defaultMoney);
    }

    private void SaveData()
    {
        PlayerPrefs.SetInt(MoneyKey, _totalMoney);
        PlayerPrefs.Save();
    }

    private void RefreshUI()
    {
        if (_moneyText != null)
            _moneyText.text = FormatMoney(_totalMoney);

        OnMoneyChanged?.Invoke(_totalMoney);
    }

    // ===================== FLY ANIM =====================

    private bool CanPlayFlyAnim()
    {
        return _coinFlyPrefab != null
            && _flyParent != null
            && _coinTarget != null
            && _canvas != null;
    }

    private void SpawnAndFlyCoin(Vector3 worldSpawnPos, int amount)
    {
        if (_worldCamera == null)
            _worldCamera = Camera.main;

        RectTransform coin = Instantiate(_coinFlyPrefab, _flyParent);
        coin.gameObject.SetActive(true);
        coin.localScale = Vector3.one;

        Vector2 startLocal = WorldToFlyParentLocal(worldSpawnPos);
        Vector2 targetLocal = UIToFlyParentLocal(_coinTarget);
        coin.anchoredPosition = startLocal;

        coin.DOAnchorPos(targetLocal, _flyDuration)
            .SetEase(_flyEase)
            .OnComplete(() =>
            {
                AddMoney(amount);
                Destroy(coin.gameObject);
            });
    }

    private Camera GetUICamera()
    {
        return _canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : _canvas.worldCamera;
    }

    private Vector2 WorldToFlyParentLocal(Vector3 worldPos)
    {
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(_worldCamera, worldPos);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _flyParent, screen, GetUICamera(), out Vector2 local);
        return local;
    }

    private Vector2 UIToFlyParentLocal(RectTransform uiRect)
    {
        Vector3 worldCenter = uiRect.TransformPoint(uiRect.rect.center);
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(GetUICamera(), worldCenter);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _flyParent, screen, GetUICamera(), out Vector2 local);
        return local;
    }

    // ===================== FEEDBACK =====================

    private void PlayCollectFeedback()
    {
        if (_coinTarget != null)
        {
            _coinTarget.DOKill();
            _coinTarget.localScale = Vector3.one;
            _coinTarget.DOPunchScale(Vector3.one * 0.5f, 0.2f, 1, 0.5f);
        }

        if (_collectEffect != null && _coinTarget != null)
        {
            _collectEffect.transform.position = _coinTarget.position;
            _collectEffect.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmitting);
            _collectEffect.Play(withChildren: true);
        }
    }

    private void PlayNotEnoughFeedback()
    {
        AudioManager.Instance.PlaySFX("NotEnoughMoney");
        ShakeMoneyUI();
    }

    private void ShakeMoneyUI()
    {
        if (_moneyPanel == null || _moneyText == null) return;

        _moneyPanel.DOKill();
        _moneyPanel.DOShakeAnchorPos(
            duration: _shakeDuration,
            strength: new Vector2(_shakeStrength, 0),
            vibrato: 20,
            randomness: 90,
            snapping: false,
            fadeOut: true);

        _moneyPanel.localScale = Vector3.one;
        _moneyPanel.DOPunchScale(Vector3.one * 0.15f, 0.25f);

        _moneyText.DOColor(Color.red, 0.1f)
            .OnComplete(() => _moneyText.DOColor(_defaultTextColor, 0.2f));
    }

    private static string FormatMoney(int value)
    {
        if (value >= 1_000_000_000)
            return (value / 1_000_000_000f).ToString("0.#") + "B";
        if (value >= 1_000_000)
            return (value / 1_000_000f).ToString("0.#") + "M";
        if (value >= 1_000)
            return (value / 1_000f).ToString("0.#") + "K";
        return value.ToString();
    }
}