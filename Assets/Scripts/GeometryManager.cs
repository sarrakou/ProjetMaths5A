using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class GeometryManager : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown algorithmDropdown;
    [SerializeField] private Button clearButton;
    [SerializeField] private Toggle visualizationToggle;

    [SerializeField] private MarcheDeJarvis jarvisMarch;
    [SerializeField] private GrahamScan grahamScan;
    [SerializeField] private IncrementalTriangulation triangulation;
    [SerializeField] private DeLaunay delaunay;
    [SerializeField] private Voronoi2D voronoi;

    private MonoBehaviour currentAlgorithm;
    private bool showVisualization = true;
    [SerializeField] private Material lineRendererMaterial;


    private void Awake()
    {
        if (GetComponent<BoxCollider2D>() == null)
        {
            BoxCollider2D collider = gameObject.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(20, 20); 
        }

        SetupLineRenderer();

        DisableAllAlgorithms();
        SetupUI();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0) && !IsMouseOverUI())
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 point = new Vector2(mousePos.x, mousePos.y);

            Debug.Log($"Click detected, current algorithm: {currentAlgorithm?.GetType().Name}"); // Add this log

            switch (currentAlgorithm)
            {
                case MarcheDeJarvis jm:
                    jm.AddPoint(point);
                    break;
                case GrahamScan gs:
                    gs.AddPoint(point);
                    break;
                case IncrementalTriangulation it:
                    it.AddPoint(point);
                    break;
                case DeLaunay dt:
                    dt.AddPoint(point);
                    break;
                case Voronoi2D vd:
                    vd.GetComponent<DeLaunay>().AddPoint(point);
                    vd.GenerateVoronoiFromDelaunay();
                    break;
            }
        }
    }

    private bool IsMouseOverUI()
    {
        return EventSystem.current.IsPointerOverGameObject();
    }

    private void SetupLineRenderer()
    {
        try
        {
            LineRenderer jarvisLR = CreateLineRenderer("JarvisLineRenderer", Color.blue);
            LineRenderer grahamLR = CreateLineRenderer("GrahamLineRenderer", Color.green);
            LineRenderer triangulationLR = CreateLineRenderer("TriangulationLineRenderer", Color.red);
            LineRenderer delaunayLR = CreateLineRenderer("DelaunayLineRenderer", Color.black);
            LineRenderer voronoiLR = CreateLineRenderer("VoronoiLineRenderer", Color.magenta);

            if (jarvisMarch != null) jarvisMarch.SetLineRenderer(jarvisLR);
            if (grahamScan != null) grahamScan.SetLineRenderer(grahamLR);
            if (triangulation != null) triangulation.SetLineRenderer(triangulationLR);
            if (delaunay != null) delaunay.SetLineRenderer(delaunayLR);
            if (voronoi != null) voronoi.SetLineRenderer(voronoiLR);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in SetupLineRenderer: {e.Message}");
        }
    }

    private LineRenderer CreateLineRenderer(string name, Color color)
    {
        GameObject lineObj = new GameObject(name);
        lineObj.transform.SetParent(transform);

        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.startWidth = 0.1f;
        lr.endWidth = 0.1f;
        lr.startColor = color;
        lr.endColor = color;
        lr.positionCount = 0;

        lr.sortingOrder = 1;

        if (name.Contains("Delaunay"))
        {
            lr.startWidth = 0.15f;
            lr.endWidth = 0.15f;
            lr.startColor = Color.black;
            lr.endColor = Color.black;
        }

        if (lineRendererMaterial != null)
        {
            lr.material = lineRendererMaterial;
        }
        else
        {
            lr.material = new Material(Shader.Find("Sprites/Default"));
        }

        return lr;
    }
    private void SetupUI()
    {
        algorithmDropdown.options.Clear();
        algorithmDropdown.options.Add(new TMP_Dropdown.OptionData("Jarvis March"));
        algorithmDropdown.options.Add(new TMP_Dropdown.OptionData("Graham Scan"));
        algorithmDropdown.options.Add(new TMP_Dropdown.OptionData("Incremental Triangulation"));
        algorithmDropdown.options.Add(new TMP_Dropdown.OptionData("Delaunay Triangulation"));
        algorithmDropdown.options.Add(new TMP_Dropdown.OptionData("Voronoi Diagram"));

        algorithmDropdown.onValueChanged.AddListener(OnAlgorithmChanged);
        clearButton.onClick.AddListener(ClearAll);
        visualizationToggle.onValueChanged.AddListener(ToggleVisualization);

        OnAlgorithmChanged(0);
    }

    private void DisableAllAlgorithms()
    {
        jarvisMarch.enabled = false;
        grahamScan.enabled = false;
        triangulation.enabled = false;
        delaunay.enabled = false;
        voronoi.enabled = false;
    }

    private void OnAlgorithmChanged(int index)
    {
        DisableAllAlgorithms();
        Debug.Log($"Changing to algorithm index: {index}");

        if (jarvisMarch != null) jarvisMarch.GetComponent<LineRenderer>().enabled = false;
        if (grahamScan != null) grahamScan.GetComponent<LineRenderer>().enabled = false;
        if (triangulation != null) triangulation.GetComponent<LineRenderer>().enabled = false;
        if (delaunay != null) delaunay.GetComponent<LineRenderer>().enabled = false;
        if (voronoi != null) voronoi.GetComponent<LineRenderer>().enabled = false;

        switch (index)
        {
            case 0:
                currentAlgorithm = jarvisMarch;
                jarvisMarch.GetComponent<LineRenderer>().enabled = true;
                break;
            case 1:
                currentAlgorithm = grahamScan;
                grahamScan.GetComponent<LineRenderer>().enabled = true;
                break;
            case 2:
                currentAlgorithm = triangulation;
                triangulation.GetComponent<LineRenderer>().enabled = true;
                break;
            case 3:
                currentAlgorithm = delaunay;
                delaunay.GetComponent<LineRenderer>().enabled = true;
                break;
            case 4: 
                currentAlgorithm = voronoi;
                delaunay.GetComponent<LineRenderer>().enabled = true;

                voronoi.GetComponent<LineRenderer>().enabled = true;
                voronoi.GenerateVoronoiFromDelaunay();
                break;
        }

        if (currentAlgorithm != null)
        {
            currentAlgorithm.enabled = true;
            Debug.Log($"Current algorithm enabled: {currentAlgorithm.GetType().Name}");
        }
    }

    private void ToggleVisualization(bool show)
    {
        showVisualization = show;
        if (currentAlgorithm != null)
        {
            switch (currentAlgorithm)
            {
                case MarcheDeJarvis jm:
                    jm.SetVisualizationActive(show);
                    break;
                case GrahamScan gs:
                    gs.SetVisualizationActive(show);
                    break;
                case IncrementalTriangulation it:
                    it.SetVisualizationActive(show);
                    break;
                case DeLaunay dt:
                    dt.SetVisualizationActive(show);
                    break;
                case Voronoi2D vd:
                    vd.SetVisualizationActive(show);
                    break;
            }
        }
    }

    private void ClearAll()
    {
        switch (currentAlgorithm)
        {
            case MarcheDeJarvis jm:
                jm.Clear();
                break;
            case GrahamScan gs:
                gs.Clear();
                break;
            case IncrementalTriangulation it:
                it.Clear();
                break;
            case DeLaunay dt:
                dt.Clear();
                break;
            case Voronoi2D vd:
                vd.Clear();
                break;
        }
    }

}