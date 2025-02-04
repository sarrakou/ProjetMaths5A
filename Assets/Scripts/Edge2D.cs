using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Edge2D 
{
    public Point2D start;
    public Point2D end;
    public Triangle2D leftTriangle;
    public Triangle2D rightTriangle;

    public Edge2D(Point2D s, Point2D e)
    {
        start = s;
        end = e;
    }

    public Vector2 AsVector()
    {
        return end.position - start.position;
    }
}
