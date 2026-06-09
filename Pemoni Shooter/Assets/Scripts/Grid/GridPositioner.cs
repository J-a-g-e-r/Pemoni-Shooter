using UnityEngine;

[ExecuteAlways]
public class GridPositioner : MonoBehaviour
{
    [SerializeField]
    private Tray tray;

    private void Reset()
    {
        tray = GetComponent<Tray>();
    }

    private void OnValidate()
    {
        UpdatePosition();
    }

    [ContextMenu("Snap To Grid")]
    public void UpdatePosition()
    {
        if (tray == null) return;

        GridSettings grid = FindObjectOfType<GridSettings>();
        if (grid == null) return;

        // Lấy kích thước shape (số cell theo X và Y)
        var shape = TrayShapeUtility.GetShape(tray.TrayType);
        int maxX = 0, maxY = 0;
        foreach (var offset in shape)
        {
            if (offset.x > maxX) maxX = offset.x;
            if (offset.y > maxY) maxY = offset.y;
        }
        // maxX+1, maxY+1 = số cell theo mỗi chiều
        float halfW = (maxX + 1) * grid.CellWidth * 0.5f;
        float halfH = (maxY + 1) * grid.CellHeight * 0.5f;

        Vector3 pos = new Vector3(
            tray.OriginCell.x * grid.CellWidth + grid.OriginOffset.x + halfW,
            tray.OriginCell.y * grid.CellHeight + grid.OriginOffset.y + halfH,
            -tray.Layer);

        transform.position = pos;
    }
}