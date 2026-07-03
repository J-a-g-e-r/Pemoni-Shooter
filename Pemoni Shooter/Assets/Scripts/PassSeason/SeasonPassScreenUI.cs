using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace SeasonPass
{
    /// <summary>
    /// Script gắn cho màn hình Season Pass. Sinh ra các hàng (row) theo dữ liệu SeasonPassData,
    /// mỗi hàng gồm 1 RewardSlotUI bên Free + 1 RewardSlotUI bên Gold.
    /// </summary>
    public class SeasonPassScreenUI : MonoBehaviour
    {
        [Header("Tham chiếu")]
        [SerializeField] private SeasonPassManager manager;

        [Header("Prefab & container")]
        [SerializeField] private GameObject rowPrefab;     // prefab 1 hàng, có 2 RewardSlotUI con: freeSlot, goldSlot
        [SerializeField] private Transform rowsContainer;  // content của ScrollView

        [Header("Header")]
        [SerializeField] private Button activateButton;
        [SerializeField] private TMP_Text pointsText;           // hiển thị kiểu "1/2"
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text countdownText;        // hiển thị kiểu "3d 15h"
        [SerializeField] private Slider pointsSlider;


        private readonly List<(RewardSlotUI free, RewardSlotUI gold)> rows = new();

        private Coroutine _countdownRoutine;

        private void OnEnable()
        {
            manager.OnStateChanged += RefreshAll;
            BuildRows();
            RefreshAll();
            RefreshCountdown();
            _countdownRoutine = StartCoroutine(CountdownLoop());
        }

        private void OnDisable()
        {
            manager.OnStateChanged -= RefreshAll;
            if (_countdownRoutine != null)
                StopCoroutine(_countdownRoutine);
        }

        private void BuildRows()
        {
            foreach (Transform child in rowsContainer) Destroy(child.gameObject);
            rows.Clear();

            var levels = manager.Data.levels;
            for (int i = 0; i < levels.Count; i++)
            {
                var rowGO = Instantiate(rowPrefab, rowsContainer);
                var rowUI = rowGO.GetComponent<RewardPassRowUI>();
                if (rowUI != null)
                    rowUI.Setup(i);
                var slots = rowGO.GetComponentsInChildren<RewardSlotUI>();
                // Quy ước: slots[0] = free (bên trái), slots[1] = gold (bên phải)
                var freeSlot = slots[0];
                var goldSlot = slots[1];

                freeSlot.Setup(i, isGoldColumn: false);
                goldSlot.Setup(i, isGoldColumn: true);

                rows.Add((freeSlot, goldSlot));
            }

            activateButton.onClick.RemoveAllListeners();
            activateButton.onClick.AddListener(OnClickActivate);
        }

        private void OnClickActivate()
        {
            // TODO: gọi luồng thanh toán/trừ gems ở đây trước, chỉ Activate khi thành công
            NavigationBarManager.Instance?.SelectItem(1);
        }

        private void RefreshAll()
        {
            foreach (var (free, gold) in rows)
            {
                free.Refresh();
                gold.Refresh();
            }

            if (pointsText != null)
            {
                int current = manager.GetPointsInCurrentLevel();
                int required = manager.GetPointsRequiredForCurrentLevelProgress();
                pointsText.text = $"{current}/{required}";
            }

            if (pointsSlider != null)
            {
                pointsSlider.minValue = 0f;
                pointsSlider.maxValue = 1f;
                pointsSlider.value = manager.GetCurrentLevelProgress01();
            }

            if (levelText != null)
            {
                levelText.text = manager.GetCurrentDisplayLevel().ToString();
            }

            if (activateButton != null)
                activateButton.gameObject.SetActive(!manager.IsGoldActive);
        }

        /// <summary>Gọi mỗi frame hoặc mỗi giây từ bên ngoài để cập nhật đếm ngược.</summary>
        public void UpdateCountdown(double secondsRemaining)
        {
            if (countdownText == null) return;
            var ts = System.TimeSpan.FromSeconds(secondsRemaining);
            countdownText.text = $"{ts.Days}d {ts.Hours}h";
        }

        private IEnumerator CountdownLoop()
        {
            var wait = new WaitForSeconds(1f);
            while (enabled)
            {
                RefreshCountdown();
                yield return wait;
            }
        }
        private void RefreshCountdown()
        {
            if (manager == null) return;
            UpdateCountdown(manager.GetSeasonSecondsRemaining());
        }
    }
}