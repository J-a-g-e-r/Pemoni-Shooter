using UnityEngine;

public class GridSettings : MonoBehaviour
{
    public static GridSettings Instance;

    [Header("Grid")]

    public float CellWidth = 1f;   // thay vì CellSize
    public float CellHeight = 1f;

    public Vector2 OriginOffset;

    private void OnEnable()
    {
        Instance = this;
    }

    public Vector3 CellToWorld(Vector2Int cell)
    {
        return new Vector3(
            OriginOffset.x + cell.x * CellWidth,
            OriginOffset.y + cell.y * CellHeight,
            0f);
    }

    //public Vector2Int WorldToCell(Vector3 worldPos)
    //{
    //    return new Vector2Int(
    //        Mathf.RoundToInt((worldPos.x - OriginOffset.x) / CellSize),
    //        Mathf.RoundToInt((worldPos.y - OriginOffset.y) / CellSize));
    //}
}