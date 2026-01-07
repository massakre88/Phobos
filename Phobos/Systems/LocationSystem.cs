using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using EFT.Interactive;
using Phobos.Diag;
using Phobos.Navigation;
using UnityEngine;
using UnityEngine.AI;
using Location = Phobos.Navigation.Location;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Phobos.Systems;

public struct Cell()
{
    public readonly List<Location> Locations = [];
    public int Congestion = 0;

    public bool HasLocations
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Locations.Count > 0;
    }
}

public class LocationSystem
{
    private const int MinCells = 3;
    private const float MaxCellSize = 50f;
    
    private readonly Cell[,] _cells;
    private readonly float _cellSize;
    private readonly Vector2Int _gridSize;
    private readonly Vector2 _worldOffset; // Bottom-left corner in world space

    private readonly List<Vector2Int> _cellBuffer;

    private static int _idCounter;

    public LocationSystem()
    {
        var locations = Collect();
        Shuffle(locations);

        // Calculate bounds from positions
        var worldMin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        var worldMax = new Vector3(float.MinValue, float.MinValue, float.MinValue);

        for (var i = 0; i < locations.Count; i++)
        {
            var pos = locations[i].Position;
            
            worldMin.x = Mathf.Min(worldMin.x, pos.x);
            worldMin.z = Mathf.Min(worldMin.z, pos.z);
            worldMax.x = Mathf.Max(worldMax.x, pos.x);
            worldMax.z = Mathf.Max(worldMax.z, pos.z);
        }
        
        // Add padding to bounds
        worldMin.x -= 10f;
        worldMin.z -= 10f;
        worldMax.x += 10f;
        worldMax.z += 10f;

        _worldOffset = new Vector2(worldMin.x, worldMin.z);
        
        var worldWidth = worldMax.x - worldMin.x;
        var worldHeight = worldMax.z - worldMin.z;
        
        // Calculate cell size that gives us at least minCells cells
        var cellSizeFromWidth = worldWidth / MinCells;
        var cellSizeFromHeight = worldHeight / MinCells;
        
        // Take the minimum of the three constraints
        _cellSize = Mathf.Min(MaxCellSize, Mathf.Max(cellSizeFromWidth, cellSizeFromHeight));
        
        // Calculate resulting grid dimensions
        var cols = Mathf.CeilToInt(worldWidth / _cellSize);
        var rows = Mathf.CeilToInt(worldHeight / _cellSize);
        
        _gridSize = new Vector2Int(cols, rows);
        _cells = new Cell[cols, rows];

        var searchRadius = Math.Max(worldWidth, worldHeight) / 2f;

        DebugLog.Write($"Location grid cell size: {_gridSize}, radius: {_cellSize:F1}, locations: {locations.Count}");
        DebugLog.Write($"Location grid world bounds: [{worldMin.x:F0},{worldMin.z:F0}] -> [{worldMax.x:F0},{worldMax.z:F0}]");
        DebugLog.Write($"Location grid world size: {worldWidth:F0}x{worldHeight:F0} search radius: {searchRadius}");

        // Initialize all cells
        for (var x = 0; x < cols; x++)
        {
            for (var y = 0; y < rows; y++)
            {
                _cells[x, y] = new Cell();
            }
        }

        for (var i = 0; i < locations.Count; i++)
        {
            var location = locations[i];
            var coords = WorldToCell(location.Position);
            DebugLog.Write($"Adding {location} to cell at [{coords.x}, {coords.y}]");
            _cells[coords.x, coords.y].Locations.Add(location);
        }

        // Loop through all the cells and check if a path can be found from the cell center or BSG locations to a neighboring cell location.
        for (var x = 0; x < _gridSize.x; x++)
        {
            for (var y = 0; y < _gridSize.y; y++)
            {
                var cellCoords = new Vector2Int(x, y);
                ref var cell = ref _cells[cellCoords.x, cellCoords.y];

                // If we already have BSG issued locations, bail out
                if (cell.Locations.Count > 0)
                {
                    continue;
                }

                DebugLog.Write($"Cell at [{x}, {y}] has no BSG locations, attempting to create synthetic at cell center");

                // Try to find a navmesh position as close to the cell center as possible.
                var worldPos = CellToWorld(cellCoords);

                if (NavMesh.SamplePosition(worldPos, out var hit, searchRadius, NavMesh.AllAreas))
                {
                    if (WorldToCell(hit.position) == cellCoords)
                    {
                        if (CheckPathing(cellCoords, hit.position))
                        {
                            DebugLog.Write($"Cell {cellCoords}: found center sample on navmesh at {worldPos} -> {hit.position}");
                            cell.Locations.Add(BuildSyntheticLocation(hit.position));
                            continue;
                        }
                        
                        DebugLog.Write($"Cell {cellCoords}: non-traversable center sample");
                    }
                }

                DebugLog.Write($"Cell {cellCoords}: no navmesh positions found");
            }
        }
    }

