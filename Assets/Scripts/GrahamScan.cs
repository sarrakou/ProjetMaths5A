using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GrahamScan : MonoBehaviour
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

        Vector2 pivot = points.OrderBy(p => p.y).First();

        List<Vector2> sortedPoints = points
            .Where(p => p != pivot)
            .OrderBy(p => PolarAngle(pivot, p))
            .ToList();
        sortedPoints.Insert(0, pivot);

        Stack<Vector2> stack = new Stack<Vector2>();
        stack.Push(sortedPoints[0]);
        stack.Push(sortedPoints[1]);

        for (int i = 2; i < sortedPoints.Count; i++)
        {
            while (stack.Count >= 2)
            {
                Vector2 p2 = stack.Pop();
                Vector2 p1 = stack.Peek();
                if (IsLeftTurn(p1, p2, sortedPoints[i]))
                {
                    stack.Push(p2);
                    break;
                }
            }
            stack.Push(sortedPoints[i]);
        }

        hullPoints = stack.Reverse().ToList();
        DrawHull();
    }

    private float PolarAngle(Vector2 pivot, Vector2 point)
    {
        return Mathf.Atan2(point.y - pivot.y, point.x - pivot.x);
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

    public void AddPoint(Vector2 point)
    {
        points.Add(point);
        UpdateHull();
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
