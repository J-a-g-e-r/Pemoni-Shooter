using System.Collections.Generic;
using UnityEngine;

namespace AudioSystem
{
    [CreateAssetMenu(fileName = "SoundDatabase", menuName = "Audio/Sound Database")]
    public class SoundDatabase : ScriptableObject
    {
        [Header("Sound Effects")]
        public List<SoundData> sfxList = new List<SoundData>();

        [Header("Music / BGM")]
        public List<SoundData> musicList = new List<SoundData>();

        private Dictionary<string, SoundData> _sfxDict;
        private Dictionary<string, SoundData> _musicDict;

        private void BuildDictionaries()
        {
            _sfxDict = new Dictionary<string, SoundData>();
            foreach (var s in sfxList)
            {
                if (s == null || string.IsNullOrEmpty(s.id)) continue;

                if (!_sfxDict.ContainsKey(s.id))
                    _sfxDict.Add(s.id, s);
                else
                    Debug.LogWarning($"[SoundDatabase] Trùng SFX id: \"{s.id}\" — kiểm tra lại database.");
            }

            _musicDict = new Dictionary<string, SoundData>();
            foreach (var m in musicList)
            {
                if (m == null || string.IsNullOrEmpty(m.id)) continue;

                if (!_musicDict.ContainsKey(m.id))
                    _musicDict.Add(m.id, m);
                else
                    Debug.LogWarning($"[SoundDatabase] Trùng Music id: \"{m.id}\" — kiểm tra lại database.");
            }
        }

        public SoundData GetSFX(string id)
        {
            if (_sfxDict == null) BuildDictionaries();

            if (_sfxDict.TryGetValue(id, out var data)) return data;

            Debug.LogWarning($"[SoundDatabase] Không tìm thấy SFX id: \"{id}\".");
            return null;
        }

        public SoundData GetMusic(string id)
        {
            if (_musicDict == null) BuildDictionaries();

            if (_musicDict.TryGetValue(id, out var data)) return data;

            Debug.LogWarning($"[SoundDatabase] Không tìm thấy Music id: \"{id}\".");
            return null;
        }

        /// <summary>Gọi lại nếu thay đổi list lúc runtime (ví dụ load thêm sound từ DLC/AssetBundle).</summary>
        public void RefreshDictionaries() => BuildDictionaries();
    }
}