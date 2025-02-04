using System.Collections.Generic;
using UnityEngine;

public class Voronoi2D : MonoBehaviour
{
    private DeLaunay delaunay;
    private LineRenderer lineRenderer;
    private List<Vector2> voronoiVertices = new List<Vector2>();
    private List<Edge2D> voronoiEdges = new List<Edge2D>();

    private float screenLeft;
    private float screenRight;
    private float screenTop;
    private float screenBottom;

    [SerializeField] private Material lineMaterial;


    private void Awake()
    {
        delaunay = GetComponent<DeLaunay>();
    }

    public void SetLineRenderer(LineRenderer lr)
    {
        if (lr == null)
        {
            Debug.LogError("Null LineRenderer passed to Voronoi");
            return;
        }

        lineRenderer = lr;
        ConfigureLineRenderer();
        Debug.Log("Voronoi LineRenderer set successfully");
    }

    private void ConfigureLineRenderer()
    {
        if (lineRenderer == null)
        {
            Debug.LogError("Cannot configure null LineRenderer");
            return;
        }

        int currentPositionCount = lineRenderer.positionCount;

        try
        {
            lineRenderer.useWorldSpace = true;
            lineRenderer.startWidth = 0.15f;  
            lineRenderer.endWidth = 0.15f;

            Color magenta = new Color(1f, 0f, 1f, 1f);
            lineRenderer.startColor = magenta;
            lineRenderer.endColor = magenta;

            lineRenderer.sortingOrder = 3; 
            lineRenderer.allowOcclusionWhenDynamic = false;  

            if (lineMaterial != null)
            {
                lineRenderer.material = lineMaterial;
            }
            else
            {
                Material defaultMat = new Material(Shader.Find("Sprites/Default"));
                defaultMat.color = magenta;
                lineRenderer.material = defaultMat;
            }

            lineRenderer.enabled = true;

            Debug.Log($"LineRenderer configured: Width={lineRenderer.startWidth}, " +
                     $"Color={lineRenderer.startColor}, SortingOrder={lineRenderer.sortingOrder}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error configuring LineRenderer: {e.Message}");
        }

        // Restore position count
        lineRenderer.positionCount = currentPositionCount;
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

                const float EPSILON = 0.0001f;
                if ((Vector2.Distance(start1.position, start2.position) < EPSILON &&
                     Vector2.Distance(end1.position, end2.position) < EPSILON) ||
                    (Vector2.Distance(start1.position, end2.position) < EPSILON &&
                     Vector2.Distance(end1.position, start2.position) < EPSILON))
                {
                    return new Edge2D(start1, end1);
                }
            }
        }

        return null;
    }
    public void Clear()
    {
        voronoiVertices.Clear();
        voronoiEdges.Clear();
        if (lineRenderer != null)
        {
            lineRenderer.positionCount = 0;
        }
    }
    private void UpdateScreenBounds()
    {
        Vector3 bottomLeft = Camera.main.ViewportToWorldPoint(new Vector3(0, 0, 0));
        Vector3 topRight = Camera.main.ViewportToWorldPoint(new Vector3(1, 1, 0));

        screenLeft = bottomLeft.x;
        screenRight = topRight.x;
        screenBottom = bottomLeft.y;
        screenTop = topRight.y;
    }

    public void GenerateVoronoiFromDelaunay()
    {
        if (delaunay == null || delaunay.triangles == null || delaunay.triangles.Count < 1)
        {
            Debug.LogWarning("No Delaunay triangulation available");
            return;
        }

        UpdateScreenBounds();
        voronoiVertices.Clear();
        voronoiEdges.Clear();

        Dictionary<Triangle2D, Vector2> circumcenters = new Dictionary<Triangle2D, Vector2>();
        foreach (Triangle2D triangle in delaunay.triangles)
        {
            Vector2 circumcenter = GeometryUtils.ComputeCircumcenter(triangle);
            circumcenters[triangle] = circumcenter;
            voronoiVertices.Add(circumcenter);
        }

        HashSet<string> processedPairs = new HashSet<string>();
        HashSet<Edge2D> convexHullEdges = GetConvexHullEdges();

        foreach (Triangle2D t1 in delaunay.triangles)
        {
            for (int i = 0; i < 3; i++)
            {
                Triangle2D t2 = t1.neighbors[i];
                string pairKey = t2 != null ? GetTrianglePairKey(t1, t2) : "";

                if (t2 != null && !processedPairs.Contains(pairKey))
                {
                    Edge2D sharedEdge = GetSharedEdge(t1, t2);
                    if (sharedEdge != null)
                    {
                        AddVoronoiEdge(circumcenters[t1], circumcenters[t2]);
                        processedPairs.Add(pairKey);
                    }
                }
                else if (t2 == null)
                {
                    Edge2D hullEdge = t1.edges[i];
                    Vector2 circumcenter = circumcenters[t1];
                    ExtendEdgeToScreenBoundary(t1, hullEdge, circumcenter);
                }
            }
        }

        DrawVoronoi();
    }

    public void SetVisualizationActive(bool active)
    {
        if (lineRenderer != null)
        {
            lineRenderer.enabled = active;
        }
        enabled = active;
    }

    private void ExtendEdgeToScreenBoundary(Triangle2D triangle, Edge2D hullEdge, Vector2 circumcenter)
    {
        Vector2 edgeDir = (hullEdge.end.position - hullEdge.start.position).normalized;
        Vector2 perpDir = new Vector2(-edgeDir.y, edgeDir.x);

        Point2D oppositeVertex = GetOppositeVertex(triangle, hullEdge);
        if (oppositeVertex != null)
        {
            Vector2 toOpposite = oppositeVertex.position - hullEdge.start.position;
            if (Vector2.Dot(toOpposite, perpDir) < 0)
            {
                perpDir = -perpDir;
            }
        }

        Vector2 intersectionPoint = FindScreenIntersection(circumcenter, perpDir);
        AddVoronoiEdge(circumcenter, intersectionPoint);
    }

    private Vector2 FindScreenIntersection(Vector2 start, Vector2 direction)
    {
        float t = float.MaxValue;
        Vector2 intersection = start;

        // Check intersection with each screen boundary
        // Left boundary
        if (direction.x != 0)
        {
            float tLeft = (screenLeft - start.x) / direction.x;
            if (tLeft > 0 && tLeft < t)
            {
                float y = start.y + tLeft * direction.y;
                if (y >= screenBottom && y <= screenTop)
                {
                    t = tLeft;
                    intersection = new Vector2(screenLeft, y);
                }
            }
        }

        // Right boundary
        if (direction.x != 0)
        {
            float tRight = (screenRight - start.x) / direction.x;
            if (tRight > 0 && tRight < t)
            {
                float y = start.y + tRight * direction.y;
                if (y >= screenBottom && y <= screenTop)
                {
                    t = tRight;
                    intersection = new Vector2(screenRight, y);
                }
            }
        }

        // Bottom boundary
        if (direction.y != 0)
        {
            float tBottom = (screenBottom - start.y) / direction.y;
            if (tBottom > 0 && tBottom < t)
            {
                float x = start.x + tBottom * direction.x;
                if (x >= screenLeft && x <= screenRight)
                {
                    t = tBottom;
                    intersection = new Vector2(x, screenBottom);
                }
            }
        }

        // Top boundary
        if (direction.y != 0)
        {
            float tTop = (screenTop - start.y) / direction.y;
            if (tTop > 0 && tTop < t)
            {
                float x = start.x + tTop * direction.x;
                if (x >= screenLeft && x <= screenRight)
                {
                    t = tTop;
                    intersection = new Vector2(x, screenTop);
                }
            }
        }

        return intersection;
    }

    private void AddVoronoiEdge(Vector2 start, Vector2 end)
    {
        voronoiEdges.Add(new Edge2D(
            new Point2D(start),
            new Point2D(end)
        ));
    }

    private HashSet<Edge2D> GetConvexHullEdges()
    {
        HashSet<Edge2D> convexHullEdges = new HashSet<Edge2D>();
        foreach (Triangle2D triangle in delaunay.triangles)
        {
            for (int i = 0; i < 3; i++)
            {
                if (triangle.neighbors[i] == null)
                {
                    convexHullEdges.Add(triangle.edges[i]);
                }
            }
        }
        return convexHullEdges;
    }

    private string GetTrianglePairKey(Triangle2D t1, Triangle2D t2)
    {
        return t1.Id.CompareTo(t2.Id) < 0
            ? $"{t1.Id}-{t2.Id}"
            : $"{t2.Id}-{t1.Id}";
    }

    private void DrawVoronoi()
    {
        if (lineRenderer == null) return;

        List<Vector3> positions = new List<Vector3>();
        foreach (Edge2D edge in voronoiEdges)
        {
            positions.Add(new Vector3(edge.start.position.x, edge.start.position.y, 0));
            positions.Add(new Vector3(edge.end.position.x, edge.end.position.y, 0));
        }

        lineRenderer.positionCount = positions.Count;
        lineRenderer.SetPositions(positions.ToArray());
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
}