using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using EFT.Interactive;
using Phobos.Diag;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Phobos.Navigation;

public struct Cell()
{
    public readonly List<Location> Locations = [];
    public int Congestion = 0;
}

public class LocationPool
{
    // We need at least 3 cells in both dimensions
    private const int MinCells = 3;
    private const float MaxDiameter = 50f;
    private const float MaxRadius = MaxDiameter / 2f;

    private readonly Cell[,] _cells;
    private readonly float _cellSize;
    private readonly Vector2Int _gridSize;
    private readonly Vector2 _worldOffset; // Bottom-left corner in world space

    private static int _idCounter;
    
    // Axial coordinate offsets for the 6 neighbors of a hex cell
    // Using flat-top hex orientation
    private static readonly Vector2Int[] NeighborOffsets =
    [
        new Vector2Int(+1, 0), new Vector2Int(+1, -1),
        new Vector2Int(0, -1), new Vector2Int(-1, 0),
        new Vector2Int(-1, +1), new Vector2Int(0, +1)
    ];

    public LocationPool()
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

        // Add padding to avoid edge cases with positions exactly on boundaries
        const float padding = 10f;
        worldMin.x -= padding;
        worldMin.z -= padding;
        worldMax.x += padding;
        worldMax.z += padding;

        _worldOffset = new Vector2(worldMin.x, worldMin.z);

        var worldWidth = worldMax.x - worldMin.x;
        var worldHeight = worldMax.z - worldMin.z;

        // Calculate cell size based on constraints
        // For hex grid: horizontal spacing = 3/2 * radius, vertical = sqrt(3) * radius
        // Calculate maximum radius that gives us at least minCells cells
        var maxRadiusFromWidth = worldWidth / (1.5f * MinCells);
        var maxRadiusFromHeight = worldHeight / (Mathf.Sqrt(3) * MinCells);

        // Take the minimum of the three constraints
        _cellSize = Mathf.Min(MaxRadius, Mathf.Min(maxRadiusFromWidth, maxRadiusFromHeight));

        // Calculate resulting grid dimensions
        var cols = Mathf.CeilToInt(worldWidth / (1.5f * _cellSize)) + 1;
        var rows = Mathf.CeilToInt(worldHeight / (Mathf.Sqrt(3) * _cellSize)) + 1;

        _gridSize = new Vector2Int(cols, rows);
        _cells = new Cell[cols, rows];

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
            _cells[coords.x, coords.y].Locations.Add(location);
        }

        for (var x = 0; x < _cells.GetLength(0); x++)
        {
            for (var y = 0; y < _cells.GetLength(1); y++)
            {
                ref var cell = ref _cells[x, y];
                if (cell.Locations.Count > 0)
                    continue;
                
                var worldPos = CellToWorld(new Vector2Int(x, y));
                var inscribedRadius = _cellSize * Mathf.Sqrt(3) / 2f;

                if (!NavMesh.SamplePosition(worldPos, out var hit, inscribedRadius, NavMesh.AllAreas)) continue;
                
                cell.Locations.Add(new Location(_idCounter, LocationCategory.Synthetic, $"Synthetic({_idCounter})", hit.position));
                _idCounter++;
            }
        }

        DebugLog.Write($"Location grid cell size: {_gridSize}, radius: {_cellSize:F1}, locations: {locations.Count}");
        DebugLog.Write($"Location grid world bounds: ({worldMin.x:F0},{worldMin.z:F0})->({worldMax.x:F0},{worldMax.z:F0})");
        DebugLog.Write($"Location grid world size: {worldWidth:F0}x{worldHeight:F0}");
    }

    public Location RequestNear(Vector3 worldPos, Vector2Int previous)
    {
        var requestCoords = WorldToCell(worldPos);

        for (var i = 0; i < NeighborOffsets.Length; i++)
        {
            var coords = requestCoords + NeighborOffsets[i];
            
            if (!IsValidCell(coords))
                continue;
        }
        
        

        return RequestWide(worldPos);
    }

    public Location RequestWide(Vector3 worldPos)
    {
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

    public Vector3 CellToWorld(Vector2Int cell)
    {
        var x = _cellSize * (3f / 2f * cell.x) + _worldOffset.x;
        var z = _cellSize * (Mathf.Sqrt(3) / 2f * cell.x + Mathf.Sqrt(3) * cell.y) + _worldOffset.y;

        return new Vector3(x, 0, z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsValidCell(Vector2Int cell)
    {
        return cell.x >= 0 && cell.x < _gridSize.x && cell.y >= 0 && cell.y < _gridSize.y;
    }

    private Vector2Int WorldToCell(Vector3 worldPos)
    {
        var x = worldPos.x - _worldOffset.x;
        var z = worldPos.z - _worldOffset.y;

        var q = 2f / 3f * x / _cellSize;
        var r = (-1f / 3f * x + Mathf.Sqrt(3) / 3f * z) / _cellSize;

        return AxialRound(q, r);
    }

    private static Vector2Int AxialRound(float q, float r)
    {
        var s = -q - r;

        var rq = Math.Round(q);
        var rr = Math.Round(r);
        var rs = Math.Round(s);

        var qDiff = Math.Abs(rq - q);
        var rDiff = Math.Abs(rr - r);
        var sDiff = Math.Abs(rs - s);

        if (qDiff > rDiff && qDiff > sDiff)
        {
            rq = -rr - rs;
        }
        else if (rDiff > sDiff)
        {
            rr = -rq - rs;
        }

        return new Vector2Int((int)rq, (int)rr);
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

            AddValid(_idCounter, collection, LocationCategory.Quest, trigger.name, trigger.transform.position);

            _idCounter++;
        }

        foreach (var container in Object.FindObjectsOfType<LootableContainer>())
        {
            if (container.transform == null || !container.enabled || container.Template == null)
                continue;

            AddValid(_idCounter, collection, LocationCategory.ContainerLoot, container.name, container.transform.position);

            _idCounter++;
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