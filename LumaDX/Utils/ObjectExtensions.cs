using Assimp;
using OpenTK.Mathematics;

namespace LumaDX;

/// <summary>
/// Simple functions for data manipulation
/// </summary>
public static class ObjectExtensions
{
    /// <summary>
    /// Add a range of values from params
    /// </summary>
    public static void AddItems<T>(this List<T> list, params T[] values) => list.AddRange(values);
    
    /// <summary>
    /// Add the x,y,z components of a 3D vector to a float list
    /// </summary>
    public static void AddVec3(this List<float> list, Vector3 vector) => list.AddItems(vector.X,vector.Y,vector.Z);

    /// <summary>
    /// Get the 3D vertex at the supplied index of a float list
    /// </summary>
    public static Vector3 GetVertex(this List<float> vertices, int index) => new (vertices[index * 3], vertices[index * 3 + 1], vertices[index * 3 + 2]);
    
    /// <summary>
    /// Get the 3D vertex at the supplied index of a float array
    /// </summary>
    public static Vector3 GetVertex(this float[] vertices, int index) => new (vertices[index * 3], vertices[index * 3 + 1], vertices[index * 3 + 2]);

    /// <summary>
    /// Set the 3D vertex at the supplied index of a float array
    /// </summary>
    public static void SetVertex(this float[] vertices, int index, Vector3 vertex)
    {
        vertices[index * 3] = vertex.X;
        vertices[index * 3 + 1] = vertex.Y;
        vertices[index * 3 + 2] = vertex.Z;
    }

    /// <summary>
    /// Convert a float array to a Vector3 array
    /// </summary>
    public static Vector3[] GetVertices(this float[] floats)
    {
        int length = floats.Length / 3;
        Vector3[] vertices = new Vector3[length];
        for (int i = 0; i < length; i++) vertices[i] = floats.GetVertex(i);
        return vertices;
    }

    /// <summary>
    /// Get the Triangle at the supplied index of a Vector3 array
    /// </summary>
    public static Maths.Triangle GetTriangle(this Vector3[] vertices, int index,Vector3 eRadius = default)
    {
        if (eRadius == default) eRadius = Vector3.One;
        return new Maths.Triangle(vertices[index*3],vertices[index*3 +1],vertices[index*3 +2],eRadius);
    }
    
    /// <summary>
    /// Convert a Vector3 array to a Triangle array
    /// </summary>
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
    
    /// <summary>
    /// Convert vertices and indices to a single array of connected triangles
    /// </summary>
    public static Vector3[] Decompress(this Vector3[] vertices, int[] indices)
    {
        Vector3[] output = new Vector3[indices.Length];
        for (int i = 0; i < indices.Length; i++) output[i] = vertices[indices[i]];
        return output;
    }

    /// <summary>
    /// Transform a 3D vector by a 4D Matrix
    /// </summary>
    public static Vector3 Transform(this Vector3 vector, Matrix4 matrix)
    {
        return (matrix * new Vector4(vector, 1f)).Xyz;
    }
    
    /// <summary>
    /// Transform an array of 3D vectors by a 4D Matrix
    /// </summary>
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

    /// <summary>
    /// Transform array of models by single 4D matrix
    /// </summary>
    public static Model[] Transform(this Model[] models, Matrix4 transform)
    {
        foreach (var m in models) m.Transform(transform);
        return models;
    }


    /// <summary>
    /// Convert array of meshes to array of models
    /// </summary>
    public static Model[] GetModels(this Objects.Mesh[] meshes) => meshes.Select(m => new Model(m)).ToArray();

    /// <summary>
    /// Enable matrix transposing on every model in an array
    /// </summary>
    public static Model[] EnableTranspose(this Model[] models)
    {
        foreach (var m in models) m.EnableTranspose();
        return models;
    }
    
    /// <summary>
    /// Disable matrix transposing on every model in an array
    /// </summary>
    public static Model[] DisableTranspose(this Model[] models)
    {
        foreach (var m in models) m.DisableTranspose();
        return models;
    }
    
    /// <summary>
    /// Flatten the data of an array of meshes to a single mesh
    /// </summary>
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

/// <summary>
/// Dictionary comparison for int pairs independent of their order (e.g. (5,1) and (1,5) would give the same output)
/// </summary>
class OrderlessIntPairs : IEqualityComparer<(int, int)>
{
    /// <summary>
    /// Set (a,b) = (b,a)
    /// </summary>
    public bool Equals((int, int) x, (int, int) y)
    {
        return (x.Item1 == y.Item1 && x.Item2 == y.Item2) ||
               (x.Item1 == y.Item2 && x.Item2 == y.Item1);
    }

    /// <summary>
    /// Hash with XOR
    /// </summary>
    public int GetHashCode((int, int) obj)
    {
        return obj.Item1 ^ obj.Item2;
    }
}