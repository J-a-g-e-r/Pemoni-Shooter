using UnityEngine;

public class GridGizmos : MonoBehaviour
{
    public int Width = 20;
    public int Height = 20;

    private void OnDrawGizmos()
    {
        GridSettings settings = FindObjectOfType<GridSettings>();

        if (settings == null)
            return;

        float cellWidth = settings.CellWidth;
        float cellHeight = settings.CellHeight;

        Vector2 origin = settings.OriginOffset;

        Gizmos.color = Color.green;

        // Vertical lines
        for (int x = 0; x <= Width; x++)
        {
            float posX = origin.x + x * cellWidth;

            Gizmos.DrawLine(
                new Vector3(posX, origin.y, 0),
                new Vector3(posX, origin.y + Height * cellHeight, 0));
        }

        // Horizontal lines
        for (int y = 0; y <= Height; y++)
        {
            float posY = origin.y + y * cellHeight;

            Gizmos.DrawLine(
                new Vector3(origin.x, posY, 0),
                new Vector3(origin.x + Width * cellWidth, posY, 0));
        }


        Gizmos.color = Color.yellow;

        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                Vector3 center = new Vector3(
                    origin.x + (x + 0.5f) * cellWidth,
                    origin.y + (y + 0.5f) * cellHeight,
                    0);

                Gizmos.DrawSphere(center, 0.05f);
            }
        }
    }
}