using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GeometryUtils : MonoBehaviour
{
    public static bool IsPointInCircumcircle(Triangle2D triangle, Point2D point)
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

        float det = (
            (ax * ax + ay * ay) * (bx * cy - cx * by) -
            (bx * bx + by * by) * (ax * cy - cx * ay) +
            (cx * cx + cy * cy) * (ax * by - bx * ay)
        );

        return det > 0;
    }

    public static Vector2 ComputeCircumcenter(Triangle2D triangle)
    {
        Vector2 a = triangle.vertices[0].position;
        Vector2 b = triangle.vertices[1].position;
        Vector2 c = triangle.vertices[2].position;

        float d = 2 * (a.x * (b.y - c.y) + b.x * (c.y - a.y) + c.x * (a.y - b.y));

        float x = (
            (a.x * a.x + a.y * a.y) * (b.y - c.y) +
            (b.x * b.x + b.y * b.y) * (c.y - a.y) +
            (c.x * c.x + c.y * c.y) * (a.y - b.y)
        ) / d;

        float y = (
            (a.x * a.x + a.y * a.y) * (c.x - b.x) +
            (b.x * b.x + b.y * b.y) * (a.x - c.x) +
            (c.x * c.x + c.y * c.y) * (b.x - a.x)
        ) / d;

        return new Vector2(x, y);
    }
}
