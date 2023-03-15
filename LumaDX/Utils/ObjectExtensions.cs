using Assimp;
using OpenTK.Mathematics;

namespace LumaDX;

public static class ObjectExtensions
{
    public static void AddItems<T>(this List<T> list, params T[] values) => list.AddRange(values);
    public static void AddVec3(this List<float> list, Vector3 vector) => list.AddItems(vector.X,vector.Y,vector.Z);

    public static Vector3 GetVertex(this List<float> vertices, int index) => new (vertices[index * 3], vertices[index * 3 + 1], vertices[index * 3 + 2]);
    public static Vector3 GetVertex(this float[] vertices, int index) => new (vertices[index * 3], vertices[index * 3 + 1], vertices[index * 3 + 2]);

    public static void SetVertex(this float[] vertices, int index, Vector3 vertex)
    {
        vertices[index * 3] = vertex.X;
        vertices[index * 3 + 1] = vertex.Y;
        vertices[index * 3 + 2] = vertex.Z;
    }

    public static Vector3[] GetVertices(this float[] floats)
    {
        int length = floats.Length / 3;
        Vector3[] vertices = new Vector3[length];
        for (int i = 0; i < length; i++) vertices[i] = floats.GetVertex(i);
        return vertices;
    }

    public static Maths.Triangle GetTriangle(this Vector3[] vertices, int index,Vector3 eRadius = default)
    {
        if (eRadius == default) eRadius = Vector3.One;
        return new Maths.Triangle(vertices[index*3],vertices[index*3 +1],vertices[index*3 +2],eRadius);
    }
    
    public static Maths.Triangle[] GetTriangles(this Vector3[] vertices, Vector3 eRadius = default)
    {
        if (eRadius == default) eRadius = Vector3.One;
        int maxTriangles = vertices.Length / 3;
        List<Maths.Triangle> triangles = new List<Maths.Triangle>(maxTriangles);
        for (int i = 0; i < maxTriangles; i++)
        {
            var triangle = vertices.GetTriangle(i, eRadius);
            if (triangle.Plane.Normal != Vector3.Zero) triangles.Add(triangle);
        }

        return triangles.ToArray();
    }
    

    public static Vector3[] Decompress(this Vector3[] vertices, int[] indices)
    {
        Vector3[] output = new Vector3[indices.Length];
        for (int i = 0; i < indices.Length; i++) output[i] = vertices[indices[i]];
        return output;
    }

    public static Vector3 Transform(this Vector3 vector, Matrix4 matrix)
    {
        return (matrix * new Vector4(vector, 1f)).Xyz;
    }
    
    public static Vector3[] Transform(this Vector3[] vectors, Matrix4 matrix)
    {
        return vectors.Select(v => v.Transform(matrix)).ToArray();
    }

    /// <summary>
    /// Interface Texture Slots Without "out"
    /// </summary>
    /// <returns>Specified Texture Slot of the Material</returns>
    public static TextureSlot GetTextureSlot(this Material material, TextureType textureType, int index)
    {
        material.GetMaterialTexture(textureType, index, out TextureSlot slot);
        return slot;
    }


    public static Model[] Transform(this Model[] models, Matrix4 transform)
    {
        foreach (var m in models) m.Transform(transform);
        return models;
    }


    public static Model[] GetModels(this Objects.Mesh[] meshes) => meshes.Select(m => new Model(m)).ToArray();

    public static Model[] EnableTranspose(this Model[] models)
    {
        foreach (var m in models) m.EnableTranspose();
        return models;
    }
    
    public static Model[] DisableTranspose(this Model[] models)
    {
        foreach (var m in models) m.DisableTranspose();
        return models;
    }
    
    
    
    
    public static Objects.Mesh Flatten(this Objects.Mesh[] meshes)
    {
        List<int> indices = new List<int>();
        int offset = 0;
        foreach (var mesh in meshes)
        {
            if (mesh.Indices == null) continue;
            indices.AddRange(mesh.Indices.Select(ind => ind + offset));
            offset += mesh.Vertices.Length / 3;
        }
        
        
        return new Objects.Mesh(
            meshes.SelectMany(m => m.Vertices ?? Array.Empty<float>()).ToArray(),
            indices.ToArray(),
            meshes.SelectMany(m => m.TexCoords ?? Array.Empty<float>()).ToArray(),
            meshes.SelectMany(m => m.Normals ?? Array.Empty<float>()).ToArray(),
            meshes.SelectMany(m => m.Tangents ?? Array.Empty<float>()).ToArray()
        );
    }
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