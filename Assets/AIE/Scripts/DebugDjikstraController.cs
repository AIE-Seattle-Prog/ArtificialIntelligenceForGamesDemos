using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugDjikstraController : MonoBehaviour
{
    public Camera cam;
    public TileAIController tileAgent;
    public NavGrid grid;

    void Update()
    {
        // agent orders
        if (Input.GetMouseButtonDown(0))
        {
            var pickerRay = cam.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(pickerRay, out var hit, Mathf.Infinity))
            {
                List<Vector3> path = new();
                if (grid.CalculatePath(tileAgent.transform.position, hit.point, path))
                {
                    tileAgent.SetPath(path);
                }
            }
        }

        // tile traversal toggle
        if (Input.GetMouseButtonDown(1))
        {
            var pickerRay = cam.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(pickerRay, out var hit, Mathf.Infinity))
            {
                //grid.GetTile3D()
            }
        }
    }
}
