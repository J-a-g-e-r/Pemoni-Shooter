using System.Collections.Generic;
using TMPro;
using UnityEngine;

[System.Serializable]
public class TraySpawnData
{
    public TrayType trayType;
    public TrayColor trayColor;
    public Vector2Int originCell;
    public int layer;
    public Vector3 localPosition;
}

[CreateAssetMenu(fileName = "NewLevelData", menuName = "Game/Level Data")]
public class LevelData : ScriptableObject
{
    public List<TraySpawnData> trays = new List<TraySpawnData>();
    public string levelName;
}
