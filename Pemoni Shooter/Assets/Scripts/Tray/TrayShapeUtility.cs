using UnityEngine;

public static class TrayShapeUtility
{
    public static Vector2Int[] GetShape(TrayType type)
    {
        switch (type)
        {
            // Tray nhỏ 2*2 (Dọc hay ngang đều như nhau)
            case TrayType.Small:
                return new[]
                {
                    new Vector2Int(0,0), new Vector2Int(1,0),
                    new Vector2Int(0,1), new Vector2Int(1,1)
                };

            // Tray vừa dọc 2*3
            case TrayType.Medium:
                return new[]
                {
                    new Vector2Int(0,0), new Vector2Int(1,0),
                    new Vector2Int(0,1), new Vector2Int(1,1),
                    new Vector2Int(0,2), new Vector2Int(1,2)
                };

            // Tray vừa ngang 3*2 (Đảo ngược tọa độ của tray 2*3)
            case TrayType.Medium_Horizontal:
                return new[]
                {
                    new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(2,0),
                    new Vector2Int(0,1), new Vector2Int(1,1), new Vector2Int(2,1)
                };

            // Tray lớn dọc 2*4        
            case TrayType.Large:
                return new[]
                {
                    new Vector2Int(0,0), new Vector2Int(1,0),
                    new Vector2Int(0,1), new Vector2Int(1,1),
                    new Vector2Int(0,2), new Vector2Int(1,2),
                    new Vector2Int(0,3), new Vector2Int(1,3)
                };

            // Tray lớn ngang 4*2 (Đảo ngược tọa độ của tray 2*4)
            case TrayType.Large_Horizontal:
                return new[]
                {
                    new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(2,0), new Vector2Int(3,0),
                    new Vector2Int(0,1), new Vector2Int(1,1), new Vector2Int(2,1), new Vector2Int(3,1)
                };

            default:
                break;
        }

        return null;
    }
}