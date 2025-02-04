using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Point2D
{
    public Vector2 position;
    public List<Edge2D> edges;

    public Point2D(Vector2 pos)
    {
        position = pos;
        edges = new List<Edge2D>();
    }
}
