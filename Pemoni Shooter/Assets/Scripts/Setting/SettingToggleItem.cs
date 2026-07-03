using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class SettingToggleItem : MonoBehaviour
{
    [Header("Toggle Knob")]
    [SerializeField] private Button _toggleButton;
    [SerializeField] private RectTransform _knobRect;      // Button xanh
    [SerializeField] private Image _knobImage;           // Image của Button xanh

    [Header("Icon")]
    [SerializeField] private Image _iconOn;                // MusicIcon
    [SerializeField] private Image _iconOff;               // MusicIconOff (luôn hiện, alpha = 1)

    [Header("Positions")]
    [SerializeField] private float _posOn = -27.5f;
    [SerializeField] private float _posOff = -115f;

    [Header("Animation")]
    [SerializeField] private float _animDuration = 0.2f;

    [Header("State")]
    [SerializeField] private bool _isOn = true;
    [SerializeField] private string _playerPrefsKey = "setting_music";

    public bool IsOn => _isOn;
    public System.Action<bool> OnValueChanged;

    private void Awake()
    {
        _isOn = PlayerPrefs.GetInt(_playerPrefsKey, 1) == 1;
        ApplyVisualInstant(_isOn);
        _toggleButton.onClick.AddListener(Toggle);
    }

    public void Toggle()
    {
        SetValue(!_isOn, animate: true);
    }

    public void SetValue(bool isOn, bool animate = true)
    {
        _isOn = isOn;
        PlayerPrefs.SetInt(_playerPrefsKey, _isOn ? 1 : 0);
        PlayerPrefs.Save();

        OnValueChanged?.Invoke(_isOn);

        if (animate)
            ApplyVisualAnimated(_isOn);
        else
            ApplyVisualInstant(_isOn);
    }

    private void ApplyVisualInstant(bool isOn)
    {
        _knobRect.DOKill();
        _knobImage.DOKill();
        _iconOn.DOKill();

        _knobRect.anchoredPosition = new Vector2(isOn ? _posOn : _posOff, _knobRect.anchoredPosition.y);
        _knobImage.color = new Color(_knobImage.color.r, _knobImage.color.g, _knobImage.color.b, isOn ? 1f : 0f);
        _iconOn.color = new Color(_iconOn.color.r, _iconOn.color.g, _iconOn.color.b, isOn ? 1f : 0f);
    }

    private void ApplyVisualAnimated(bool isOn)
    {
        float targetX = isOn ? _posOn : _posOff;
        float targetAlpha = isOn ? 1f : 0f;

        _knobRect.DOKill();
        _knobImage.DOKill();
        _iconOn.DOKill();

        _knobRect.DOAnchorPosX(targetX, _animDuration).SetEase(Ease.OutQuad);
        _knobImage.DOFade(targetAlpha, _animDuration);
        _iconOn.DOFade(targetAlpha, _animDuration);
    }
}