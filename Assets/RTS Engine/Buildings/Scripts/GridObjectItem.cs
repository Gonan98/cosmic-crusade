using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridObjectItem
{

    public static Dir GetNextDir(Dir dir)
    {
        switch (dir)
        {
            default:
            case Dir.Down: return Dir.Left;
            case Dir.Left: return Dir.Up;
            case Dir.Up: return Dir.Right;
            case Dir.Right: return Dir.Down;
        }
    }

    public enum Dir
    {
        Down,
        Left,
        Up,
        Right,
    }
    public Transform prefab;
    public Transform visual;
    public int width;
    public int length;

    public GridObjectItem(Transform prefab, Transform visual, int length, int width)
    {
        this.prefab = prefab;
        this.visual = visual;
        this.width = width;
        this.length = length;
    }

    public int GetRotationAngle(Dir dir)
    {
        switch (dir)
        {
            default:
            case Dir.Down: return 0;
            case Dir.Left: return 90;
            case Dir.Up: return 180;
            case Dir.Right: return 270;
        }
    }

    public Vector2Int GetRotationOffset(Dir dir)
    {
        switch (dir)
        {
            default:
            case Dir.Down: return new Vector2Int(0, 0);
            case Dir.Left: return new Vector2Int(0, width);
            case Dir.Up: return new Vector2Int(width, length);
            case Dir.Right: return new Vector2Int(length, 0);
        }
    }

    public List<Vector2Int> old_GetGridPositionList(Vector2Int offset)
    {
        List<Vector2Int> gridPositionList = new List<Vector2Int>();
       
        for (int x = 0; x < length; x++)
        {
            for (int z = 0; z < width; z++)
            {
                gridPositionList.Add(offset + new Vector2Int(x, z));
            }
        }
        
        return gridPositionList;
    }

    public List<Vector2Int> GetGridPositionList(Vector2Int offset)
    {
        List<Vector2Int> gridPositionList = new List<Vector2Int>();

        for (int x = 0; x <= 10; x++)
        {
            for (int z = 0; z <= 10; z++)
            {
                gridPositionList.Add(offset + new Vector2Int(x, z));
                // gridPositionList.Add(offset + new Vector2Int(x, z));
            }
        }
        return gridPositionList;
    }
}
