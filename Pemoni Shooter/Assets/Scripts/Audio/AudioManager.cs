using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace AudioSystem
{
    /// <summary>
    /// Singleton quản lý toàn bộ âm thanh trong game: SFX (phát chồng qua pool AudioSource)
    /// và Music/BGM (crossfade mượt giữa 2 AudioSource).
    ///
    /// Cách dùng:
    ///   AudioManager.Instance.PlaySFX("sfx_jump");
    ///   AudioManager.Instance.PlayMusic("bgm_main_menu");
    ///
    /// Setup:
    ///   1. Tạo các asset SoundData (Create > Audio > Sound Data) cho từng âm thanh.
    ///   2. Tạo 1 asset SoundDatabase (Create > Audio > Sound Database), kéo các SoundData vào
    ///      list sfxList / musicList tương ứng.
    ///   3. Tạo 1 GameObject trong scene đầu tiên (vd: "AudioManager"), add component này,
    ///      kéo SoundDatabase, MainAudio mixer và các group BGM/SFX vào Inspector.
    /// </summary>
    [DisallowMultipleComponent]
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Database")]
        [SerializeField] private SoundDatabase soundDatabase;

        [Header("Audio Mixer")]
        [SerializeField] private AudioMixer mainMixer;
        [SerializeField] private AudioMixerGroup bgmGroup;
        [SerializeField] private AudioMixerGroup sfxGroup;

        [Header("SFX Pool Settings")]
        [SerializeField] private int sfxPoolSize = 12;

        [Header("Music Settings")]
        [SerializeField] private float defaultMusicFadeDuration = 1f;

        [Header("Volume (0 - 1)")]
        [SerializeField, Range(0f, 1f)] private float masterSFXVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float masterMusicVolume = 1f;

        // ----- SFX -----
        private readonly List<AudioSource> _sfxPool = new List<AudioSource>();
        private Transform _sfxPoolParent;

        // ----- Music -----
        private AudioSource _musicSourceA;
        private AudioSource _musicSourceB;
        private AudioSource _activeMusicSource;
        private Coroutine _musicFadeCoroutine;
        private string _currentMusicId;

        private const string PrefSfxVolume = "audio_sfx_volume";
        private const string PrefMusicVolume = "audio_music_volume";
        private const string MixerBgmParam = "BGM";
        private const string MixerSfxParam = "SFX";
        private const float MinDb = -80f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadVolumeSettings();
            BuildSFXPool();
            BuildMusicSources();
        }

        #region Setup

        private void BuildSFXPool()
        {
            var poolGO = new GameObject("SFX_Pool");
            poolGO.transform.SetParent(transform);
            _sfxPoolParent = poolGO.transform;

            for (int i = 0; i < sfxPoolSize; i++)
            {
                _sfxPool.Add(CreateSFXSource(i));
            }
        }

        private AudioSource CreateSFXSource(int index)
        {
            var go = new GameObject($"SFX_Source_{index}");
            go.transform.SetParent(_sfxPoolParent);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.outputAudioMixerGroup = sfxGroup;
            return src;
        }

        private void BuildMusicSources()
        {
            var musicGO = new GameObject("Music_Sources");
            musicGO.transform.SetParent(transform);

            _musicSourceA = musicGO.AddComponent<AudioSource>();
            _musicSourceA.playOnAwake = false;
            _musicSourceA.loop = true;
            _musicSourceA.outputAudioMixerGroup = bgmGroup;

            _musicSourceB = musicGO.AddComponent<AudioSource>();
            _musicSourceB.playOnAwake = false;
            _musicSourceB.loop = true;
            _musicSourceB.outputAudioMixerGroup = bgmGroup;

            _activeMusicSource = _musicSourceA;
        }

        #endregion

        #region SFX

        /// <summary>Phát SFX theo id khai báo trong SoundDatabase.</summary>
        public void PlaySFX(string id)
        {
            PlaySFX(id, 1f, 1f);
        }

        /// <summary>Phát SFX với hệ số nhân thêm cho volume/pitch (vd: combo hit mạnh hơn thì pitch cao hơn).</summary>
        public void PlaySFX(string id, float volumeScale, float pitchScale)
        {
            if (soundDatabase == null) return;

            var data = soundDatabase.GetSFX(id);
            if (data == null || data.clip == null) return;

            var src = GetAvailableSFXSource();
            if (src == null) return;

            src.clip = data.clip;
            src.volume = data.GetRandomVolume() * volumeScale;
            src.pitch = data.GetRandomPitch() * pitchScale;
            src.loop = data.loop;
            src.Play();
        }

        /// <summary>Dừng toàn bộ SFX đang phát (vd: khi pause game).</summary>
        public void StopAllSFX()
        {
            foreach (var src in _sfxPool)
            {
                if (src.isPlaying) src.Stop();
            }
        }

        private AudioSource GetAvailableSFXSource()
        {
            foreach (var src in _sfxPool)
            {
                if (!src.isPlaying) return src;
            }

            // Hết pool (nhiều SFX phát cùng lúc) -> tự mở rộng thêm 1 source để tránh mất tiếng.
            var newSrc = CreateSFXSource(_sfxPool.Count);
            _sfxPool.Add(newSrc);
            Debug.LogWarning("[AudioManager] SFX pool đã hết, tự mở rộng thêm 1 source. " +
                              "Nếu việc này xảy ra thường xuyên, hãy tăng sfxPoolSize.");
            return newSrc;
        }

        #endregion

        #region Music

        /// <summary>Phát nhạc nền theo id. Mặc định crossfade mượt sang bài mới.</summary>
        public void PlayMusic(string id, bool fade = true, float fadeDuration = -1f)
        {
            if (soundDatabase == null) return;
            if (_currentMusicId == id && _activeMusicSource.isPlaying) return;

            var data = soundDatabase.GetMusic(id);
            if (data == null || data.clip == null) return;

            _currentMusicId = id;
            float duration = fadeDuration >= 0f ? fadeDuration : defaultMusicFadeDuration;

            if (_musicFadeCoroutine != null) StopCoroutine(_musicFadeCoroutine);

            _musicFadeCoroutine = fade
                ? StartCoroutine(CrossfadeMusic(data, duration))
                : StartCoroutine(PlayMusicImmediate(data));
        }

        private IEnumerator PlayMusicImmediate(SoundData data)
        {
            _activeMusicSource.Stop();
            _activeMusicSource.clip = data.clip;
            _activeMusicSource.volume = data.volume;
            _activeMusicSource.pitch = data.pitch;
            _activeMusicSource.loop = data.loop;
            _activeMusicSource.Play();
            _musicFadeCoroutine = null;
            yield break;
        }

        private IEnumerator CrossfadeMusic(SoundData newData, float duration)
        {
            var fromSource = _activeMusicSource;
            var toSource = (_activeMusicSource == _musicSourceA) ? _musicSourceB : _musicSourceA;

            toSource.clip = newData.clip;
            toSource.volume = 0f;
            toSource.pitch = newData.pitch;
            toSource.loop = newData.loop;
            toSource.Play();

            float targetVolume = newData.volume;
            float fromStartVolume = fromSource.volume;
            float t = 0f;

            while (t < duration)
            {
                t += Time.deltaTime;
                float ratio = duration > 0f ? t / duration : 1f;
                toSource.volume = Mathf.Lerp(0f, targetVolume, ratio);
                fromSource.volume = Mathf.Lerp(fromStartVolume, 0f, ratio);
                yield return null;
            }

            fromSource.Stop();
            toSource.volume = targetVolume;
            _activeMusicSource = toSource;
            _musicFadeCoroutine = null;
        }

        /// <summary>Dừng nhạc nền hiện tại (có thể fade out mượt).</summary>
        public void StopMusic(bool fade = true, float fadeDuration = -1f)
        {
            float duration = fadeDuration >= 0f ? fadeDuration : defaultMusicFadeDuration;
            _currentMusicId = null;

            if (_musicFadeCoroutine != null) StopCoroutine(_musicFadeCoroutine);

            _musicFadeCoroutine = fade
                ? StartCoroutine(FadeOutAndStop(_activeMusicSource, duration))
                : StartCoroutine(StopImmediate(_activeMusicSource));
        }

        private IEnumerator StopImmediate(AudioSource src)
        {
            src.Stop();
            _musicFadeCoroutine = null;
            yield break;
        }

        private IEnumerator FadeOutAndStop(AudioSource src, float duration)
        {
            float startVolume = src.volume;
            float t = 0f;

            while (t < duration)
            {
                t += Time.deltaTime;
                src.volume = Mathf.Lerp(startVolume, 0f, duration > 0f ? t / duration : 1f);
                yield return null;
            }

            src.Stop();
            _musicFadeCoroutine = null;
        }

        public void PauseMusic() => _activeMusicSource.Pause();
        public void ResumeMusic() => _activeMusicSource.UnPause();
        public bool IsMusicPlaying => _activeMusicSource != null && _activeMusicSource.isPlaying;
        public string CurrentMusicId => _currentMusicId;

        #endregion

        #region Volume Control

        public void SetSFXVolume(float volume)
        {
            masterSFXVolume = Mathf.Clamp01(volume);
            ApplyMixerVolume(MixerSfxParam, masterSFXVolume);
            PlayerPrefs.SetFloat(PrefSfxVolume, masterSFXVolume);
        }

        public void SetMusicVolume(float volume)
        {
            masterMusicVolume = Mathf.Clamp01(volume);
            ApplyMixerVolume(MixerBgmParam, masterMusicVolume);
            PlayerPrefs.SetFloat(PrefMusicVolume, masterMusicVolume);
        }

        public float GetSFXVolume() => masterSFXVolume;
        public float GetMusicVolume() => masterMusicVolume;

        private void LoadVolumeSettings()
        {
            masterSFXVolume = PlayerPrefs.GetFloat(PrefSfxVolume, masterSFXVolume);
            masterMusicVolume = PlayerPrefs.GetFloat(PrefMusicVolume, masterMusicVolume);
            ApplyMixerVolume(MixerSfxParam, masterSFXVolume);
            ApplyMixerVolume(MixerBgmParam, masterMusicVolume);
        }

        private void ApplyMixerVolume(string param, float linearVolume)
        {
            if (mainMixer == null) return;
            mainMixer.SetFloat(param, LinearToDb(linearVolume));
        }

        private static float LinearToDb(float linear)
        {
            return linear > 0.0001f ? Mathf.Log10(linear) * 20f : MinDb;
        }

        #endregion
    }
}