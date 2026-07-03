using System.Collections.Generic;
using UnityEngine;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]
public struct TrayPrefabMapping
{
    public TrayType type;
    public TrayColor color;
    public GameObject prefab;
}

public class LevelLoader : MonoBehaviour
{
    [Header("Level Data")]
    public LevelData levelData;
    public Transform boardParent;
    [SerializeField] private TextMeshProUGUI _levelNameText;
    [SerializeField] private TextMeshProUGUI _winLevelText;
    [SerializeField] private TextMeshProUGUI _loseLevelText;



    [Header("Prefab Configurations")]
    public List<TrayPrefabMapping> prefabMappings;

    private void Start()
    {
        if(LevelManager.Instance != null)
            levelData = LevelManager.Instance.GetCurrentLevelData();
        LoadLevel();
    }

    public void LoadLevel()
    {
        if (levelData == null) return;

        // 1. Clear any existing trays under boardParent
        foreach (Transform child in boardParent)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }

        // 2. Spawn Trays from data
        foreach (var data in levelData.trays)
        {
            GameObject prefab = GetPrefab(data.trayType, data.trayColor);
            if (prefab != null)
            {
                GameObject trayObj = Instantiate(prefab, boardParent);
                trayObj.transform.localPosition = data.localPosition;

                Tray tray = trayObj.GetComponent<Tray>();
                if (tray != null)
                {
                    tray.TrayType = data.trayType;
                    tray.TrayColor = data.trayColor;
                    tray.OriginCell = data.originCell;
                    tray.Layer = data.layer;
                    tray.ApplyGridVisual(); // Ensure visual/sorting order is updated
                }
            }
            else
            {
                Debug.LogError($"[LevelLoader] Missing prefab mapping for: {data.trayType} - {data.trayColor}");
            }
        }

        // 3. Rebuild grid layout logic
        if (GridMapManager.Instance != null)
        {
            GridMapManager.Instance.BuildGrid();
            GridMapManager.Instance.RefreshCoveredState();
        }

        // 4. Spawn Cups after Trays are loaded and registered in the grid
        if (CupSpawner.Instance != null)
        {
            CupSpawner.Instance.SpawnForLevel();
        }


        // 5. Update UI
        if (_levelNameText != null)
            _levelNameText.text = levelData.levelName;
        if (_winLevelText != null)
            _winLevelText.text = levelData.levelName;
        if (_loseLevelText != null)
            _loseLevelText.text = levelData.levelName;
    }

    private GameObject GetPrefab(TrayType type, TrayColor color)
    {
        foreach (var map in prefabMappings)
        {
            if (map.type == type && map.color == color)
                return map.prefab;
        }
        return null;
    }

    #if UNITY_EDITOR
    [ContextMenu("Bake Scene to Level Data")]
    public void BakeSceneToLevelData()
    {
        if (levelData == null)
        {
            Debug.LogError("[LevelLoader] Please assign a LevelData asset before baking!");
            return;
        }

        levelData.trays.Clear();
        
        // Find all active trays in the scene
        Tray[] activeTrays = FindObjectsOfType<Tray>();

        foreach (Tray t in activeTrays)
        {
            // Skip runtime spawned objects under boardParent if we happen to click this during play mode
            if (t.transform.parent == boardParent && Application.isPlaying)
                continue;

            TraySpawnData data = new TraySpawnData
            {
                trayType = t.TrayType,
                trayColor = t.TrayColor,
                originCell = t.OriginCell,
                layer = t.Layer,
                localPosition = t.transform.localPosition
            };

            levelData.trays.Add(data);
        }

        EditorUtility.SetDirty(levelData);
        AssetDatabase.SaveAssets();
        Debug.Log($"[LevelLoader] Successfully baked {levelData.trays.Count} trays into: {levelData.name}");
    }
    #endif
}
