using UnityEngine;

public static class TrayShapeUtility
{
    public static Vector2Int[] GetShape(TrayType type)
    {
        switch (type)
        {
            // Tray nhỏ 2*2
            case TrayType.Small:
                return new[]
                {
                    new Vector2Int(0,0),
                    new Vector2Int(1,0),

                    new Vector2Int(0,1),
                    new Vector2Int(1,1)
                };

            // Tray vừa 2*3
            case TrayType.Medium:
                return new[]
                {
                    new Vector2Int(0,0),
                    new Vector2Int(1,0),

                    new Vector2Int(0,1),
                    new Vector2Int(1,1),

                    new Vector2Int(0,2),
                    new Vector2Int(1,2)
                };
            
            // Tray lớn 2*4        
            case TrayType.Large:
                return new[]
                {
                    new Vector2Int(0,0),
                    new Vector2Int(1,0),

                    new Vector2Int(0,1),
                    new Vector2Int(1,1),

                    new Vector2Int(0,2),
                    new Vector2Int(1,2),

                    new Vector2Int(0,3),
                    new Vector2Int(1,3)
                };
            default:
                break;
        }

        return null;
    }
}