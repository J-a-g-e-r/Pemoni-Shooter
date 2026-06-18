using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    [Header("Setup")]
    [SerializeField] private bool _enableTutorial = true;
    [SerializeField] private GameObject _handTutPrefab;
    [Tooltip("Kéo thả các Tray theo thứ tự người chơi cần bấm")]
    [SerializeField] private List<Tray> _tutorialTrays = new();

    [Header("Hand Position")]
    [SerializeField] private Vector3 _handOffset = new(0f, 0.8f, -1f);

    [Header("Animation")]
    [SerializeField] private float _fadeDuration = 0.4f;
    [SerializeField] private float _tapDuration = 0.5f;
    [SerializeField] private float _tapMoveY = 0.15f;

    private int _currentIndex;
    private GameObject _currentHand;
    private SpriteRenderer _handRenderer;
    private Tween _tapTween;
    private bool _isActive;
    private bool _isTransitioning;

    public bool IsActive => _isActive;
    public Tray CurrentTray =>
        _isActive && _currentIndex < _tutorialTrays.Count
            ? _tutorialTrays[_currentIndex]
            : null;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (!_enableTutorial || _tutorialTrays.Count == 0 || _handTutPrefab == null)
            return;

        _isActive = true;
        ShowHandAtCurrentTray();
    }

    /// <summary>
    /// Tray gọi khi được click thành công.
    /// </summary>
    public void OnTrayClicked(Tray tray)
    {
        if (!_isActive || _isTransitioning) return;
        if (tray != CurrentTray) return;

        StartCoroutine(AdvanceAfterClick());
    }

    /// <summary>
    /// Tray gọi trước khi xử lý click — chỉ cho bấm đúng khay tutorial.
    /// </summary>
    public bool CanClickTray(Tray tray)
    {
        if (!_isActive) return true;
        return tray == CurrentTray && !_isTransitioning;
    }

    private void ShowHandAtCurrentTray()
    {
        Tray tray = CurrentTray;
        if (tray == null)
        {
            EndTutorial();
            return;
        }

        Vector3 pos = tray.transform.position + _handOffset;
        _currentHand = Instantiate(_handTutPrefab, pos, _handTutPrefab.transform.rotation);
        _handRenderer = _currentHand.GetComponentInChildren<SpriteRenderer>();

        // Reset alpha (tránh prefab bị chỉnh sẵn)
        if (_handRenderer != null)
        {
            Color c = _handRenderer.color;
            c.a = 1f;
            _handRenderer.color = c;
        }

        //PlayTapLoop();
    }

    private void PlayTapLoop()
    {
        if (_currentHand == null) return;

        _tapTween?.Kill();
        Vector3 basePos = _currentHand.transform.position;

        _tapTween = _currentHand.transform
            .DOMoveY(basePos.y - _tapMoveY, _tapDuration * 0.5f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    private IEnumerator AdvanceAfterClick()
    {
        _isTransitioning = true;
        _tapTween?.Kill();

        if (_handRenderer != null)
        {
            yield return _handRenderer
                .DOFade(0f, _fadeDuration)
                .WaitForCompletion();
        }
        else
        {
            yield return new WaitForSeconds(_fadeDuration);
        }

        if (_currentHand != null)
            Destroy(_currentHand);

        _currentIndex++;
        _isTransitioning = false;

        if (_currentIndex >= _tutorialTrays.Count)
            EndTutorial();
        else
            ShowHandAtCurrentTray();
    }

    private void EndTutorial()
    {
        _isActive = false;
        _tapTween?.Kill();
        Debug.Log("[TutorialManager] Tutorial hoàn thành.");
    }

    private void OnDestroy()
    {
        _tapTween?.Kill();
    }
}