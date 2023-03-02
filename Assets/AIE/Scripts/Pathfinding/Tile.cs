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

    public Vector2Int GridPosition
    {
        get
        {
            int x = tileset.dimensions.x;
            int z = tileset.dimensions.y;

            return new Vector2Int(x, z);
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
