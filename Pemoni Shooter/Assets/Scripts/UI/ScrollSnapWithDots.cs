using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Gắn script này vào GameObject có ScrollRect (Horizontal).
/// - content: Content của ScrollRect (kéo từ ScrollRect.content vào hoặc để trống, script tự lấy)
/// - dotsContainer: Transform chứa các dot (có Horizontal Layout Group)
/// - dotPrefab: Prefab 1 Image hình tròn dùng làm dot
/// </summary>
[RequireComponent(typeof(ScrollRect))]
public class ScrollSnapWithDots : MonoBehaviour, IBeginDragHandler, IEndDragHandler
{
    [Header("References")]
    public RectTransform content;
    public Transform dotsContainer;
    public GameObject dotPrefab;

    [Header("Dot Colors")]
    public Color activeColor = Color.white;
    public Color inactiveColor = new Color(1f, 1f, 1f, 0.4f);

    [Header("Settings")]
    [Tooltip("Tốc độ trượt về đúng trang sau khi thả tay")]
    public float snapSpeed = 10f;
    [Tooltip("Số pixel kéo tối thiểu để chuyển trang")]
    public float swipeThreshold = 50f;
    [Tooltip("Tự động chạy carousel (giây/trang). 0 = tắt")]
    public float autoPlayInterval = 0f;

    private ScrollRect scrollRect;
    private List<RectTransform> pages = new List<RectTransform>();
    private List<Image> dots = new List<Image>();
    private int currentPage = 0;
    private float pageWidth;
    private bool isDragging = false;
    private float dragStartX;
    private float targetPosX;
    private bool isSnapping = false;
    private float autoPlayTimer = 0f;

    void Awake()
    {
        scrollRect = GetComponent<ScrollRect>();
        if (content == null) content = scrollRect.content;
    }

    void Start()
    {
        StartCoroutine(InitAfterLayout());
    }

    // QUAN TRỌNG: Horizontal Layout Group / Content Size Fitter chưa rebuild xong
    // ngay tại Start(), nên phải đợi 1 frame (hoặc ForceRebuildLayoutImmediate)
    // rồi mới đọc kích thước/ vị trí của các trang. Nếu đọc sớm, pageWidth sẽ
    // sai (bằng size mặc định của prefab) -> snap lệch, hiện ra "lưng chừng"
    // giữa 2 trang giống lỗi bạn đang gặp.
    IEnumerator InitAfterLayout()
    {
        yield return new WaitForEndOfFrame();
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);

        SetupPages();
        SetupDots();
        UpdateDots();
    }

    void SetupPages()
    {
        pages.Clear();
        foreach (Transform child in content)
        {
            RectTransform rt = child as RectTransform;
            if (rt != null) pages.Add(rt);
        }

        if (pages.Count >= 2)
        {
            // Tính pageWidth bằng khoảng cách thực tế giữa 2 trang liền kề.
            // Cách này tự động cộng luôn Spacing của Horizontal Layout Group
            // (nếu có), chính xác hơn nhiều so với chỉ lấy rect.width.
            pageWidth = Mathf.Abs(pages[1].anchoredPosition.x - pages[0].anchoredPosition.x);
        }
        else if (pages.Count == 1)
        {
            pageWidth = pages[0].rect.width;
        }
    }

    void SetupDots()
    {
        if (dotsContainer == null || dotPrefab == null) return;

        foreach (Transform child in dotsContainer)
            Destroy(child.gameObject);

        dots.Clear();
        for (int i = 0; i < pages.Count; i++)
        {
            GameObject dot = Instantiate(dotPrefab, dotsContainer);
            Image img = dot.GetComponent<Image>();
            dots.Add(img);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;
        isSnapping = false;
        autoPlayTimer = 0f;
        dragStartX = content.anchoredPosition.x;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
        float dragDelta = content.anchoredPosition.x - dragStartX;

        if (Mathf.Abs(dragDelta) > swipeThreshold)
        {
            if (dragDelta > 0 && currentPage > 0)
                currentPage--;
            else if (dragDelta < 0 && currentPage < pages.Count - 1)
                currentPage++;
        }

        SnapToPage(currentPage);
    }

    public void SnapToPage(int pageIndex)
    {
        currentPage = Mathf.Clamp(pageIndex, 0, pages.Count - 1);
        targetPosX = -currentPage * pageWidth;
        isSnapping = true;
        UpdateDots();
    }

    void Update()
    {
        // Snap mượt về đúng trang
        if (isSnapping && !isDragging)
        {
            Vector2 pos = content.anchoredPosition;
            pos.x = Mathf.Lerp(pos.x, targetPosX, Time.deltaTime * snapSpeed);
            content.anchoredPosition = pos;

            if (Mathf.Abs(pos.x - targetPosX) < 0.5f)
            {
                pos.x = targetPosX;
                content.anchoredPosition = pos;
                isSnapping = false;
            }
        }

        // Auto play (tự chuyển trang sau N giây, giống banner quảng cáo)
        if (autoPlayInterval > 0f && !isDragging && pages.Count > 1)
        {
            autoPlayTimer += Time.deltaTime;
            if (autoPlayTimer >= autoPlayInterval)
            {
                autoPlayTimer = 0f;
                int next = (currentPage + 1) % pages.Count;
                SnapToPage(next);
            }
        }
    }

    void UpdateDots()
    {
        for (int i = 0; i < dots.Count; i++)
        {
            if (dots[i] != null)
                dots[i].color = (i == currentPage) ? activeColor : inactiveColor;
        }
    }

    // Có thể gọi 2 hàm này từ Button (ví dụ nút mũi tên trái/phải nếu muốn)
    public void NextPage() => SnapToPage(currentPage + 1);
    public void PrevPage() => SnapToPage(currentPage - 1);
}