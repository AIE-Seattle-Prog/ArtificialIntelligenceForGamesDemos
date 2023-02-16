using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[SelectionBase]
public class Tile : MonoBehaviour
{
    public List<Tile> connections = new List<Tile>();
    [HideInInspector]
    public int id;
    public NavGrid tileset;

    public Vector3Int GridPosition
    {
        get
        {
            int y = id / tileset.dimensions.x * tileset.dimensions.z;
            int layerLocalId = id - y * tileset.dimensions.x * tileset.dimensions.z;

            int x = layerLocalId % tileset.dimensions.x;
            int z = layerLocalId / tileset.dimensions.z;

            return new Vector3Int(x, y, z);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;

        if (connections == null) { return; }
        foreach(var con in connections)
        {
            Gizmos.DrawLine(transform.position, con.transform.position);
        }
    }
}
