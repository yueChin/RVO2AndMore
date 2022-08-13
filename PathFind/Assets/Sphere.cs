using RVO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sphere : MonoBehaviour
{
    public bool Debug = false;

    // Gizmos
    public List<Line> m_GizmosLines = new List<Line>();
    public int msId;
    public RVO.Vector2 msVelocity;

    void OnDrawGizmos()
    {
        if (!Debug)
        {
            return;
        }

        Gizmos.color = Color.red;

        // ORCA
        Vector3 from;
        Vector3 to;
        foreach (Line msGizmosLine in m_GizmosLines)
        {
            RVO.Vector2 from_ = msGizmosLine.Point - msGizmosLine.Direction * 100;
            RVO.Vector2 to_ = msGizmosLine.Point + msGizmosLine.Direction * 100;

            from = transform.position + new Vector3(from_.X(), 0, from_.Y());
            to = transform.position + new Vector3(to_.X(), 0, to_.Y());

            Gizmos.DrawLine(from, to);
        }

        m_GizmosLines.Clear();
        // velocity
        Gizmos.color = Color.green;

        from = transform.position;
        to = transform.position + new Vector3(msVelocity.X(), 0, msVelocity.Y());

        Gizmos.DrawLine(from, to);
    }
}
