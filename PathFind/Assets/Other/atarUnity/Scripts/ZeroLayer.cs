using UnityEngine;
using System.Collections;

// Handles Raycast-detection
public class ZeroLayer : MonoBehaviour
{
    public GameObject otherCube;
    private Grid grid;

    void Start()
    {
        grid = FindObjectOfType<Grid>();
    }

    void Update()
    {
        if (!grid.isCalculating)
        {
            if (Input.GetKey(KeyCode.Mouse0))
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit, 100))
                {
                    if (otherCube)
                    {
                        otherCube.GetComponent<Collider>().enabled = false;
                        otherCube.transform.position = FindClosestCell(hit.point) + new Vector3(0, 1, 0);
                    }
                }
            }
            else if (Input.GetKeyUp(KeyCode.Mouse0))
            {
                if (otherCube)
                {
                    otherCube.GetComponent<Collider>().enabled = true;
                    otherCube = null;

                    grid.CalculatePathExternal();
                }
            }
        }
    }

    private Vector3 FindClosestCell(Vector3 startPosition)
    {
        GameObject closest = null;
        float distance = Mathf.Infinity;
        Vector3 position = startPosition;
        foreach (GameObject gameObject in grid.allCells)
        {
            Vector3 diff = gameObject.transform.position - position;
            float curDistance = diff.sqrMagnitude;
            if (curDistance < distance)
            {
                closest = gameObject;
                distance = curDistance;
            }
        }

        return closest.transform.position;
    }
}