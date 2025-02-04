public class Triangle2D : System.IEquatable<Triangle2D>
{
    public readonly System.Guid Id;

    public override bool Equals(object obj)
    {
        return Equals(obj as Triangle2D);
    }

    public bool Equals(Triangle2D other)
    {
        if (other == null) return false;
        return Id.Equals(other.Id);
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    public static bool operator ==(Triangle2D left, Triangle2D right)
    {
        if (ReferenceEquals(left, null)) return ReferenceEquals(right, null);
        return left.Equals(right);
    }

    public static bool operator !=(Triangle2D left, Triangle2D right)
    {
        return !(left == right);
    }
    public Point2D[] vertices;
    public Edge2D[] edges;
    public Triangle2D[] neighbors;

    public Triangle2D(Point2D a, Point2D b, Point2D c)
    {
        Id = System.Guid.NewGuid(); 
        vertices = new Point2D[] { a, b, c };
        edges = new Edge2D[3];
        neighbors = new Triangle2D[3];
        OrientCounterClockwise();
    }

    private void OrientCounterClockwise()
    {
        float crossProduct = UnityEngine.Vector3.Cross(
            vertices[1].position - vertices[0].position,
            vertices[2].position - vertices[0].position
        ).z;

        if (crossProduct < 0)
        {
            var temp = vertices[1];
            vertices[1] = vertices[2];
            vertices[2] = temp;
        }
    }
}