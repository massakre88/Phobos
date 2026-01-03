using System.Collections.Generic;
using EFT.Interactive;
using Phobos.Diag;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Phobos.Navigation;

public struct Cell(Queue<Location> locations, int congestion)
{
    public readonly Queue<Location> Locations = locations;
    public int Congestion = congestion;
}

public class LocationPool
{
    private readonly List<Cell> _candidates = new(16);
    private readonly Cell[,] _cells;

    private readonly Vector2 _boundsMin;
    private readonly Vector2Int _gridDimensions;
    private readonly Vector2 _cellSize;

    public LocationPool()
    {
        var locations = Collect();
        Shuffle(locations);

        // Calculate 2D bounds (ignore Y axis)
        var minX = float.MaxValue;
        var minZ = float.MaxValue;
        var maxX = float.MinValue;
        var maxZ = float.MinValue;

        for (var i = 0; i < locations.Count; i++)
        {
            var pos = locations[i].Position;

            if (pos.x < minX) minX = pos.x;
            if (pos.x > maxX) maxX = pos.x;
            if (pos.z < minZ) minZ = pos.z;
            if (pos.z > maxZ) maxZ = pos.z;
        }

        _boundsMin = new Vector2(minX, minZ);
        var boundsSize = new Vector2(maxX - minX, maxZ - minZ);

        // Calculate grid dimensions based on map aspect ratio
        // ~15 objectives per cell
        var totalCells = Mathf.Max(25, locations.Count / 15);
        var aspectRatio = boundsSize.x / boundsSize.y;

        // Distribute cells proportionally to aspect ratio
        var cellsY = Mathf.RoundToInt(Mathf.Sqrt(totalCells) / aspectRatio);
        var cellsX = Mathf.RoundToInt(cellsY * aspectRatio);

        // Ensure minimum dimensions to prevent degenerate grids
        cellsX = Mathf.Max(3, cellsX);
        cellsY = Mathf.Max(3, cellsY);

        _gridDimensions = new Vector2Int(cellsX, cellsY);
        _cellSize = new Vector2(boundsSize.x / _gridDimensions.x, boundsSize.y / _gridDimensions.y);
        _cells = new Cell[_gridDimensions.x, _gridDimensions.y];

        // Populate grid with objective indices
        for (var i = 0; i < locations.Count; i++)
        {
            var location = locations[i];
            var coords = WorldToGrid(location.Position);
            var cell = _cells[coords.x, coords.y];

            cell.Locations.Enqueue(location);
        }
        
        DebugLog.Write($"Location pool built with grid size {_gridDimensions}");
    }

    public Location Request()
    {
        // TODO: This needs to be done differently.
        //       We need to maintain a SortedSet of cells with available locations. The set is sorted by congestion.
        //       When we retrieve a location, we check if the cell is now empty, if yes, we move it to the EmptyCells dictionary.
        //       When a location is returned, we'll check if it was empty before, and if yes, we'll remove it from the EmptyCells and add back to the available locations  set.
        //       This ensures that we'll maximize dispersion but also fully utilize all locations we can.
        
        // Find cell(s) with minimum congestion
        _candidates.Clear();
        var minCongestion = int.MaxValue;

        for (var x = 0; x < _cells.GetLength(0); x++)
        {
            for (var y = 0; y < _cells.GetLength(1); y++)
            {
                var cell = _cells[x, y];
                
                if (cell.Congestion < minCongestion)
                {
                    minCongestion = cell.Congestion;
                    _candidates.Clear();
                    _candidates.Add(cell);
                }
                else if (cell.Congestion == minCongestion)
                {
                    _candidates.Add(cell);
                }
            }
        }

        // ReSharper disable once LoopCanBeConvertedToQuery
        for (var i = 0; i < _candidates.Count; i++)
        {
            var candidate = _candidates[i];

            if (candidate.Locations.Count <= 0) continue;
            
            var location = candidate.Locations.Dequeue();
            candidate.Congestion--;
            var coords = WorldToGrid(location.Position);
            _cells[coords.x, coords.y] = candidate;
        }
        
        return null;
    }

    public void Return(Location location)
    {
        var coords = WorldToGrid(location.Position);
        ref var cell = ref _cells[coords.x, coords.y];
        cell.Locations.Enqueue(location);
        cell.Congestion--;

        if (cell.Congestion >= 0) return;
        
        cell.Congestion = 0;
        DebugLog.Write($"Returning {location} to the pool resulted in negative congestion");
    }

    private Vector2Int WorldToGrid(Vector3 worldPos)
    {
        var x = Mathf.FloorToInt((worldPos.x - _boundsMin.x) / _cellSize.x);
        var y = Mathf.FloorToInt((worldPos.z - _boundsMin.y) / _cellSize.y);

        // Clamp to grid bounds
        x = Mathf.Clamp(x, 0, _gridDimensions.x - 1);
        y = Mathf.Clamp(y, 0, _gridDimensions.y - 1);

        return new Vector2Int(x, y);
    }

    private static void Shuffle(List<Location> objectives)
    {
        // Fisher-Yates in-place shuffle
        for (var i = 0; i < objectives.Count; i++)
        {
            var randomIndex = Random.Range(i, objectives.Count);
            (objectives[i], objectives[randomIndex]) = (objectives[randomIndex], objectives[i]);
        }
    }

    private static List<Location> Collect()
    {
        var collection = new List<Location>();

        DebugLog.Write("Collecting quests POIs");

        var idCounter = 0;

        foreach (var trigger in Object.FindObjectsOfType<TriggerWithId>())
        {
            if (trigger.transform == null)
                continue;

            AddValid(idCounter, collection, LocationCategory.Quest, trigger.name, trigger.transform.position);

            idCounter++;
        }

        foreach (var container in Object.FindObjectsOfType<LootableContainer>())
        {
            if (container.transform == null || !container.enabled || container.Template == null)
                continue;

            AddValid(idCounter, collection, LocationCategory.ContainerLoot, container.name, container.transform.position);

            idCounter++;
        }

        DebugLog.Write($"Collected {collection.Count} points of interest");

        return collection;
    }

    private static void AddValid(int idCounter, List<Location> collection, LocationCategory category, string name, Vector3 position)
    {
        if (NavMesh.SamplePosition(position, out var target, 5f, NavMesh.AllAreas))
        {
            var objective = new Location(idCounter, category, name, target.position);

            collection.Add(objective);
            DebugLog.Write($"{objective} added as location");
        }
        else
        {
            DebugLog.Write($"Objective({category}, {name}, {position}) too far from navmesh");
        }
    }
}