using UnityEngine;

namespace AudioSystem
{
    [CreateAssetMenu(fileName = "NewSound", menuName = "Audio/Sound Data")]
    public class SoundData : ScriptableObject
    {
        [Header("Identification")]
        public string id;

        [Header("Clip")]
        public AudioClip clip;

        [Header("Playback Settings")]
        [Range(0f, 1f)] public float volume = 1f;
        [Range(0.1f, 3f)] public float pitch = 1f;
        public bool loop = false;

        [Header("Randomization (optional, thường dùng cho SFX)")]
        [Tooltip("Random thêm/bớt volume trong khoảng +/- giá trị này mỗi lần phát")]
        [Range(0f, 0.5f)] public float volumeVariance = 0f;
        [Tooltip("Random thêm/bớt pitch trong khoảng +/- giá trị này mỗi lần phát")]
        [Range(0f, 0.5f)] public float pitchVariance = 0f;

        public float GetRandomVolume()
        {
            return Mathf.Clamp01(volume + Random.Range(-volumeVariance, volumeVariance));
        }

        public float GetRandomPitch()
        {
            return Mathf.Clamp(pitch + Random.Range(-pitchVariance, pitchVariance), 0.1f, 3f);
        }
    }
}