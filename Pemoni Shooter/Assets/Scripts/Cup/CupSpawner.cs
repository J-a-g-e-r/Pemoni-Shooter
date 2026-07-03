using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gắn vào GameObject bất kỳ trong scene (ví dụ: GameManager).
/// Tính tổng capacity của tất cả Tray, tạo đúng số lượng Cup tương ứng.
/// Mặc định shuffle ngẫu nhiên; nếu bật Tutorial Mode thì xếp theo thứ tự
/// màu cố định (_tutorialOrder), phần dư còn lại mới random.
/// </summary>
public class CupSpawner : MonoBehaviour
{
    public static CupSpawner Instance { get; private set; }

    [Header("Cup Prefabs – đặt đúng thứ tự enum TrayColor")]
    [SerializeField] private GameObject _cupBluePrefab;
    [SerializeField] private GameObject _cupPinkPrefab;
    [SerializeField] private GameObject _cupRedPrefab;
    [SerializeField] private GameObject _cupYellowPrefab;
    [SerializeField] private GameObject _cupGreenPrefab;
    [SerializeField] private GameObject _cupPurplePrefab;
    [SerializeField] private GameObject _cupOrangePrefab;
    [SerializeField] private GameObject _cupBrownPrefab;

    [Header("Tutorial Mode")]
    [Tooltip("Bật để xếp cốc theo đúng thứ tự định trước (dùng cho level tutorial). Tắt = random như cũ.")]
    [SerializeField] private bool _useTutorialOrder = false;

    [Tooltip("Thứ tự MÀU cốc mong muốn (chỉ định thứ tự, KHÔNG định số lượng — số lượng mỗi màu vẫn lấy từ tổng Capacity của các Tray). " +
             "Nếu một màu xuất hiện nhiều lần trong list, mỗi lần sẽ 'tiêu' 1 cốc của màu đó theo đúng vị trí. " +
             "Phần cốc còn dư (không được liệt kê đủ) sẽ được random và nối vào cuối hàng.")]
    [SerializeField] private List<TrayColor> _tutorialOrder = new();

    // -------------------------------------------------------

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // Nếu Scene không sử dụng LevelLoader (xếp khay thủ công sẵn trên Scene),
        // thì CupSpawner tự động spawn cốc ngay khi bắt đầu.
        LevelLoader loader = FindObjectOfType<LevelLoader>();
        if (loader == null)
        {
            SpawnForLevel();
        }
    }

    /// <summary>
    /// Tính capacity từ tất cả Tray hiện có, tạo danh sách Cup đúng số lượng + màu.
    /// Nếu _useTutorialOrder = true: xếp theo thứ tự cố định trong _tutorialOrder
    /// (số lượng mỗi màu vẫn theo capacity thật, chỉ thứ tự là cố định; phần dư random).
    /// Nếu false: random toàn bộ như cũ.
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

        // 3 + 4. Xây danh sách thứ tự màu: Tutorial (cố định) hoặc Normal (random)
        List<TrayColor> colorList = _useTutorialOrder
            ? BuildTutorialOrder(colorCount)
            : BuildShuffledOrder(colorCount);

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

    /// <summary>
    /// Logic cũ: tạo list màu theo capacity rồi shuffle Fisher-Yates.
    /// </summary>
    private List<TrayColor> BuildShuffledOrder(Dictionary<TrayColor, int> colorCount)
    {
        var colorList = new List<TrayColor>();
        foreach (var kv in colorCount)
        {
            for (int i = 0; i < kv.Value; i++)
                colorList.Add(kv.Key);
        }

        for (int i = colorList.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (colorList[i], colorList[j]) = (colorList[j], colorList[i]);
        }

        return colorList;
    }

    /// <summary>
    /// Tutorial mode: giữ đúng thứ tự màu đã định nghĩa trong _tutorialOrder.
    /// Số lượng mỗi màu vẫn lấy từ colorCount (capacity thực tế của Tray).
    /// _tutorialOrder chỉ quyết định THỨ TỰ xuất hiện, mỗi lần dùng sẽ trừ dần
    /// số lượng còn lại của màu đó. Cốc dư ra (không được liệt kê đủ) sẽ được
    /// random và nối vào cuối hàng.
    /// </summary>
    private List<TrayColor> BuildTutorialOrder(Dictionary<TrayColor, int> colorCount)
    {
        // Copy ra để trừ dần mà không ảnh hưởng dict gốc
        var remaining = new Dictionary<TrayColor, int>(colorCount);
        var result = new List<TrayColor>();

        foreach (TrayColor color in _tutorialOrder)
        {
            if (remaining.TryGetValue(color, out int left) && left > 0)
            {
                result.Add(color);
                remaining[color] = left - 1;
            }
            else
            {
                Debug.LogWarning($"[CupSpawner] Tutorial order có màu {color} nhưng không còn cốc màu này trong Tray (hết hoặc không tồn tại). Bỏ qua entry này.");
            }
        }

        // Phần dư: random rồi nối vào cuối
        var leftoverList = new List<TrayColor>();
        foreach (var kv in remaining)
        {
            for (int i = 0; i < kv.Value; i++)
                leftoverList.Add(kv.Key);
        }

        for (int i = leftoverList.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (leftoverList[i], leftoverList[j]) = (leftoverList[j], leftoverList[i]);
        }

        result.AddRange(leftoverList);
        return result;
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