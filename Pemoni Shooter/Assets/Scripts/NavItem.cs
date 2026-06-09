using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NavItem : MonoBehaviour
{
    [Header("References")]
    public RectTransform pill;
    public RectTransform iconTransform;
    public CanvasGroup labelGroup;

    [Header("Settings")]
    public float activeFlexWidth = 260f;
    public float inactiveFlexWidth = 80f;
    public float iconLiftY = 120f;
    public float animDuration = 0.2f;

    private LayoutElement layoutElement;
    private Coroutine animCoroutine;
    private bool isActive = false;

    void Awake()
    {
        layoutElement = GetComponent<LayoutElement>();
        // Đặt trạng thái ban đầu
        pill.localScale = new Vector3(0f, 1f, 1f);
        if (labelGroup) labelGroup.alpha = 0f;
    }

    // Kích hoạt hoặc hủy kích hoạt item với animation, gọi ở NavigationBarManager :))
    public void SetActive(bool active, bool instant = false)
    {
        isActive = active;
        if (animCoroutine != null) StopCoroutine(animCoroutine);
        animCoroutine = StartCoroutine(AnimateState(active, instant));
    }


    // Thực hiện animation mở rộng width, nâng icon và fade in label
    IEnumerator AnimateState(bool active, bool instant)
    {
        float startWidth = layoutElement.preferredWidth;
        float targetWidth = active ? activeFlexWidth : inactiveFlexWidth;

        Vector3 startIconPos = iconTransform.anchoredPosition;
        Vector3 targetIconPos = new Vector3(
            iconTransform.anchoredPosition.x,
            active ? iconLiftY : 0f, 0f);

        Vector3 startIconScale = iconTransform.localScale;
        Vector3 targetIconScale = active ? Vector3.one * 1.2f : Vector3.one;

        float startPillScaleX = pill.localScale.x;
        float targetPillScaleX = active ? 1f : 0f;

        float startAlpha = labelGroup ? labelGroup.alpha : 0f;
        float targetAlpha = active ? 1f : 0f;

        float elapsed = 0f;
        while (elapsed < animDuration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = elapsed / animDuration;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / animDuration);

            layoutElement.preferredWidth = Mathf.Lerp(startWidth, targetWidth, t);
            iconTransform.anchoredPosition = Vector3.Lerp(startIconPos, targetIconPos, t);
            iconTransform.localScale = Vector3.Lerp(startIconScale, targetIconScale, t);

            float scaleX = Mathf.Lerp(startPillScaleX, targetPillScaleX, t);
            pill.localScale = new Vector3(scaleX, 1f, 1f);

            float t_label = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(normalizedTime * 2f));

            if (labelGroup) labelGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t_label);

            yield return null;
        }

        layoutElement.preferredWidth = targetWidth;
    }
}