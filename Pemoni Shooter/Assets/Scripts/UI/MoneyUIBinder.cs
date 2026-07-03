using TMPro;
using UnityEngine;

/// <summary>
/// Gắn UI tiền của scene hiện tại vào MoneyManager persistent.
/// Mỗi scene có 1 binder; không cần duplicate MoneyManager.
/// </summary>
public class MoneyUIBinder : MonoBehaviour
{
    [Header("HUD")]
    [SerializeField] private TextMeshProUGUI _moneyText;
    [SerializeField] private RectTransform _coinTarget;
    [SerializeField] private RectTransform _moneyPanel;

    [Header("Fly Anim (chỉ scene gameplay)")]
    [SerializeField] private bool _bindFlyAnim;
    [SerializeField] private Canvas _canvas;
    [SerializeField] private RectTransform _flyParent;
    [SerializeField] private RectTransform _coinFlyPrefab;
    [SerializeField] private ParticleSystem _collectEffect;

    private void Start()
    {
        if (MoneyManager.Instance == null) return;

        MoneyManager.Instance.BindHUD(_moneyText, _coinTarget, _moneyPanel);

        if (_bindFlyAnim)
        {
            MoneyManager.Instance.BindFlyAnim(
                _canvas, _flyParent, _coinFlyPrefab, _collectEffect);
        }
    }

    private void OnDestroy()
    {
        MoneyManager.Instance?.UnbindUI();
    }
}