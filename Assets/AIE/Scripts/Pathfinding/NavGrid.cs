﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NavGrid : MonoBehaviour
{
    public GameObject tilePrefab;
    public Vector3Int dimensions = new Vector3Int(3,1,3);
    public int TileCount { get { return dimensions.x * dimensions.y * dimensions.z; } }

    // row-major storage of tiles
    Tile[] tiles;

    /// <summary>
    /// A Vector3 returning the half-dimensions of the collider
    /// </summary>
    public Vector3 TileHalfDimensions
    {
        get
        {
            var tileRenderer = tilePrefab.GetComponentInChildren<MeshRenderer>();
            return tileRenderer.bounds.extents;
        }
    }

    //
    // 1D-style accessor
    //

    // Get tile on a 1D row-major fashion in grid-space
    public Tile GetTile(int idx)
    {
        return tiles[idx];
    }

    //
    // 2D-style accessors
    //

    // Get tile on a 2D grid (with layer specifying verticality) in grid-space
    public Tile GetTile2D(int idx, int idz, int layer=0)
    {
        return GetTile(dimensions.x * dimensions.z * layer + idx + dimensions.x * idz);
    }

    // Get tile on a 2D grid (with idx.y specifying verticality) in grid-space
    public Tile GetTile2D(Vector3Int idx)
    {
        return GetTile2D(idx.x, idx.z, idx.y);
    }

    //
    // 3D-style accessors
    //

    // Get tile on a 3D grid in grid-space
    public Tile GetTile3D(int idx, int idy, int idz)
    {
        return GetTile(dimensions.x * dimensions.z * idy + idx + dimensions.x * idz);
    }

    // Get the nearest cell on the grid given a world-space position
    public Vector3Int GetNearestCellOnGrid(Vector3 point)
    {
        Vector3Int cell = new Vector3Int();
        for (int i = 0; i < 3; ++i)
        {
            point[i] = Mathf.Clamp(point[i], 0, dimensions[i]-1);
            cell[i] = Mathf.RoundToInt(point[i]);
        }

        return cell;
    }

    private void Start()
    {
        Vector3 tileDim = TileHalfDimensions;

        tiles = new Tile[dimensions.x * dimensions.y * dimensions.z];

        // SPAWN TILES

        // row
        for (int i = 0; i < dimensions.z; ++i)
        {
            // col
            for (int j = 0; j < dimensions.x; ++j)
            {
                // spawn and set position
                var babyTile = Instantiate(tilePrefab.gameObject);
                babyTile.transform.position = new Vector3(j * tileDim.x * 2, 0, i * tileDim.z * 2);

                // calculate ID no. for 1-D array storage
                int id1d = i * dimensions.x + j;

                // populate tile data
                var tileActually = babyTile.GetComponent<Tile>();
                tiles[id1d] = tileActually;

                tileActually.tileset = this;
                tileActually.id = id1d;
                tileActually.name = $"Tile {tileActually.id}";
            }
        }

        if(dimensions.y != 1) { Debug.LogWarning("Layering not yet possible"); }

        // MAKE CONNECTIONS

        for (int i = 0; i < dimensions.x; ++i)
        {
            for (int j = 0; j < dimensions.z; ++j)
            {
                var curTile = GetTile2D(i, j);

                // left?
                if (i > 0)
                {
                    curTile.connections.Add(GetTile2D(i - 1, j));
                }

                // right?
                if (i < dimensions.x - 1)
                {
                    curTile.connections.Add(GetTile2D(i + 1, j));
                }

                // top?
                if (j < dimensions.z - 1)
                {
                    curTile.connections.Add(GetTile2D(i, j + 1));
                }

                // bottom?
                if (j > 0)
                {
                    curTile.connections.Add(GetTile2D(i, j - 1));
                }
            }
        }
    }

    private class TileRecord : IComparable<TileRecord>
    {
        public Tile tile;

        public float gScore = Mathf.Infinity;
        public float hScore = 0;
        public float fScore => gScore + hScore;

        public TileRecord prev;

        public TileRecord(Tile tile) { this.tile = tile; }

        public int CompareTo(TileRecord other)
        {
            return fScore.CompareTo(other.gScore);
        }
    }

    /// <summary>
    /// Given two world-space positions, plots a path between them if possible.
    /// </summary>
    /// <param name="worldStart">Starting world-space position.</param>
    /// <param name="worldEnd">Ending world-space position.</param>
    /// <param name="path">To be populated with the path, if possible. Must be non-null.</param>
    /// <returns>True if possible, otherwise false.</returns>
    public bool CalculatePath(Vector3 worldStart, Vector3 worldEnd, List<Vector3> path)
    {
        Vector3Int startInt = GetNearestCellOnGrid(worldStart);
        Vector3Int endInt = GetNearestCellOnGrid(worldEnd);

        return CalculatePath(startInt, endInt, path);
    }

    /// <summary>
    /// Given two grid positions, plots a path between them if possible.
    /// </summary>
    /// <param name="gridStart">Starting grid position.</param>
    /// <param name="gridEnd">Ending grid position.</param>
    /// <param name="path">To be populated with the path, if possible. Must be non-null.</param>
    /// <returns>True if possible, otherwise false.</returns>
    public bool CalculatePath(Vector3Int gridStart, Vector3Int gridEnd, List<Vector3> path)
    {
        // create copies of each tile w/ metadata for pathfinding purposes
        TileRecord[] records = new TileRecord[TileCount];
        for (int i = 0; i < TileCount; ++i)
        {
            records[i] = new TileRecord(GetTile(i));
        }

        List<TileRecord> openList = new();
        HashSet<TileRecord> closedList = new();

        // TBD: allow variable move cost
        const int moveCost = 1;

        records[GetTile2D(gridStart).id].gScore = 0.0f;
        openList.Add(records[GetTile2D(gridStart).id]);

        TileRecord goalTile = records[GetTile2D(gridEnd).id];
        bool goalReached = false;
        while (openList.Count > 0)
        {
            TileRecord currentTile = openList[0];
            // remove current tile from unvisited list
            openList.Remove(currentTile);
            closedList.Add(currentTile);

            // calculate new distance for all tiles
            foreach (var connection in currentTile.tile.connections)
            {
                // skip tiles that are already evaluated
                if(closedList.Contains(records[connection.id])) { continue; }

                float tentativeDist = currentTile.gScore + moveCost;
                if (records[connection.id].gScore > tentativeDist)
                {
                    records[connection.id].gScore = tentativeDist;
                    records[connection.id].prev = currentTile;
                    openList.Sort();
                }

                // add to open list if not already added
                if (!openList.Contains(records[connection.id]))
                {
                    openList.Add(records[connection.id]);
                    openList.Sort();
                }
            }

            // check if goal reached
            if (currentTile == goalTile)
            {
                goalReached = true;
                break;
            }
        }

        // early exit if path not possible
        if (!goalReached) { path.Clear();  return false; }

        // retrace the path
        List<Vector3> pathTrace = new List<Vector3>();
        for (var cur = goalTile; cur != null; cur = cur.prev)
        {
            pathTrace.Add(cur.tile.transform.position);
        }

        // repopulate with path nodes
        path.Clear();
        for(int i = 0; i < pathTrace.Count; ++i)
        {
            path.Add(pathTrace[pathTrace.Count - 1 - i]);
        }

        return true;
    }

    /// <summary>
    /// Given two world-space positions, plots a path between them if possible.
    /// </summary>
    /// <param name="worldStart">Starting world-space position.</param>
    /// <param name="worldEnd">Ending world-space position.</param>
    /// <param name="path">To be populated with the path, if possible. Must be non-null.</param>
    /// <returns>True if possible, otherwise false.</returns>
    public bool CalculatePathAStar(Vector3 worldStart, Vector3 worldEnd, List<Vector3> path)
    {
        Vector3Int startInt = GetNearestCellOnGrid(worldStart);
        Vector3Int endInt = GetNearestCellOnGrid(worldEnd);

        return CalculatePathAStar(startInt, endInt, path);
    }

    /// <summary>
    /// Given two grid positions, plots a path between them if possible.
    /// </summary>
    /// <param name="gridStart">Starting grid position.</param>
    /// <param name="gridEnd">Ending grid position.</param>
    /// <param name="path">To be populated with the path, if possible. Must be non-null.</param>
    /// <returns>True if possible, otherwise false.</returns>
    public bool CalculatePathAStar(Vector3Int gridStart, Vector3Int gridEnd, List<Vector3> path)
    {
        // create copies of each tile w/ metadata for pathfinding purposes
        TileRecord[] records = new TileRecord[TileCount];
        for (int i = 0; i < TileCount; ++i)
        {
            records[i] = new TileRecord(GetTile(i));
        }

        List<TileRecord> openList = new();
        HashSet<TileRecord> closedList = new();

        // TBD: allow variable move cost
        const int moveCost = 1;

        records[GetTile2D(gridStart).id].gScore = 0.0f;
        openList.Add(records[GetTile2D(gridStart).id]);

        TileRecord goalTile = records[GetTile2D(gridEnd).id];
        bool goalReached = false;
        while (openList.Count > 0)
        {
            TileRecord currentTile = openList[0];
            // remove current tile from unvisited list
            openList.Remove(currentTile);
            closedList.Add(currentTile);

            // calculate new distance for all tiles
            foreach (var connection in currentTile.tile.connections)
            {
                // skip tiles that are already evaluated
                if (closedList.Contains(records[connection.id])) { continue; }

                float tentativeDist = currentTile.gScore + moveCost;
                if (records[connection.id].gScore > tentativeDist)
                {
                    records[connection.id].gScore = tentativeDist;
                    records[connection.id].hScore = ManhattanDistance(connection.GridPosition, goalTile.tile.GridPosition );
                    records[connection.id].prev = currentTile;
                    openList.Sort();
                }

                // add to open list if not already added
                if (!openList.Contains(records[connection.id]))
                {
                    openList.Add(records[connection.id]);
                    openList.Sort();
                }
            }

            // check if goal reached
            if (currentTile == goalTile)
            {
                goalReached = true;
                break;
            }
        }

        // early exit if path not possible
        if (!goalReached) { path.Clear(); return false; }

        // retrace the path
        List<Vector3> pathTrace = new List<Vector3>();
        for (var cur = goalTile; cur != null; cur = cur.prev)
        {
            pathTrace.Add(cur.tile.transform.position);
        }

        // repopulate with path nodes
        path.Clear();
        for (int i = 0; i < pathTrace.Count; ++i)
        {
            path.Add(pathTrace[pathTrace.Count - 1 - i]);
        }

        return true;
    }

    private int ManhattanDistance(Vector3Int gridStart, Vector3Int gridEnd)
    {
        return Mathf.Abs(gridStart.x - gridEnd.x) + Mathf.Abs(gridStart.z - gridEnd.z); 
    }
}
