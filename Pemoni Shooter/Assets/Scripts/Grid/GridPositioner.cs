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
        transform.position = ComputeWorldPosition();
    }

    public Vector3 ComputeWorldPosition()
    {
        if (tray == null) return transform.position;

        GridSettings grid = GridSettings.Instance != null
            ? GridSettings.Instance
            : FindObjectOfType<GridSettings>();
        if (grid == null) return transform.position;

        var shape = TrayShapeUtility.GetShape(tray.TrayType);
        if (shape == null) return transform.position;

        int maxX = 0, maxY = 0;
        foreach (var offset in shape)
        {
            if (offset.x > maxX) maxX = offset.x;
            if (offset.y > maxY) maxY = offset.y;
        }

        float halfW = (maxX + 1) * grid.CellWidth * 0.5f;
        float halfH = (maxY + 1) * grid.CellHeight * 0.5f;

        return new Vector3(
            tray.OriginCell.x * grid.CellWidth + grid.OriginOffset.x + halfW,
            tray.OriginCell.y * grid.CellHeight + grid.OriginOffset.y + halfH,
            -tray.Layer);
    }
}