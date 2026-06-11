using System.Collections.Generic;
using UnityEngine;

public class GridMapManager : MonoBehaviour
{
    public static GridMapManager Instance;

    private readonly Dictionary<Vector3Int, Tray> _grid =
        new Dictionary<Vector3Int, Tray>();

    private readonly List<Tray> _allTrays =
        new List<Tray>();

    private int _maxLayer;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        BuildGrid();
        RefreshCoveredState();
    }

    #region Build Grid

    public void BuildGrid()
    {
        _grid.Clear();
        _allTrays.Clear();

        _maxLayer = 0;

        Tray[] trays =
            FindObjectsOfType<Tray>();

        foreach (Tray tray in trays)
        {
            RegisterTray(tray);
        }
    }

    private void RegisterTray(Tray tray)
    {
        _allTrays.Add(tray);

        _maxLayer =
            Mathf.Max(_maxLayer, tray.Layer);

        var shape =
            TrayShapeUtility.GetShape(tray.TrayType);

        foreach (var offset in shape)
        {
            Vector2Int cell =
                tray.OriginCell + offset;

            Vector3Int key =
                new Vector3Int(
                    cell.x,
                    cell.y,
                    tray.Layer);

            if (_grid.ContainsKey(key))
            {
                Debug.LogError(
                    $"Grid conflict at {key}");
            }

            _grid[key] = tray;
        }
    }

    #endregion

    #region Covered Check

    public bool IsCovered(Tray tray)
    {
        var shape =
            TrayShapeUtility.GetShape(tray.TrayType);

        foreach (var offset in shape)
        {
            Vector2Int cell =
                tray.OriginCell + offset;

            for (int layer = tray.Layer + 1;
                 layer <= _maxLayer;
                 layer++)
            {
                Vector3Int key =
                    new Vector3Int(
                        cell.x,
                        cell.y,
                        layer);

                if (_grid.ContainsKey(key))
                {
                    return true;
                }
            }
        }

        return false;
    }

    public void RefreshCoveredState()
    {
        foreach (var tray in _allTrays)
        {
            tray.IsCovered =
                IsCovered(tray);
        }
    }

    #endregion

    #region Remove

    /// Xóa tray khỏi grid + danh sách nhưng KHÔNG Destroy GameObject.
    /// Dùng khi tray đang bay vào slot (Destroy sau khi animation kết thúc).
    public void UnregisterTray(Tray tray)
    {
        var shape = TrayShapeUtility.GetShape(tray.TrayType);

        foreach (var offset in shape)
        {
            Vector2Int cell = tray.OriginCell + offset;
            Vector3Int key = new Vector3Int(cell.x, cell.y, tray.Layer);
            _grid.Remove(key);
        }

        _allTrays.Remove(tray);
    }

    public void RemoveTray(Tray tray)
    {
        var shape =
            TrayShapeUtility.GetShape(
                tray.TrayType);

        foreach (var offset in shape)
        {
            Vector2Int cell =
                tray.OriginCell + offset;

            Vector3Int key =
                new Vector3Int(
                    cell.x,
                    cell.y,
                    tray.Layer);

            _grid.Remove(key);
        }

        _allTrays.Remove(tray);

        Destroy(tray.gameObject);

        RefreshCoveredState();
    }

    #endregion

#if UNITY_EDITOR

    [ContextMenu("Refresh Grid")]
    private void RefreshEditor()
    {
        BuildGrid();
        RefreshCoveredState();
    }

#endif
}