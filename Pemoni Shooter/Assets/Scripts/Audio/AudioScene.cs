using UnityEngine;
using AudioSystem;

public class AudioScene : MonoBehaviour
{
    [SerializeField] private string _musicName = "bgm_main";

    private void Start()
    {
        AudioManager.Instance.PlayMusic(_musicName);
    }
}