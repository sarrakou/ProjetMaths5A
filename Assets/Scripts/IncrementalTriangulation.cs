using UnityEngine;
using System.Collections.Generic;

public class IncrementalTriangulation : MonoBehaviour
{
    private List<Point2D> points = new List<Point2D>();
    private List<Edge2D> edges = new List<Edge2D>();
    private List<Triangle2D> triangles = new List<Triangle2D>();
    private LineRenderer lineRenderer;
    [SerializeField] private Material lineMaterial;

    public void SetLineRenderer(LineRenderer lr)
    {
        lineRenderer = lr;
    }

    private void OnMouseDown()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        AddPoint(new Vector2(mousePos.x, mousePos.y));
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

        Triangle2D containingTriangle = FindContainingTriangle(newPoint);

        if (containingTriangle != null)
        {
            SplitTriangle(containingTriangle, newPoint);
        }
        else
        {
            Debug.Log("Point is outside triangulation");
            AddPointOutside(newPoint);
        }

        UpdateVisualization();
    }

    private void CreateInitialTriangle()
    {
        if (!IsCounterClockwise(points[0].position, points[1].position, points[2].position))
        {
            Point2D temp = points[1];
            points[1] = points[2];
            points[2] = temp;
        }

        Triangle2D triangle = new Triangle2D(points[0], points[1], points[2]);

        triangle.edges[0] = new Edge2D(points[0], points[1]);
        triangle.edges[1] = new Edge2D(points[1], points[2]);
        triangle.edges[2] = new Edge2D(points[2], points[0]);

        edges.AddRange(triangle.edges);

        triangles.Add(triangle);

        UpdateVisualization();
    }

    private Triangle2D FindContainingTriangle(Point2D point)
    {
        foreach (Triangle2D triangle in triangles)
        {
            if (IsPointInTriangle(point.position, triangle))
            {
                Debug.Log($"Found containing triangle!"); 
                return triangle;
            }
        }
        Debug.Log($"Point is outside all triangles");  
        return null;
    }

    private void SplitTriangle(Triangle2D triangle, Point2D newPoint)
    {
        Debug.Log("Splitting triangle");

        Triangle2D[] originalNeighbors = new Triangle2D[3];
        Edge2D[] originalEdges = new Edge2D[3];

        for (int i = 0; i < 3; i++)
        {
            originalNeighbors[i] = triangle.neighbors[i];
            originalEdges[i] = triangle.edges[i];
        }

        triangles.Remove(triangle);

        Triangle2D[] newTriangles = new Triangle2D[3];
        newTriangles[0] = new Triangle2D(triangle.vertices[0], triangle.vertices[1], newPoint);
        newTriangles[1] = new Triangle2D(triangle.vertices[1], triangle.vertices[2], newPoint);
        newTriangles[2] = new Triangle2D(triangle.vertices[2], triangle.vertices[0], newPoint);

        for (int i = 0; i < 3; i++)
        {
            SetupTriangleEdges(newTriangles[i]);
        }

        newTriangles[0].neighbors[2] = newTriangles[1];
        newTriangles[1].neighbors[2] = newTriangles[2];
        newTriangles[2].neighbors[2] = newTriangles[0];

        for (int i = 0; i < 3; i++)
        {
            Triangle2D neighbor = originalNeighbors[i];
            if (neighbor != null)
            {
                Edge2D originalEdge = originalEdges[i];
                for (int j = 0; j < 3; j++)
                {
                    if (SharesEdge(newTriangles[j], originalEdge))
                    {
                        ConnectTriangles(newTriangles[j], neighbor);
                        break;
                    }
                }
            }
        }

        triangles.AddRange(newTriangles);
    }

    private void SetupTriangleEdges(Triangle2D triangle)
    {
        if (triangle == null)
        {
            Debug.LogError("Cannot setup edges for null triangle");
            return;
        }

        triangle.edges = new Edge2D[3];
        for (int i = 0; i < 3; i++)
        {
            Point2D start = triangle.vertices[i];
            Point2D end = triangle.vertices[(i + 1) % 3]; 

            if (start == null || end == null)
            {
                Debug.LogError($"Triangle vertex {i} is null");
                continue;
            }

            Edge2D edge = new Edge2D(start, end);
            triangle.edges[i] = edge;

            start.edges.Add(edge);
            end.edges.Add(edge);
        }

        if (triangle.neighbors == null)
        {
            triangle.neighbors = new Triangle2D[3];
        }
    }

    private bool SharesEdge(Triangle2D triangle, Edge2D edge)
    {
        foreach (Edge2D triangleEdge in triangle.edges)
        {
            if (EdgesEqual(triangleEdge, edge))
                return true;
        }
        return false;
    }

    private bool EdgesEqual(Edge2D e1, Edge2D e2)
    {
        return (Vector2.Distance(e1.start.position, e2.start.position) < 0.0001f &&
                Vector2.Distance(e1.end.position, e2.end.position) < 0.0001f) ||
               (Vector2.Distance(e1.start.position, e2.end.position) < 0.0001f &&
                Vector2.Distance(e1.end.position, e2.start.position) < 0.0001f);
    }

    private void ConnectTriangles(Triangle2D t1, Triangle2D t2)
    {
        Edge2D sharedEdge = null;
        int t1EdgeIndex = -1;
        int t2EdgeIndex = -1;

        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                if (EdgesEqual(t1.edges[i], t2.edges[j]))
                {
                    sharedEdge = t1.edges[i];
                    t1EdgeIndex = i;
                    t2EdgeIndex = j;
                    break;
                }
            }
            if (sharedEdge != null) break;
        }

        if (t1EdgeIndex != -1 && t2EdgeIndex != -1)
        {
            t1.neighbors[t1EdgeIndex] = t2;
            t2.neighbors[t2EdgeIndex] = t1;
        }
    }

    private void AddPointOutside(Point2D point)
    {
        List<Edge2D> visibleEdges = FindVisibleEdges(point);

        foreach (Edge2D edge in visibleEdges)
        {
            Triangle2D newTriangle = new Triangle2D(edge.start, edge.end, point);
            triangles.Add(newTriangle);
        }

        UpdateEdgeList();
    }

    private void UpdateTriangleNeighbors(Triangle2D existingTriangle, Triangle2D newTriangle)
    {
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                if (ShareEdge(existingTriangle, i, newTriangle, j))
                {
                    existingTriangle.neighbors[i] = newTriangle;
                    newTriangle.neighbors[j] = existingTriangle;

                    Edge2D sharedEdge = existingTriangle.edges[i];
                    if (sharedEdge.leftTriangle == null)
                    {
                        sharedEdge.leftTriangle = existingTriangle;
                        sharedEdge.rightTriangle = newTriangle;
                    }
                    else
                    {
                        sharedEdge.rightTriangle = newTriangle;
                    }
                }
            }
        }
    }

    private List<Edge2D> FindVisibleEdges(Point2D point)
    {
        List<Edge2D> visibleEdges = new List<Edge2D>();
        foreach (Edge2D edge in edges)
        {
            if (IsEdgeVisible(edge, point))
            {
                visibleEdges.Add(edge);
            }
        }
        return visibleEdges;
    }

    private bool IsEdgeVisible(Edge2D edge, Point2D point)
    {
        Vector2 edgeVector = edge.end.position - edge.start.position;
        Vector2 pointVector = point.position - edge.start.position;
        return Vector3.Cross(edgeVector, pointVector).z > 0;
    }

    private void UpdateNeighborRelationships(Triangle2D t1, Triangle2D t2, Triangle2D t3)
    {
        SetTriangleNeighbors(t1, t2);
        SetTriangleNeighbors(t2, t3);
        SetTriangleNeighbors(t3, t1);

        foreach (Triangle2D existing in triangles)
        {
            UpdateTriangleNeighbors(existing, t1);
            UpdateTriangleNeighbors(existing, t2);
            UpdateTriangleNeighbors(existing, t3);
        }
    }

    private bool IsCounterClockwise(Vector2 a, Vector2 b, Vector2 c)
    {
        return ((b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x)) > 0;
    }

    private void SetTriangleNeighbors(Triangle2D t1, Triangle2D t2)
    {
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                if (ShareEdge(t1, i, t2, j))
                {
                    t1.neighbors[i] = t2;
                    t2.neighbors[j] = t1;
                }
            }
        }
    }

    private bool ShareEdge(Triangle2D t1, int edge1, Triangle2D t2, int edge2)
    {
        Point2D start1 = t1.vertices[edge1];
        Point2D end1 = t1.vertices[(edge1 + 1) % 3];
        Point2D start2 = t2.vertices[edge2];
        Point2D end2 = t2.vertices[(edge2 + 1) % 3];

        return (start1 == start2 && end1 == end2) || (start1 == end2 && end1 == start2);
    }

    private void UpdateEdgeList()
    {
        edges.Clear();
        foreach (Triangle2D triangle in triangles)
        {
            for (int i = 0; i < 3; i++)
            {
                Edge2D edge = new Edge2D(triangle.vertices[i], triangle.vertices[(i + 1) % 3]);
                if (!edges.Contains(edge))
                {
                    edges.Add(edge);
                }
            }
        }
    }

    private void UpdateVisualization()
    {
        List<Vector3> positions = new List<Vector3>();
        foreach (Triangle2D triangle in triangles)
        {
            positions.Add(new Vector3(triangle.vertices[0].position.x, triangle.vertices[0].position.y, 0));
            positions.Add(new Vector3(triangle.vertices[1].position.x, triangle.vertices[1].position.y, 0));
            positions.Add(new Vector3(triangle.vertices[2].position.x, triangle.vertices[2].position.y, 0));
            positions.Add(new Vector3(triangle.vertices[0].position.x, triangle.vertices[0].position.y, 0));
        }

        lineRenderer.positionCount = positions.Count;
        lineRenderer.SetPositions(positions.ToArray());
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

    public void Clear()
    {
        points.Clear();
        edges.Clear();
        triangles.Clear();
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
        foreach (Point2D point in points)  
        {
            Gizmos.DrawSphere(new Vector3(point.position.x, point.position.y, 0), 0.1f);
        }
    }
}