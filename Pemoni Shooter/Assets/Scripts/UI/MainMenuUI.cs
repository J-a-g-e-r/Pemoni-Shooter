using AudioSystem;
using DG.Tweening;
using System.Collections;
using TMPro;
using UnityEngine;

public class MainMenuUI : MonoBehaviour
{
    [Header("All Panels")]
    [SerializeField] private TextMeshProUGUI _levelButtonText;
    [SerializeField] private GameObject _fadePanel;
    [SerializeField] private GameObject _settingPanel;
    [SerializeField] private GameObject _seasonPassPanel;



    [Header("Popup Anim")]
    [SerializeField] private float _popInDuration = 0.3f;
    [SerializeField] private float _popOutDuration = 0.2f;
    [SerializeField] private Ease _popInEase = Ease.OutBack;
    [SerializeField] private Ease _popOutEase = Ease.InBack;

    private void Start()
    {
        RefreshLevelText();
        HidePanelInstant(_settingPanel);
    }

    private void OnEnable()
    {
        RefreshLevelText(); // cập nhật mỗi lần quay về Home
    }

    public void RefreshLevelText()
    {
        if (_levelButtonText != null && LevelManager.Instance != null)
            _levelButtonText.text = LevelManager.Instance.GetLevelDisplayText();
    }

    public void OnPlayClicked()
    {
        LevelManager.Instance?.TryLoadCurrentLevel();
    }

    private void HidePanelInstant(GameObject panel)
    {
        if (panel == null) return;
        panel.transform.DOKill();
        panel.transform.localScale = Vector3.zero;
        panel.SetActive(false);
    }

    private void OpenFadePanel()
    {
        if (_fadePanel != null)
            _fadePanel.SetActive(true);
    }

    private void CloseFadePanel()
    {
        if (_fadePanel != null)
            _fadePanel.SetActive(false);
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

    public void OnOpenSettingPanel()
    {
        AudioManager.Instance.PlaySFX("Click");
        OpenFadePanel();
        PopIn(_settingPanel);
    }

    public void CloseSettingPanel()
    {
        StartCoroutine(PopOut(_settingPanel));
        CloseFadePanel();
    }

    public void OnOpenSeasonPassPanel()
    {
        AudioManager.Instance.PlaySFX("Click");
        //OpenFadePanel();
        PopIn(_seasonPassPanel);
    }

    public void CloseSeasonPassPanel()
    {
        StartCoroutine(PopOut(_seasonPassPanel));
        //CloseFadePanel();
    }

    public void CloseAllPanel()
    {
        _seasonPassPanel.SetActive(false);
        _settingPanel.SetActive(false);
        CloseFadePanel();
    }



}