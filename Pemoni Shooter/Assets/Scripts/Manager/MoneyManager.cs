using DG.Tweening;
using TMPro;
using UnityEngine;

public class MoneyManager : MonoBehaviour
{
    public static MoneyManager Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private Canvas _canvas;
    [SerializeField] private RectTransform _coinTarget;      // icon tiền trên HUD (child Image của Money UI)
    [SerializeField] private TextMeshProUGUI _moneyText;
    [SerializeField] private RectTransform _flyParent;       // container spawn coin bay (thường là Canvas hoặc child)
    [SerializeField] private ParticleSystem _collectEffect;     // hiệu ứng khi coin bay đến đích

    [Header("Prefab")]
    [SerializeField] private RectTransform _coinFlyPrefab;

    [Header("Reward")]
    [SerializeField] private int _moneyPerTray = 10;

    [Header("Fly Anim")]
    [SerializeField] private float _flyDuration = 0.45f;
    [SerializeField] private Ease _flyEase = Ease.InQuad;

    private int _totalMoney;
    private Camera _worldCamera;

    private void Awake()
    {
        Instance = this;
        _worldCamera = Camera.main;
        UpdateMoneyUI();
    }

    /// <summary>
    /// Gọi khi 1 khay vừa lấp đầy.
    /// </summary>
    public void OnTrayCompleted(Transform trayTransform, int amount = -1)
    {
        int reward = amount > 0 ? amount : _moneyPerTray;
        SpawnAndFlyCoin(trayTransform.position, reward);
    }

    private void SpawnAndFlyCoin(Vector3 worldSpawnPos, int amount)
    {
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

    // World (khay trên bàn) -> local của flyParent
    private Vector2 WorldToFlyParentLocal(Vector3 worldPos)
    {
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(_worldCamera, worldPos);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _flyParent, screen, GetUICamera(), out Vector2 local);
        return local;
    }

    // UI icon tiền -> local của flyParent
    private Vector2 UIToFlyParentLocal(RectTransform uiRect)
    {
        // Lấy tâm icon, không dùng pivot
        Vector3 worldCenter = uiRect.TransformPoint(uiRect.rect.center);
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(GetUICamera(), worldCenter);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _flyParent, screen, GetUICamera(), out Vector2 local);
        return local;
    }


    private void AddMoney(int amount)
    {
        _totalMoney += amount;
        UpdateMoneyUI();

        // Optional: punch scale icon tiền khi nhận
        _coinTarget.DOKill();
        _coinTarget.localScale = Vector3.one;
        _coinTarget.DOPunchScale(Vector3.one * 0.5f, 0.2f, 1, 0.5f);
        // Hiệu ứng khi coin bay đến đích
        if (_collectEffect != null)
        {
            _collectEffect.transform.position = _coinTarget.position;
            // Dừng phát hạt mới từ hiệu ứng cũ nhưng GIỮ NGUYÊN các hạt đang bay dở trên màn hình
            _collectEffect.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmitting);

            // Phát lại toàn bộ hệ thống cha và con từ đầu
            _collectEffect.Play(withChildren: true);
        }
    }

    private void UpdateMoneyUI()
    {
        if (_moneyText != null)
            _moneyText.text = _totalMoney.ToString();
    }
}