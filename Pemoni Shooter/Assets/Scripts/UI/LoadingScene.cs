using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadingScene : MonoBehaviour
{
    [SerializeField] private string nextScene = "MainScene";
    [SerializeField] private float loadingTime = 5f;

    private void Start()
    {
        StartCoroutine(LoadNextScene());
    }

    private IEnumerator LoadNextScene()
    {
        yield return new WaitForSeconds(loadingTime);

        SceneManager.LoadScene(nextScene);
    }
}