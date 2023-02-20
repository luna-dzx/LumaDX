using OpenTK.Mathematics;

namespace LumaDX;

public static class ObjectExtensions
{
    public static void AddItems<T>(this List<T> list, params T[] values) => list.AddRange(values);
    public static void AddVec3(this List<float> list, Vector3 vector) => list.AddItems(vector.X,vector.Y,vector.Z);

    public static Vector3 GetVertex(this List<float> vertices, int index) => new (vertices[index * 3], vertices[index * 3 + 1], vertices[index * 3 + 2]);
    public static Vector3 GetVertex(this float[] vertices, int index) => new (vertices[index * 3], vertices[index * 3 + 1], vertices[index * 3 + 2]);


}

class OrderlessIntPairs : IEqualityComparer<(int, int)>
{
    public bool Equals((int, int) x, (int, int) y)
    {
        return (x.Item1 == y.Item1 && x.Item2 == y.Item2) ||
               (x.Item1 == y.Item2 && x.Item2 == y.Item1);
    }

    public int GetHashCode((int, int) obj)
    {
        return obj.Item1 ^ obj.Item2;
    }
}