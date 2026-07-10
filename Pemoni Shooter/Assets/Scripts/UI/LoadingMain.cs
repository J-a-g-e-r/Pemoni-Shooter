using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class LoadingMain : MonoBehaviour
{
    [SerializeField] private Slider slider;
    [SerializeField] private float loadingTime = 2f;

    private void OnEnable()
    {
        slider.value = 0;
        StartCoroutine(Loading());
    }

    private IEnumerator Loading()
    {
        float timer = 0;

        while (timer < loadingTime)
        {
            timer += Time.deltaTime;

            slider.value = Mathf.Clamp01(timer / loadingTime);

            yield return null;
        }

        slider.value = 1;

        gameObject.SetActive(false);
    }
}