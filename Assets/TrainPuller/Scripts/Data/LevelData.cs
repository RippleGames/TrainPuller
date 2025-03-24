using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
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
        public bool isTrainSpawned;
        public bool isExit;
        public bool isBarrier;
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
            Barrier = 9
        }

        [Serializable]
        public struct GridData
        {
            public List<GridColorType> colorTypes;
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
                        y = y,
                        stackData = new GridData
                        {
                            colorTypes = new List<GridColorType>
                                { GridColorType.None }
                        }
                    };
                }
            }
        }


        public GridCell[,] GetGrid() => gridCells;
        public GridCell GetGridCell(int x, int y) => gridCells[x, y];

        public void SetCellColor(int x, int y, GridColorType stackColor, bool isExit, bool isBarrier, int subIndex)
        {
            var cell = gridCells[x, y];
            cell.isExit = isExit;
            cell.isBarrier = isBarrier;
            if (cell.stackData.colorTypes == null)
            {
                cell.stackData.colorTypes = new List<LevelData.GridColorType>();
            }

            if (cell.stackData.colorTypes.Contains(GridColorType.Trail))
            {
                cell.stackData.colorTypes.Clear();
                cell.stackData.colorTypes.Add(GridColorType.Trail);
                cell.stackData.colorTypes.Add(stackColor);
                gridCells[x, y] = cell;
            }
            else
            {
                if (cell.isExit)
                {
                    cell.stackData.colorTypes[0] = stackColor;
                    gridCells[x, y] = cell;
                    return;
                } 
                if (cell.isBarrier)
                {
                    cell.stackData.colorTypes[0] = stackColor;
                    gridCells[x, y] = cell;
                    return;
                }

                if (cell.stackData.colorTypes.Count < 10)
                {
                    if (cell.stackData.colorTypes.Contains(GridColorType.None))
                    {
                        cell.stackData.colorTypes[0] = stackColor;
                        gridCells[x, y] = cell;
                        return;
                    }

                    cell.stackData.colorTypes.Add(stackColor);
                    gridCells[x, y] = cell;
                }
            }
        }

        public void RemoveCellColor(int x, int y, int o)
        {
            var cell = GetGridCell(x, y);

            if (cell.stackData.colorTypes == null) return;

            cell.stackData.colorTypes.Clear();
            cell.stackData.colorTypes.Add(LevelData.GridColorType.None);
            cell.isExit = false;
            cell.isBarrier = false;
            gridCells[x, y] = cell;
        }
    }
}