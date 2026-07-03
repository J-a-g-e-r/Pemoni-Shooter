using UnityEngine;
using AudioSystem;

public class SettingsManager : MonoBehaviour
{
    [SerializeField] private SettingToggleItem _musicToggle;
    [SerializeField] private SettingToggleItem _sfxToggle;
    [SerializeField] private SettingToggleItem _vibrateToggle;

    private void Start()
    {
        bool musicOn = PlayerPrefs.GetInt("setting_music", 1) == 1;
        bool sfxOn = PlayerPrefs.GetInt("setting_sfx", 1) == 1;
        ApplyMusic(musicOn);
        ApplySFX(sfxOn);
        _musicToggle.OnValueChanged += ApplyMusic;
        _sfxToggle.OnValueChanged += ApplySFX;
        _vibrateToggle.OnValueChanged += ApplyVibrate;
    }

    private void ApplyMusic(bool isOn)
    {
        AudioManager.Instance.SetMusicVolume(isOn ? 1f : 0f);
    }

    private void ApplySFX(bool isOn)
    {
        AudioManager.Instance.SetSFXVolume(isOn ? 1f : 0f);
    }

    private void ApplyVibrate(bool isOn)
    {
        // Chưa có code rung trong project — chỉ lưu pref, dùng sau khi gọi Handheld.Vibrate()
        PlayerPrefs.SetInt("setting_vibrate", isOn ? 1 : 0);
    }
}