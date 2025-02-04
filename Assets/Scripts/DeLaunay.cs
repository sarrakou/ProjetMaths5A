using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class DeLaunay : MonoBehaviour
{
    private List<Point2D> points = new List<Point2D>();
    public List<Triangle2D> triangles = new List<Triangle2D>();
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

    public void AddPoint(Vector2 position)
    {
        Debug.Log($"Adding point at {position}");
        Point2D newPoint = new Point2D(position);
        points.Add(newPoint);

        if (points.Count < 3)
        {
            Debug.Log("Not enough points for triangulation yet");
            return;
        }

        if (points.Count == 3)
        {
            Debug.Log("Creating initial triangle");
            CreateInitialTriangle();
            return;
        }

        Debug.Log($"Processing point #{points.Count}");
        InsertPointAndFlip(newPoint);
    }

    private void CreateInitialTriangle()
    {
        Debug.Log("Creating initial triangle");

        Triangle2D triangle = new Triangle2D(points[0], points[1], points[2]);

        triangle.edges[0] = new Edge2D(points[0], points[1]);
        triangle.edges[1] = new Edge2D(points[1], points[2]);
        triangle.edges[2] = new Edge2D(points[2], points[0]);

        triangles.Add(triangle);

        Debug.Log($"Initial triangle vertices: " +
            $"({triangle.vertices[0].position.x}, {triangle.vertices[0].position.y}), " +
            $"({triangle.vertices[1].position.x}, {triangle.vertices[1].position.y}), " +
            $"({triangle.vertices[2].position.x}, {triangle.vertices[2].position.y})");

        UpdateEdges();
        DrawTriangulation();
    }
    private void InsertPointAndFlip(Point2D newPoint)
    {
        Triangle2D containingTriangle = FindContainingTriangle(newPoint);
        List<Edge2D> edgesToCheck = new List<Edge2D>();

        if (containingTriangle != null)
        {
            Point2D p1 = containingTriangle.vertices[0];
            Point2D p2 = containingTriangle.vertices[1];
            Point2D p3 = containingTriangle.vertices[2];

            triangles.Remove(containingTriangle);

            Triangle2D t1 = new Triangle2D(p1, p2, newPoint);
            Triangle2D t2 = new Triangle2D(p2, p3, newPoint);
            Triangle2D t3 = new Triangle2D(p3, p1, newPoint);

            SetupTriangleEdges(t1);
            SetupTriangleEdges(t2);
            SetupTriangleEdges(t3);

            triangles.Add(t1);
            triangles.Add(t2);
            triangles.Add(t3);

            foreach (Edge2D edge in t1.edges) edgesToCheck.Add(edge);
            foreach (Edge2D edge in t2.edges) edgesToCheck.Add(edge);
            foreach (Edge2D edge in t3.edges) edgesToCheck.Add(edge);
        }
        else
        {
            List<Edge2D> visibleEdges = FindVisibleEdges(newPoint);
            foreach (Edge2D edge in visibleEdges)
            {
                Triangle2D newTriangle = new Triangle2D(edge.start, edge.end, newPoint);
                SetupTriangleEdges(newTriangle);
                triangles.Add(newTriangle);
                edgesToCheck.AddRange(newTriangle.edges);
            }
        }

        FlipEdgesUntilDelaunay(edgesToCheck);

        UpdateEdges();
        DrawTriangulation();
    }

    private void FlipEdgesUntilDelaunay(List<Edge2D> initialEdges)
    {
        HashSet<string> processedEdges = new HashSet<string>();
        Queue<Edge2D> edgesToCheck = new Queue<Edge2D>(initialEdges);

        while (edgesToCheck.Count > 0)
        {
            Edge2D edge = edgesToCheck.Dequeue();
            string edgeKey = GetEdgeKey(edge.start, edge.end);

            if (processedEdges.Contains(edgeKey))
                continue;

            processedEdges.Add(edgeKey);

            Triangle2D t1 = null, t2 = null;
            foreach (Triangle2D t in triangles)
            {
                if (HasEdge(t, edge))
                {
                    if (t1 == null) t1 = t;
                    else { t2 = t; break; }
                }
            }

            if (t1 == null || t2 == null) continue;

            if (NeedsFlip(t1, t2, edge))
            {
                Point2D p1 = GetOppositeVertex(t1, edge);
                Point2D p2 = GetOppositeVertex(t2, edge);

                triangles.Remove(t1);
                triangles.Remove(t2);

                Triangle2D newT1 = new Triangle2D(p1, edge.start, p2);
                Triangle2D newT2 = new Triangle2D(p2, edge.end, p1);

                SetupTriangleEdges(newT1);
                SetupTriangleEdges(newT2);

                triangles.Add(newT1);
                triangles.Add(newT2);

                foreach (Edge2D e in newT1.edges)
                {
                    if (!processedEdges.Contains(GetEdgeKey(e.start, e.end)))
                        edgesToCheck.Enqueue(e);
                }
                foreach (Edge2D e in newT2.edges)
                {
                    if (!processedEdges.Contains(GetEdgeKey(e.start, e.end)))
                        edgesToCheck.Enqueue(e);
                }
            }
        }
    }

    private bool NeedsFlip(Triangle2D t1, Triangle2D t2, Edge2D edge)
    {
        Point2D p1 = GetOppositeVertex(t1, edge);
        Point2D p2 = GetOppositeVertex(t2, edge);

        if (p1 == null || p2 == null) return false;

        return GeometryUtils.IsPointInCircumcircle(t1, p2);
    }

    private bool HasEdge(Triangle2D triangle, Edge2D edge)
    {
        foreach (Edge2D e in triangle.edges)
        {
            if ((Vector2.Distance(e.start.position, edge.start.position) < 0.0001f &&
                 Vector2.Distance(e.end.position, edge.end.position) < 0.0001f) ||
                (Vector2.Distance(e.start.position, edge.end.position) < 0.0001f &&
                 Vector2.Distance(e.end.position, edge.start.position) < 0.0001f))
            {
                return true;
            }
        }
        return false;
    }

    private string GetEdgeKey(Point2D p1, Point2D p2)
    {
        return p1.position.x < p2.position.x ?
            $"{p1.position.x},{p1.position.y}-{p2.position.x},{p2.position.y}" :
            $"{p2.position.x},{p2.position.y}-{p1.position.x},{p1.position.y}";
    }

    private bool IsQuadrilateralConvex(Triangle2D t1, Triangle2D t2)
    {
        Point2D[] quadPoints = new Point2D[4];
        Edge2D sharedEdge = GetSharedEdge(t1, t2);
        if (sharedEdge == null) return false;

        quadPoints[0] = sharedEdge.start;
        quadPoints[1] = sharedEdge.end;
        quadPoints[2] = GetOppositeVertex(t1, sharedEdge);
        quadPoints[3] = GetOppositeVertex(t2, sharedEdge);

        if (quadPoints.Any(p => p == null)) return false;

        for (int i = 0; i < 4; i++)
        {
            Vector2 current = quadPoints[i].position;
            Vector2 next = quadPoints[(i + 1) % 4].position;
            Vector2 prev = quadPoints[(i + 3) % 4].position;

            Vector2 v1 = next - current;
            Vector2 v2 = prev - current;

            float cross = v1.x * v2.y - v1.y * v2.x;

            if (cross < 0) return false;
        }

        return true;
    }

    private float CalculateCircumcirclePredicate(Triangle2D triangle, Point2D point)
    {
        Vector2 a = triangle.vertices[0].position;
        Vector2 b = triangle.vertices[1].position;
        Vector2 c = triangle.vertices[2].position;
        Vector2 d = point.position;

        float ax = a.x - d.x;
        float ay = a.y - d.y;
        float bx = b.x - d.x;
        float by = b.y - d.y;
        float cx = c.x - d.x;
        float cy = c.y - d.y;

        float det = (ax * ax + ay * ay) * (bx * cy - cx * by) -
                    (bx * bx + by * by) * (ax * cy - cx * ay) +
                    (cx * cx + cy * cy) * (ax * by - bx * ay);

        return det;
    }

    private IEnumerable<Triangle2D> GetNeighbors(Triangle2D triangle)
    {
        return triangle.neighbors.Where(n => n != null);
    }

    private void SetupTriangleEdges(Triangle2D triangle)
    {
        triangle.edges[0] = new Edge2D(triangle.vertices[0], triangle.vertices[1]);
        triangle.edges[1] = new Edge2D(triangle.vertices[1], triangle.vertices[2]);
        triangle.edges[2] = new Edge2D(triangle.vertices[2], triangle.vertices[0]);
    }

    private Triangle2D FindContainingTriangle(Point2D point)
    {
        foreach (Triangle2D triangle in triangles)
        {
            if (IsPointInTriangle(point.position, triangle))
            {
                return triangle;
            }
        }
        return null;
    }

    private List<Edge2D> FindVisibleEdges(Point2D point)
    {
        List<Edge2D> visibleEdges = new List<Edge2D>();
        foreach (Triangle2D triangle in triangles)
        {
            for (int i = 0; i < 3; i++)
            {
                Edge2D edge = triangle.edges[i];
                if (IsEdgeVisible(edge, point))
                {
                    visibleEdges.Add(edge);
                }
            }
        }
        return visibleEdges;
    }

    private bool IsEdgeVisible(Edge2D edge, Point2D point)
    {
        Vector2 edgeVector = edge.end.position - edge.start.position;
        Vector2 pointVector = point.position - edge.start.position;
        return Vector3.Cross(new Vector3(edgeVector.x, edgeVector.y, 0),
                           new Vector3(pointVector.x, pointVector.y, 0)).z > 0;
    }

    private void FlipEdge(Triangle2D t1, Triangle2D t2, Edge2D edge)
    {
        Point2D p1 = GetOppositeVertex(t1, edge);
        Point2D p2 = GetOppositeVertex(t2, edge);

        if (p1 == null || p2 == null) return;

        triangles.Remove(t1);
        triangles.Remove(t2);

        Triangle2D newT1 = new Triangle2D(p1, edge.start, p2);
        Triangle2D newT2 = new Triangle2D(p2, edge.end, p1);

        SetupTriangleEdges(newT1);
        SetupTriangleEdges(newT2);

        triangles.Add(newT1);
        triangles.Add(newT2);
    }

    private Point2D GetOppositeVertex(Triangle2D triangle, Edge2D edge)
    {
        foreach (Point2D vertex in triangle.vertices)
        {
            if (vertex != edge.start && vertex != edge.end)
                return vertex;
        }
        return null;
    }

    private bool IsPointInTriangle(Vector2 p, Triangle2D t)
    {
        Vector2 a = t.vertices[0].position;
        Vector2 b = t.vertices[1].position;
        Vector2 c = t.vertices[2].position;

        float d1 = Sign(p, a, b);
        float d2 = Sign(p, b, c);
        float d3 = Sign(p, c, a);

        bool hasNeg = (d1 < 0) || (d2 < 0) || (d3 < 0);
        bool hasPos = (d1 > 0) || (d2 > 0) || (d3 > 0);

        return !(hasNeg && hasPos);
    }

    private float Sign(Vector2 p1, Vector2 p2, Vector2 p3)
    {
        return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
    }

    private void UpdateEdges()
    {
        foreach (Triangle2D triangle in triangles)
        {
            SetupTriangleEdges(triangle);
        }
    }

    private Edge2D GetSharedEdge(Triangle2D t1, Triangle2D t2)
    {
        for (int i = 0; i < 3; i++)
        {
            Point2D start1 = t1.vertices[i];
            Point2D end1 = t1.vertices[(i + 1) % 3];

            for (int j = 0; j < 3; j++)
            {
                Point2D start2 = t2.vertices[j];
                Point2D end2 = t2.vertices[(j + 1) % 3];

                if ((Vector2.Distance(start1.position, start2.position) < 0.0001f &&
                     Vector2.Distance(end1.position, end2.position) < 0.0001f) ||
                    (Vector2.Distance(start1.position, end2.position) < 0.0001f &&
                     Vector2.Distance(end1.position, start2.position) < 0.0001f))
                {
                    return new Edge2D(start1, end1);
                }
            }
        }
        return null;
    }

    private void DrawTriangulation()
    {
        if (triangles == null || triangles.Count == 0)
        {
            Debug.LogWarning("No triangles to draw");
            return;
        }

        Debug.Log($"Drawing {triangles.Count} triangles");

        List<(Vector2, Vector2)> allEdges = new List<(Vector2, Vector2)>();
        HashSet<string> processedEdges = new HashSet<string>();

        foreach (Triangle2D triangle in triangles)
        {
            for (int i = 0; i < 3; i++)
            {
                Vector2 start = triangle.vertices[i].position;
                Vector2 end = triangle.vertices[(i + 1) % 3].position;

                string edgeKey = GetUniqueEdgeKey(start, end);

                if (!processedEdges.Contains(edgeKey))
                {
                    allEdges.Add((start, end));
                    processedEdges.Add(edgeKey);
                }
            }
        }

        List<Vector3> positions = new List<Vector3>();
        foreach (var edge in allEdges)
        {
            positions.Add(new Vector3(edge.Item1.x, edge.Item1.y, 0));
            positions.Add(new Vector3(edge.Item2.x, edge.Item2.y, 0));
        }

        if (lineRenderer != null)
        {
            lineRenderer.positionCount = positions.Count;
            lineRenderer.SetPositions(positions.ToArray());
            lineRenderer.startWidth = 0.1f;
            lineRenderer.endWidth = 0.1f;
            lineRenderer.startColor = Color.yellow;
            lineRenderer.endColor = Color.yellow;
            lineRenderer.enabled = true;
        }
        else
        {
            Debug.LogError("LineRenderer is null in DrawTriangulation");
        }
    }

    private string GetUniqueEdgeKey(Vector2 p1, Vector2 p2)
    {
        return p1.x < p2.x || (p1.x == p2.x && p1.y < p2.y)
            ? $"{p1.x:F6},{p1.y:F6}-{p2.x:F6},{p2.y:F6}"
            : $"{p2.x:F6},{p2.y:F6}-{p1.x:F6},{p1.y:F6}";
    }


    public void Clear()
    {
        points.Clear();
        triangles.Clear();
        if (lineRenderer != null)
        {
            lineRenderer.positionCount = 0;
        }
    }

    public void SetVisualizationActive(bool active)
    {
        if (lineRenderer != null)
        {
            lineRenderer.enabled = active;
            if (active)
            {
                lineRenderer.startWidth = 0.1f;
                lineRenderer.endWidth = 0.1f;
                lineRenderer.startColor = Color.yellow;
                lineRenderer.endColor = Color.yellow;
            }
        }
        enabled = active;
    }

    private void OnDrawGizmos()
    {
        if (!enabled) return;

        Gizmos.color = Color.blue;
        if (points != null)
        {
            foreach (Point2D point in points)
            {
                Gizmos.DrawSphere(new Vector3(point.position.x, point.position.y, 0), 0.1f);
            }
        }

        if (triangles != null)
        {
            Gizmos.color = new Color(1, 0, 0, 0.2f);
            foreach (Triangle2D t in triangles)
            {
                Vector2 center = GeometryUtils.ComputeCircumcenter(t);
                float radius = Vector2.Distance(center, t.vertices[0].position);
                DrawCircle(center, radius);
            }
        }
    }

    private void DrawCircle(Vector2 center, float radius)
    {
        int segments = 32;
        Vector3 prevPoint = new Vector3(center.x + radius, center.y, 0);

        for (int i = 1; i <= segments; i++)
        {
            float angle = i * 2 * Mathf.PI / segments;
            Vector3 nextPoint = new Vector3(
                center.x + radius * Mathf.Cos(angle),
                center.y + radius * Mathf.Sin(angle),
                0);
            Gizmos.DrawLine(prevPoint, nextPoint);
            prevPoint = nextPoint;
        }
    }
}