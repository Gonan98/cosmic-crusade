/* 
    ------------------- Code Monkey -------------------

    Thank you for downloading this package
    I hope you find it useful in your projects
    If you have any questions let me know
    Cheers!

               unitycodemonkey.com
    --------------------------------------------------
 */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
// using CodeMonkey.Utils;

namespace RTSEngine
{
    public class GridXZ<TGridObject>
    {

        public event EventHandler<OnGridObjectChangedEventArgs> OnGridObjectChanged;
        public const int sortingOrderDefault = 5000;

        public class OnGridObjectChangedEventArgs : EventArgs
        {
            public int x;
            public int z;

            public bool isStatusActive;
            public bool isBusy;
        }

        private int width;
        private int height;
        private float cellSize;
        private Vector3 originPosition;
        private GridObject[,] gridArray;
        private List<Vector2Int> activeGridPositionList;


        public GridXZ(int width, int height, float cellSize, Vector3 originPosition, Func<int, int, float, GridObject> createGridObject)
        {
            this.width = width - (int)originPosition.x;
            this.height = height - (int)originPosition.z; ;
            this.cellSize = cellSize;
            this.originPosition = originPosition;
            this.activeGridPositionList = new List<Vector2Int>();
            gridArray = new GridObject[this.width, this.height];
            for (int x = 0; x < gridArray.GetLength(0); x += (int)cellSize)
            {
                for (int z = 0; z < gridArray.GetLength(1); z += (int)cellSize)
                {
                    gridArray[x, z] = createGridObject(x, z, cellSize);
                }
            }
        }

        public int GetWidth()
        {
            return width;
        }

        public int GetHeight()
        {
            return height;
        }

        public float GetCellSize()
        {
            return cellSize;
        }

        public Vector3 GetWorldPosition(int x, int z)
        {
            return new Vector3(x, 0, z) * cellSize + originPosition;
        }

        public void GetXZ(Vector3 worldPosition, out int x, out int z)
        {
            x = Mathf.FloorToInt((worldPosition - originPosition).x / cellSize);
            z = Mathf.FloorToInt((worldPosition - originPosition).z / cellSize);
        }

        public void SetGridObject(int x, int z, GridObject gridObject)
        {
            if (x >= 0 && z >= 0 && x < width && z < height)
            {
                gridArray[x, z] = gridObject;
                TriggerGridObjectChanged(x, z, gridObject.GetIsBusy(), gridObject.GetIsActive());
            }
        }
        // public void SetGridObject(Vector3 worldPosition, GridObject value)
        // {
        //     GetXZ(worldPosition, out int x, out int z);
        //     TriggerGridObjectChanged(x, z, true, false);
        // }

        public void TriggerGridObjectChanged(int x, int z, bool isBusy, bool isStatusActive)
        {
            OnGridObjectChanged?.Invoke(this, new OnGridObjectChangedEventArgs { x = x, z = z, isBusy = isBusy, isStatusActive = isStatusActive });
        }



        public GridObject GetGridObject(int x, int z)
        {
            // Debug.Log(x + ", " + z);
            if (x >= 0 && z >= 0 && x < width && z < height)
            {
                return gridArray[x, z];
            }
            else
            {
                return default(GridObject);
            }
        }

        public List<Vector2Int> GetActiveGridPositionList()
        {
            return activeGridPositionList;
        }
        public Vector2Int ValidateGridPosition(Vector2Int gridPosition)
        {
            return new Vector2Int(
                Mathf.Clamp(gridPosition.x, 0, width - 1),
                Mathf.Clamp(gridPosition.y, 0, height - 1)
            );
        }
    }
}