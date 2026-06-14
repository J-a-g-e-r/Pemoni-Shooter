using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gắn vào GameObject bất kỳ trong scene (ví dụ: GameManager).
/// Tính tổng capacity của tất cả Tray, tạo đúng số lượng Cup tương ứng,
/// shuffle ngẫu nhiên rồi nạp vào CupQueue.
/// </summary>
public class CupSpawner : MonoBehaviour
{
    [Header("Cup Prefabs – đặt đúng thứ tự enum TrayColor")]
    [SerializeField] private GameObject _cupBluePrefab;
    [SerializeField] private GameObject _cupPinkPrefab;
    [SerializeField] private GameObject _cupRedPrefab;
    [SerializeField] private GameObject _cupYellowPrefab;
    [SerializeField] private GameObject _cupGreenPrefab;
    [SerializeField] private GameObject _cupPurplePrefab;
    [SerializeField] private GameObject _cupOrangePrefab;
    [SerializeField] private GameObject _cupBrownPrefab;

    // -------------------------------------------------------

    private void Start()
    {
        SpawnForLevel();
    }

    /// <summary>
    /// Tính capacity từ tất cả Tray hiện có,
    /// tạo danh sách Cup đúng số lượng + màu, shuffle rồi đưa vào CupQueue.
    /// </summary>
    public void SpawnForLevel()
    {
        // 1. Thu thập tất cả Tray trong scene
        Tray[] allTrays = FindObjectsOfType<Tray>();

        // 2. Đếm số cốc cần theo từng màu
        //    key = TrayColor, value = tổng capacity
        var colorCount = new Dictionary<TrayColor, int>();

        foreach (Tray tray in allTrays)
        {
            int cap = tray.Capacity;
            if (!colorCount.ContainsKey(tray.TrayColor))
                colorCount[tray.TrayColor] = 0;
            colorCount[tray.TrayColor] += cap;
        }

        // 3. Tạo list màu (mỗi phần tử = 1 cốc)
        var colorList = new List<TrayColor>();
        foreach (var kv in colorCount)
        {
            for (int i = 0; i < kv.Value; i++)
                colorList.Add(kv.Key);
        }

        // 4. Shuffle Fisher-Yates
        for (int i = colorList.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (colorList[i], colorList[j]) = (colorList[j], colorList[i]);
        }

        // 5. Tạo Cup GameObject và đưa vào CupQueue
        var cups = new List<Cup>(colorList.Count);
        foreach (TrayColor color in colorList)
        {
            GameObject prefab = GetPrefab(color);
            if (prefab == null)
            {
                Debug.LogError($"[CupSpawner] Thiếu prefab cho màu {color}");
                continue;
            }

            GameObject go = Instantiate(prefab);
            go.SetActive(false); // CupQueue sẽ kích hoạt khi cần hiển thị

            Cup cup = go.GetComponent<Cup>();
            if (cup == null)
            {
                Debug.LogError($"[CupSpawner] Prefab {prefab.name} thiếu component Cup");
                continue;
            }

            cup.Color = color;
            cups.Add(cup);
        }

        Debug.Log($"[CupSpawner] Spawned {cups.Count} cups: {string.Join(", ", colorCount)}");

        // 6. Nạp vào CupQueue
        CupQueue.Instance.Initialize(cups);
    }

    // -------------------------------------------------------

    private GameObject GetPrefab(TrayColor color)
    {
        return color switch
        {
            TrayColor.Blue => _cupBluePrefab,
            TrayColor.Pink => _cupPinkPrefab,
            TrayColor.Red => _cupRedPrefab,
            TrayColor.Yellow => _cupYellowPrefab,
            TrayColor.Green => _cupGreenPrefab,
            TrayColor.Purple => _cupPurplePrefab,
            TrayColor.Orange => _cupOrangePrefab,
            TrayColor.Brown => _cupBrownPrefab,
            _ => null
        };
    }
}