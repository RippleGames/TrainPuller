using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using FluffyUnderware.Curvy;
using TemplateProject.Scripts.Runtime.Models;

namespace TemplateProject.Scripts.Utilities
{
    public class RoadSplineGenerator : MonoBehaviour
    {
        public int width, height;
        public GridBase[,] grid;
        private HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        public void GenerateSplines(GridBase[,] gridBases, float modifier)
        {
            visited = new HashSet<Vector2Int>();
            grid = gridBases;
            width = gridBases.GetLength(0);
            height = gridBases.GetLength(1);

            Vector2Int start = FindStartPoint();
            if (start == Vector2Int.one * -1) return;

            TraverseAndCreateSplines(start);
        }

        private void TraverseInDirection(Vector2Int start)
        {
            var directions = new List<Vector2Int>()
                { Vector2Int.left, Vector2Int.right, Vector2Int.up, Vector2Int.down };
        }

        private void TraverseAndCreateSplines(Vector2Int start)
        {
            List<Vector2Int> stack = new List<Vector2Int> { start };
            List<CurvySpline> splines = new List<CurvySpline>();
            Dictionary<int, List<Vector2Int>> connections = new Dictionary<int, List<Vector2Int>>();
            while (stack.Count > 0)
            {
                Debug.Log("Stack Count = " + stack.Count);
                Vector2Int current = stack[0];
                stack.RemoveAt(0);
                if (visited.Contains(current)) continue;

                CreateSpline(current, stack, splines, connections);
            }

            foreach (var connection in connections)
            {
                var splineSegments = new List<CurvySplineSegment>();
                foreach (var val in connection.Value)
                {
                    CurvySplineSegment segment = null;
                    foreach (var spline in splines)
                    {
                        segment = spline.ControlPointsList.FirstOrDefault(x =>
                            Vector3.Distance(x.transform.position,
                                grid[val.x, val.y].gameObject.transform.transform.position) <= 0.1f);
                        if (segment)
                        {
                            break;
                        }
                    }


                    // Ensure the segment is not already in a connection
                    if (segment && !segment.Connection)
                    {
                        splineSegments.Add(segment);
                    }
                    else
                    {
                        Debug.LogWarning($"Skipping segment {segment?.name} because it is already connected.");
                    }
                }

                if (splineSegments.Count > 1)
                {
                    var newConnection = CurvyConnection.Create(splineSegments.ToArray());
                    SyncCurvyConnection(newConnection);
                    newConnection.AutoSetFollowUp();
                }
            }
        }

        private void SyncCurvyConnection(CurvyConnection connection)
        {
            if (connection == null || connection.ControlPointsList.Count == 0)
                return;

            // Get the main control point (first in list)
            CurvySplineSegment mainSegment = connection.ControlPointsList[0];
            Vector3 position = mainSegment.transform.position;
            Quaternion rotation = mainSegment.transform.rotation;

            // Apply position & rotation to all connected segments
            foreach (var segment in connection.ControlPointsList)
            {
                segment.transform.position = position;
                segment.transform.rotation = rotation;
            }
        }

        private void CreateSpline(Vector2Int start, List<Vector2Int> stack, List<CurvySpline> splines,
            Dictionary<int, List<Vector2Int>> connections)
        {
            CurvySpline spline = new GameObject($"Spline_{splines.Count}").AddComponent<CurvySpline>();

            if (!splines.Contains(spline))
            {
                splines.Add(spline);
            }

            spline.gameObject.transform.SetParent(grid[start.x, start.y].gameObject.transform.parent);
            List<Vector2Int> path = new List<Vector2Int>();
            Vector2Int current = start;
            var connectionPoint = new Vector2Int();
            while (current != Vector2Int.one * -1 && !visited.Contains(current))
            {
                visited.Add(current);
                path.Add(current);
                if (IsConnectionPoint(current))
                {
                    connectionPoint = current;
                    var neighbors = GetValidNeighbors(current);
                    foreach (var neighbor in neighbors)
                    {
                        if (!visited.Contains(neighbor))
                        {
                            stack.Add(neighbor);
                        }
                    }

                    break;
                }

                current = GetNextNeighbor(current);
            }

            foreach (Vector2Int pos in path)
            {
                spline.Add(grid[pos.x, pos.y].gameObject.transform.transform.position);
            }

            var connectionPoints = new List<Vector2Int>();
            if (connectionPoint == Vector2Int.zero) return;
            connectionPoints.Add(connectionPoint);
            connectionPoints.AddRange(GetValidNeighbors(connectionPoint));
            connections.Add(splines.IndexOf(spline), connectionPoints);
        }

        private Vector2Int FindStartPoint()
        {
            for (int y = 0; y < height; y++)
            for (int x = width - 1; x >= 0; x--)
                if (grid[x, y].isTrail)
                    return new Vector2Int(x, y);
            return Vector2Int.one * -1;
        }

        private bool IsConnectionPoint(Vector2Int pos)
        {
            return GetValidNeighbors(pos).Count > 2;
        }

        private List<Vector2Int> GetValidNeighbors(Vector2Int pos)
        {
            var neighborGrids = grid[pos.x, pos.y].GetNeighbors();

            return neighborGrids.Select(gridBase => new Vector2Int(gridBase.GetXAxis(), gridBase.GetYAxis())).ToList();
        }

        private Vector2Int GetNextNeighbor(Vector2Int pos)
        {
            foreach (var neighbor in GetValidNeighbors(pos))
            {
                if (!visited.Contains(neighbor)) return neighbor;
            }

            return Vector2Int.one * -1;
        }
    }
}