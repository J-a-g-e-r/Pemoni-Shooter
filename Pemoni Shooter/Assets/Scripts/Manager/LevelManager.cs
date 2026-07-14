using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : SingletonPersistent<LevelManager>
{
    private const string PrefKey = "current_level";

    [SerializeField] private string tutorialScene = "Level_1";
    [SerializeField] private string gameplayScene = "Gameplay"; // đổi tên từ Level_2

    [Header("Level 2 trở đi — Level 1 dùng scene tutorial riêng")]
    [SerializeField] private LevelData[] levels; // [0]=Level 2, [1]=Level 3, ...

    public int CurrentLevel { get; private set; }

    public override void Awake()
    {
        base.Awake();
        CurrentLevel = PlayerPrefs.GetInt(PrefKey, 1);
    }

    public string GetLevelDisplayText() => $"Level {CurrentLevel}";

    public LevelData GetCurrentLevelData()
    {
        // Level 1 không dùng LevelData trong scene Gameplay
        return levels[CurrentLevel - 2];
    }

    public void LoadCurrentLevel()
    {
        if (CurrentLevel == 1)
            SceneManager.LoadScene(tutorialScene);
        else
            SceneManager.LoadScene(gameplayScene);
    }

    public void TryLoadCurrentLevel()
    {
        if (HeartManager.Instance != null && !HeartManager.Instance.CanPlay())
        {
            NavigationBarManager.Instance.SelectItem(1); // Chuyển sang tab SHop
            return;
        }
        LoadCurrentLevel();
    }

        public void CompleteCurrentLevel()
    {
        int maxLevel = 1 + levels.Length;
        if (CurrentLevel < maxLevel)
            CurrentLevel++;

        PlayerPrefs.SetInt(PrefKey, CurrentLevel);
        PlayerPrefs.Save();

        MissionManager.Instance?.AddProgress(MissionType.CompleteLevels, 1);
    }

    public void LoadMainScene() => SceneManager.LoadScene("MainScene");
}