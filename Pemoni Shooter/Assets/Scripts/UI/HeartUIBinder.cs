using TMPro;
using UnityEngine;

public class HeartUIBinder : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _heartText;
    [SerializeField] private TextMeshProUGUI _numberHeartText;
    [SerializeField] private GameObject _normalHeartIcon;
    [SerializeField] private GameObject _infiniteHeartIcon;

    private void OnEnable()
    {
        if (HeartManager.Instance == null) return;

        HeartManager.Instance.BindUI(
            _heartText,
            _numberHeartText,
            _normalHeartIcon,
            _infiniteHeartIcon);
    }

    private void OnDisable()
    {
        if (HeartManager.Instance == null) return;
        HeartManager.Instance.UnbindUI();
    }
}