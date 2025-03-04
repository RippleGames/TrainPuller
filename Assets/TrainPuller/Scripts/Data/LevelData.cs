using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using static TemplateProject.Scripts.Data.LevelData;

namespace TemplateProject.Scripts.Data
{
    [Serializable]
    public struct GridCell
    {
        public int x;
        public int y;
        public GridData stackData;
        public List<GridCell> adjacentCells;
    }

    [Serializable]
    public class LevelData
    {
        public enum GridColorType
        {
            None = 0,
            Red = 1,
            Green = 2,
            Blue = 3,
            Yellow = 4,
            Purple = 5,
            Orange = 6,
            Pink = 7,
            Trail = 8,
            Close = 9
        }

        [Serializable]
        public struct GridData
        {
            public GridColorType gridColorType;
            public bool isSecret;
            public bool isReserved;
        }

        public int width => gridCells.GetLength(0);
        public int height => gridCells.GetLength(1);
        public GridCell[,] gridCells { get; set; }

        public LevelData(int width, int height)
        {
            gridCells = new GridCell[width, height];
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    gridCells[x, y] = new GridCell
                    {
                        x = x,
                        y = y
                    };
                }
            }
        }

        public GridCell[,] GetGrid() => gridCells;
        public GridCell GetGridCell(int x, int y) => gridCells[x, y];

        public void SetCellSettings(int x, int y, GridColorType stickmanColor, bool isSecretStickman,
            bool isReservedStickman)
        {
            gridCells[x, y].stackData.gridColorType = stickmanColor;
            gridCells[x, y].stackData.isSecret = isSecretStickman;
            gridCells[x, y].stackData.isReserved = isReservedStickman;
        }

        public void RemoveCellSettings(int x, int y)
        {
            gridCells[x, y].stackData.gridColorType = 0;
            gridCells[x, y].stackData.isSecret = false;
            gridCells[x, y].stackData.isReserved = false;
        }
    }
}