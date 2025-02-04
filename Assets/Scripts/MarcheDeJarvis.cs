using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MarcheDeJarvis : MonoBehaviour
{
    private List<Vector2> points = new List<Vector2>();
    private List<Vector2> hullPoints = new List<Vector2>();
    private LineRenderer lineRenderer;

    [SerializeField] private Material lineMaterial;

    public void SetLineRenderer(LineRenderer lr)
    {
        if (lr == null)
        {
            Debug.LogError($"Null LineRenderer passed to {this.GetType().Name}");
            return;
        }
        lineRenderer = lr;
        Debug.Log($"LineRenderer set for {this.GetType().Name}");
    }

    private void OnMouseDown()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 point = new Vector2(mousePos.x, mousePos.y);
        points.Add(point);
        UpdateHull();
    }

    public void UpdateHull()
    {
        if (points.Count < 3) return;

        hullPoints.Clear();
        Vector2 leftmost = points.OrderBy(p => p.x).First();
        Vector2 current = leftmost;

        do
        {
            hullPoints.Add(current);
            Vector2 next = points[0];

            for (int i = 1; i < points.Count; i++)
            {
                if (next == current || IsLeftTurn(current, next, points[i]))
                {
                    next = points[i];
                }
            }

            current = next;
        } while (current != leftmost);

        DrawHull();
    }

    public void AddPoint(Vector2 point)
    {
        points.Add(point);
        UpdateHull();
    }

    private bool IsLeftTurn(Vector2 p1, Vector2 p2, Vector2 p3)
    {
        return ((p2.x - p1.x) * (p3.y - p1.y) - (p2.y - p1.y) * (p3.x - p1.x)) > 0;
    }

    private void DrawHull()
    {
        lineRenderer.positionCount = hullPoints.Count + 1;
        for (int i = 0; i <= hullPoints.Count; i++)
        {
            Vector2 point = hullPoints[i % hullPoints.Count];
            lineRenderer.SetPosition(i, new Vector3(point.x, point.y, 0));
        }
    }

    public void Clear()
    {
        points.Clear();
        hullPoints.Clear();
        lineRenderer.positionCount = 0;
    }

    public void SetVisualizationActive(bool active)
    {
        lineRenderer.enabled = active;
        enabled = active;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        foreach (Vector2 point in points)
        {
            Gizmos.DrawSphere(new Vector3(point.x, point.y, 0), 0.1f);
        }
    }
}