    public Location RequestNear(Vector3 worldPos, Location previous)
    {
        var requestCoords = WorldToCell(worldPos);
        var previousCoords = WorldToCell(previous.Position);
        
        _cellBuffer.Clear();

        var congestionRms = 0f;
        
        // First pass: determine the average congestion in the surrounding cells
        for (var dx = -1; dx <= 1; dx++)
        {
            for (var dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;

                var coords = new Vector2Int(requestCoords.x + dx, requestCoords.y + dy);
                
                if (!IsValidCell(coords))
                    continue;
                
                ref var cell = ref _cells[coords.x, coords.y];
                
                if (!cell.HasLocations)
                    continue;
                
                _cellBuffer.Add(coords);
                congestionRms += cell.Congestion * cell.Congestion;
            }
        }

        if (_cellBuffer.Count == 0)
        {
            // We can't go to any neighboring cell for some reason, grab something from the current cell. 
            var currentCell = _cells[requestCoords.x, requestCoords.y];
            return currentCell.Locations.Count > 0 ? currentCell.Locations[Random.Range(0, currentCell.Locations.Count)] : null;
        }
        
        congestionRms = Mathf.Sqrt(congestionRms / _cellBuffer.Count);

        if (congestionRms < 1e-2)
        {
            congestionRms = 1f;
        }
        
        // Second pass: normalize the congestions by the average, subtract one and multiply by -1 to get the congestion penalty
        // The previously visited cell gets -1 weight and it's neighbors get -0.5
        // Add a random weight between 0 and 1
        for (var i = 0; i < _cellBuffer.Count; i++)
        {
            var coords =  _cellBuffer[i];
            ref var cell =  ref _cells[coords.x, coords.y];
            
            // The congestion score is +ve for cells with below average congestion and -ve for above average
            // We use base 2 log so that congestion of 2x or 0.5x the average has a score of +-1
            var congestionScore = -1 * Mathf.Log(cell.Congestion / congestionRms, 2);
            
            // The momentum score penalizes the previously visited cell and it's neighbors and thus imparts a momentum on the next pick
            // We use the Chebyshev distance to avoid diagonals getting extra penalties
            var chebyshev = Mathf.Max(Mathf.Abs(coords.x - previousCoords.x), Mathf.Abs(coords.y - previousCoords.y));
            var momentumScore = -1 * (1f - Mathf.InverseLerp(0f, 3f, chebyshev));
            
            // Add some randomization
            var randomization = Random.Range(0f, 1f);
            
            var score = congestionScore + momentumScore + randomization;
        }

        return null;
    }

    public void Return(Location location)
    {
        var coords = WorldToCell(location.Position);
        ref var cell = ref _cells[coords.x, coords.y];
        cell.Congestion--;

        if (cell.Congestion >= 0) return;

        cell.Congestion = 0;
        DebugLog.Write($"Returning {location} to the pool resulted in negative congestion");
    }

    private bool CheckPathing(Vector2Int center, Vector3 candidatePos)
    {
        var visited = new HashSet<Vector2Int>();
        var queue = new Queue<Vector2Int>();
    
        queue.Enqueue(center);
        visited.Add(center);

        var tempPath = new NavMeshPath();
        
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            if (!IsValidCell(current))
                continue;
            
            var currentCell =  _cells[current.x, current.y];
            
            // Check each location in this cell against the candidate location for traversability
            // ReSharper disable once LoopCanBeConvertedToQuery
            for (var i = 0; i < currentCell.Locations.Count; i++)
            {
                var location = currentCell.Locations[i];

                if (!NavMesh.CalculatePath(candidatePos, location.Position, NavMesh.AllAreas, tempPath)) continue;
                
                // Only accept paths that actually arrive at the destination
                if (tempPath.corners.Length > 0 && (tempPath.corners[^1] - location.Position).sqrMagnitude <= 1)
                    return true;
            }
        
            for (var dx = -1; dx <= 1; dx++)
            {
                for (var dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    
                    var coords = new Vector2Int(current.x + dx, current.y + dy);
                    
                    // Returns false if already visited
                    if (visited.Add(coords))
                    {
                        queue.Enqueue(coords);
                    }
                }
            }
        }
        
        return false;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsValidCell(Vector2Int cell)
    {
        return cell.x >= 0 && cell.x < _gridSize.x && cell.y >= 0 && cell.y < _gridSize.y;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Vector3 CellToWorld(Vector2Int cell)
    {
        var x = _worldOffset.x + (cell.x + 0.5f) * _cellSize;
        var z = _worldOffset.y + (cell.y + 0.5f) * _cellSize;
        
        return new Vector3(x, 0, z);
    }

    private Vector2Int WorldToCell(Vector3 worldPos)
    {
        var x = Mathf.FloorToInt((worldPos.x - _worldOffset.x) / _cellSize);
        var y = Mathf.FloorToInt((worldPos.z - _worldOffset.y) / _cellSize);
        
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

        _idCounter = 0;

        foreach (var trigger in Object.FindObjectsOfType<TriggerWithId>())
        {
            if (trigger.transform == null)
                continue;

            ValidateAndAddLocation(collection, LocationCategory.Quest, trigger.name, trigger.transform.position);
        }

        foreach (var container in Object.FindObjectsOfType<LootableContainer>())
        {
            if (container.transform == null || !container.enabled || container.Template == null)
                continue;

            ValidateAndAddLocation(collection, LocationCategory.ContainerLoot, container.name, container.transform.position);
        }

        DebugLog.Write($"Collected {collection.Count} points of interest");

        return collection;
    }

    private static void ValidateAndAddLocation(List<Location> collection, LocationCategory category, string name, Vector3 position)
    {
        if (NavMesh.SamplePosition(position, out var target, 2f, NavMesh.AllAreas))
        {
            var objective = new Location(_idCounter, category, name, target.position);
            collection.Add(objective);
            _idCounter++;
        }
        else
        {
            DebugLog.Write($"Skipping Location({category}, {name}, {position}), too far from navmesh");
        }
    }

    private static Location BuildSyntheticLocation(Vector3 position)
    {
        var location = new Location(_idCounter, LocationCategory.Synthetic, $"Synthetic_{_idCounter}", position);
        _idCounter++;
        return location;
    }
}