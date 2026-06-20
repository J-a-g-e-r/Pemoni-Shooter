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

    public void RefreshAllGridTrayVisuals()
    {
        RecalculateMaxLayer();
        RefreshCoveredState();
    }

    private void RecalculateMaxLayer()
    {
        _maxLayer = 0;
        foreach (var tray in _allTrays)
            _maxLayer = Mathf.Max(_maxLayer, tray.Layer);
    }

    #endregion

    #region Swap

    public bool IsOnGrid(Tray tray) => _allTrays.Contains(tray);

    public int CountOnGrid => _allTrays.Count;

    public bool CanSwapTrays(Tray a, Tray b)
    {
        if (a == null || b == null || a == b) return false;
        if (!IsOnGrid(a) || !IsOnGrid(b)) return false;
        if (a.TrayType != b.TrayType) return false;

        Vector2Int aCell = a.OriginCell;
        Vector2Int bCell = b.OriginCell;
        int aLayer = a.Layer;
        int bLayer = b.Layer;

        RemoveTrayKeys(a);
        RemoveTrayKeys(b);

        bool ok = CanPlaceTray(a, bCell, bLayer) && CanPlaceTray(b, aCell, aLayer);

        AddTrayKeys(a);
        AddTrayKeys(b);

        return ok;
    }

    public void SwapTrays(Tray a, Tray b)
    {
        Vector2Int aCell = a.OriginCell;
        Vector2Int bCell = b.OriginCell;
        int aLayer = a.Layer;
        int bLayer = b.Layer;

        RemoveTrayKeys(a);
        RemoveTrayKeys(b);

        a.OriginCell = bCell;
        a.Layer = bLayer;
        b.OriginCell = aCell;
        b.Layer = aLayer;

        AddTrayKeys(a);
        AddTrayKeys(b);

        RecalculateMaxLayer();
        RefreshCoveredState();
    }

    private void RemoveTrayKeys(Tray tray)
    {
        var shape = TrayShapeUtility.GetShape(tray.TrayType);
        if (shape == null) return;

        foreach (var offset in shape)
        {
            Vector2Int cell = tray.OriginCell + offset;
            Vector3Int key = new Vector3Int(cell.x, cell.y, tray.Layer);
            _grid.Remove(key);
        }
    }

    private void AddTrayKeys(Tray tray)
    {
        var shape = TrayShapeUtility.GetShape(tray.TrayType);
        if (shape == null) return;

        _maxLayer = Mathf.Max(_maxLayer, tray.Layer);

        foreach (var offset in shape)
        {
            Vector2Int cell = tray.OriginCell + offset;
            Vector3Int key = new Vector3Int(cell.x, cell.y, tray.Layer);

            if (_grid.ContainsKey(key))
                Debug.LogError($"Grid conflict at {key}");

            _grid[key] = tray;
        }
    }

    private bool CanPlaceTray(Tray tray, Vector2Int originCell, int layer)
    {
        var shape = TrayShapeUtility.GetShape(tray.TrayType);
        if (shape == null) return false;

        foreach (var offset in shape)
        {
            Vector3Int key = new Vector3Int(
                originCell.x + offset.x,
                originCell.y + offset.y,
                layer);

            if (_grid.ContainsKey(key))
                return false;
        }

        return true;
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