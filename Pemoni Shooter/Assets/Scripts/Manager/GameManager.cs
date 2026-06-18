using System.Collections;
using TMPro;
using UnityEngine;
using DG.Tweening;
using UnityEngine.SceneManagement;

/// <summary>
/// Quản lý trạng thái màn chơi: thắng / thua / UI số cốc còn lại.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI _cupLeftText;

    [Header("Panels")]
    [SerializeField] private GameObject _fadePanel;
    [SerializeField] private GameObject _congratPanel;
    [SerializeField] private GameObject _winPanel;
    [SerializeField] private GameObject _outOfSpacePanel;
    [SerializeField] private GameObject _losePanel;

    [Header("Timing")]
    [SerializeField] private float _winDelay = 1f;

    [Header("Popup Anim")]
    [SerializeField] private float _popInDuration = 0.3f;
    [SerializeField] private float _popOutDuration = 0.2f;
    [SerializeField] private Ease _popInEase = Ease.OutBack;
    [SerializeField] private Ease _popOutEase = Ease.InBack;



    //[Header("Win Fireworks Spawn")]
    //[SerializeField] private ParticleSystem _fireworkPrefab;
    //[SerializeField] private RectTransform _fireworkParent;
    //[SerializeField] private RectTransform[] _fireworkSpawnPoints; // gán đúng 5 điểm
    //[SerializeField] private int _fireworksToSpawn = 5;
    //[SerializeField] private float _fireworkSpawnInterval = 0.2f;


    private bool _gameOver = false;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        HidePanelInstant(_congratPanel);
        HidePanelInstant(_winPanel);
        HidePanelInstant(_outOfSpacePanel);
        HidePanelInstant(_losePanel);
    }

    public void UpdateCupLeftUI(int remaining)
    {
        if (_cupLeftText != null)
            _cupLeftText.text = remaining.ToString();
    }

    public void OnWin()
    {
        if (_gameOver) return;
        _gameOver = true;

        Debug.Log("[GameManager] WIN!");



        //OpenFadePanel();
        PopIn(_congratPanel);
        StartCoroutine(ShowPanelAfterDelay(_winPanel,_winDelay));
    }

    private void PlayEffect(ParticleSystem effect)
    {
        if (effect == null) return;

        if (!effect.gameObject.activeSelf)
            effect.gameObject.SetActive(true);

        // Reset state before replay to ensure effect is visible every win.
        effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        effect.Clear(true);
        effect.Play(true);
    }

    //private IEnumerator ShowWinAfterDelay()
    //{
    //    yield return new WaitForSeconds(_winDelay);
    //    PopIn(_winPanel);
    //}

    public void CheckLose()
    {
        if (_gameOver) return;
        if (CupQueue.Instance.TotalRemaining == 0) return;

        if (TableSlotManager.Instance.IsFull && !CanDispatchAny())
        {
            _gameOver = true;
            Debug.Log("[GameManager] LOSE!");

            OpenFadePanel();
            StartCoroutine(ShowPanelAfterDelay(_outOfSpacePanel, 2f));
        }
    }
    private IEnumerator ShowPanelAfterDelay(GameObject panel,float t)
    {
        yield return new WaitForSeconds(t);

        if (panel != null)
            PopIn(panel);
    }

    /// <summary>
    /// Nút Give Up gọi hàm này.
    /// </summary>
    public void OnGiveUp()
    {
        StartCoroutine(HandleGiveUpFlow());
    }



    private IEnumerator HandleGiveUpFlow()
    {
        if (_outOfSpacePanel != null && _outOfSpacePanel.activeSelf)
        {
            yield return PopOut(_outOfSpacePanel);
        }

        PopIn(_losePanel);
    }

    private bool CanDispatchAny()
    {
        Cup front = CupQueue.Instance.GetFrontCup();
        if (front == null) return false;

        Tray tray = TableSlotManager.Instance.GetTrayByColor(front.Color);
        return tray != null && tray.GetNextEmptyCupSlot() != null;
    }

    public void RestartLevel()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }

    public void LoadMainScene()
    {
        SceneManager.LoadScene(0);
    }

    private void OpenFadePanel()
    {
        if (_fadePanel != null)
            _fadePanel.SetActive(true);
    }

    private void HidePanelInstant(GameObject panel)
    {
        if (panel == null) return;
        panel.transform.DOKill();
        panel.transform.localScale = Vector3.zero;
        panel.SetActive(false);
    }

    private void PopIn(GameObject panel)
    {
        if (panel == null) return;

        panel.transform.DOKill();
        panel.SetActive(true);
        panel.transform.localScale = Vector3.zero;
        panel.transform
            .DOScale(Vector3.one, _popInDuration)
            .SetEase(_popInEase);
    }

    private IEnumerator PopOut(GameObject panel)
    {
        if (panel == null) yield break;

        panel.transform.DOKill();
        Tween t = panel.transform
            .DOScale(Vector3.zero, _popOutDuration)
            .SetEase(_popOutEase);

        yield return t.WaitForCompletion();
        panel.SetActive(false);
    }
}