using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugPathfindingController : MonoBehaviour
{
    public Camera cam;
    public TileAIController tileAgent;
    public NavGrid grid;

    public bool useAStar = false;
    private Vector3 clickLocation;

    void Update()
    {
        // agent orders
        if (Input.GetMouseButtonDown(0))
        {
            var pickerRay = cam.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(pickerRay, out var hit, Mathf.Infinity))
            {
                clickLocation = hit.point;
                List<Vector3> path = new();

                if(useAStar)
                {
                    grid.CalculatePathAStar(tileAgent.transform.position, hit.point, path);
                }
                else
                {
                    grid.CalculatePath(tileAgent.transform.position, hit.point, path);
                }

                if (path.Count > 0)
                {
                    tileAgent.SetPath(path);
                }
            }
        }

        // tile traversal toggle
        if (Input.GetMouseButtonDown(1))
        {
            var pickerRay = cam.ScreenPointToRay(Input.mousePosition);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        if (clickLocation != Vector3.zero)
        {
            Gizmos.DrawRay(clickLocation, Vector3.up * 3.0f);
        }
    }
}
