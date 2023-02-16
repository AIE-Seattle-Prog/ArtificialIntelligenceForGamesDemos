using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NavGrid : MonoBehaviour
{
    public GameObject tilePrefab;
    public Vector3Int dimensions = new Vector3Int(3,1,3);
    public int tileCount { get { return dimensions.x * dimensions.y * dimensions.z; } }

    // row-major storage of tiles
    Tile[] tiles;

    // get world-space dimensions of tile
    public Vector3 tileDimensions
    {
        get
        {
            var tileRenderer = tilePrefab.GetComponentInChildren<MeshRenderer>();
            return tileRenderer.bounds.extents;
        }
    }

    // 1D-style accessor

    // Get tile on a 1D row-major fashion in grid-space
    public Tile GetTile(int idx)
    {
        return tiles[idx];
    }

    // 2D-style accessors

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

    // 3D-style accessor

    // Get tile on a 3D grid in grid-space
    public Tile GetTile(int idx, int idy, int idz)
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
        Vector3 tileDim = tileDimensions;

        tiles = new Tile[dimensions.x * dimensions.y * dimensions.z];

        // SPAWN TILES

        // row
        for (int i = 0; i < dimensions.z; ++i)
        {
            // col
            for (int j = 0; j < dimensions.x; ++j)
            {
                var babyTile = Instantiate<Transform>(tilePrefab.transform);

                babyTile.transform.position = new Vector3(j * tileDim.x * 2, 0, i * tileDim.z * 2);

                var tileActually = babyTile.GetComponent<Tile>();
                int id1d = i * dimensions.x + j;
                tiles[id1d] = tileActually;
                tileActually.tileset = this;
                tileActually.id = id1d;
            }
        }

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
        public float distance = Mathf.Infinity;

        public TileRecord prev;

        public TileRecord(Tile tile) { this.tile = tile; }

        public int CompareTo(TileRecord other)
        {
            return distance.CompareTo(other.distance);
        }
    }

    public bool CalculatePath(Vector3 start, Vector3 end, List<Vector3> path)
    {
        Vector3Int startInt = GetNearestCellOnGrid(start);
        Vector3Int endInt = GetNearestCellOnGrid(end);

        return CalculatePath(startInt, endInt, path);
    }

    public bool CalculatePath(Vector3Int start, Vector3Int end, List<Vector3> path)
    {
        // create copies of each tile w/ metadata for pathfinding purposes
        TileRecord[] allTiles = new TileRecord[tileCount];
        List<TileRecord> openList = new List<TileRecord>();
        for (int i = 0; i < tileCount; ++i)
        {
            allTiles[i] = new TileRecord(GetTile(i));
            openList.Add(allTiles[i]);
        }

        TileRecord currentTile = allTiles[GetTile2D(start).id];
        currentTile.distance = 0;
        TileRecord goalTile = allTiles[GetTile2D(end).id];

        // TBD: allow variable move cost
        const int moveCost = 1;

        bool goalReached = false;
        while (openList.Count > 0)
        {
            openList.Sort();
            currentTile = openList[0];

            // calculate new distance for all tiles
            foreach (var connection in currentTile.tile.connections)
            {
                float tentativeDist = currentTile.distance + moveCost;
                if (allTiles[connection.id].distance > tentativeDist)
                {
                    allTiles[connection.id].distance = tentativeDist;
                    allTiles[connection.id].prev = currentTile;
                }
            }

            // remove current tile from unvisited list
            openList.RemoveAt(0); // this was the first tile

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
}
