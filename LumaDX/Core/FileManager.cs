using System.Numerics;
using Assimp;
using OpenTK.Mathematics;
using Steps = Assimp.PostProcessSteps;
using Triangle = LumaDX.Maths.Triangle;
using Vector3 = OpenTK.Mathematics.Vector3;
using Vector4 = OpenTK.Mathematics.Vector4;

namespace LumaDX;

/// <summary>
/// Struct storing useful info about a 3D model
/// </summary>
public struct ModelInfo
{
    public string FileName;
    public int MeshCount;
    
    public string[] MeshNames;
    public int[] VertexCounts;
    public int[] FaceCounts;
    public Bounds[] MeshBounds;

    public bool[] HasNormals;
    public bool[] HasTangents;
    public bool[] HasTexCoords;
    public bool[] HasColours;


    /// <summary>
    /// Output info as table
    /// </summary>
    public override string ToString()
    {
        var modelInfo = this; // copy to make this visible to local functions
        
        int maxPadding = 0;
        for (int i = 0; i < modelInfo.MeshCount; i++)
        {
            int padding = (i + " " +modelInfo.MeshNames[i]).Length;
            padding = Math.Max(padding, (""+modelInfo.VertexCounts[i]).Length);
            padding = Math.Max(padding, (""+modelInfo.FaceCounts[i]).Length);
            padding = Math.Max(padding, (""+(int)modelInfo.MeshBounds[i].Diameter).Length + 3);

            maxPadding = Math.Max(maxPadding, 2 + padding);
        }

        string separator = new string('-', maxPadding) + "|";
        string separators = "";
        for (int i = 0; i < modelInfo.MeshCount; i++) separators += separator;
        

        string PrintCell(string output)
        {
            int padding = maxPadding - output.Length;
            int left = padding / 2;
            int right = padding - left;

            return new string(' ', left) + output + new string(' ', right) + "|";
        }

        string PrintNames()
        {
            string s = ""; for (int i = 0; i < modelInfo.MeshCount; i++) s+=PrintCell(modelInfo.MeshNames[i] + " " + i);
            return s;
        }

        string PrintIntArray(int[] array)
        {
            string s = ""; for (int i = 0; i < modelInfo.MeshCount; i++) s+=PrintCell(""+array[i]);
            return s;
        }

        string PrintFloatArray(float[] array)
        {
            string s = ""; for (int i = 0; i < modelInfo.MeshCount; i++) s += PrintCell(array[i].ToString("0.000"));
            return s;
        }

        string PrintBoolArray(bool[] array)
        {
            string s = ""; for (int i = 0; i < modelInfo.MeshCount; i++) s+=PrintCell(array[i]?"✓":"☓");
            return s;
        }


        string output = "";
        output += "File Name: " + FileName + "\n";
        output += " ------------ |" + separators + "\n";
        output += " Mesh Name    |" + PrintNames() + "\n";
        output += " ------------ |" + separators + "\n";
        output += " Num. Verts   |" + PrintIntArray(modelInfo.VertexCounts) + "\n";
        output += " Num. Faces   |" + PrintIntArray(modelInfo.FaceCounts) + "\n";
        output += " Mesh Size    |" + PrintFloatArray(modelInfo.MeshBounds.Select(a => a.Diameter).ToArray()) + "\n";
        output += " ------------ |" + separators + "\n";
        output += " Normals?     |" + PrintBoolArray(modelInfo.HasNormals) + "\n";
        output += " Tangents?    |" + PrintBoolArray(modelInfo.HasTangents) + "\n";
        output += " Tex. Coords? |" + PrintBoolArray(modelInfo.HasTexCoords) + "\n";
        output += " Colours?     |" + PrintBoolArray(modelInfo.HasColours) + "\n";
        output += " ------------ |" + separators + "\n";


        return output;
    }
}

/// <summary>
/// BoundingBox with OpenTK Vector3s instead of Assimp Vector3Ds and Cached Diameter
/// </summary>
public struct Bounds
{
    public Vector3 Max;
    public Vector3 Min;
    public float Diameter;

    public Bounds(Vector3 min, Vector3 max)
    {
        Max = max;
        Min = min;
        Diameter = (Min - Max).Length;
    }

    public Bounds(BoundingBox box)
    {
        Max = new Vector3(box.Max.X,box.Max.Y,box.Max.Z);
        Min = new Vector3(box.Min.X,box.Min.Y,box.Min.Z);
        Diameter = (Min - Max).Length;
    }
}

/// <summary>
/// Handling 3D model loading
/// </summary>
public class FileManager
{
    private FileInfo fileInfo;
    private PostProcessSteps postProcessFlags = PostProcessSteps.Triangulate | PostProcessSteps.GenerateNormals;
    private bool initialized = false;
    private Scene scene;
    private Objects.Mesh[]? meshes = null;

    /// <summary>
    /// All data from 3D model file - load if not loaded yet
    /// </summary>
    private Scene Scene
    {
        get
        {
            if (initialized) return scene;
            
            AssimpContext importer = new AssimpContext();
            scene = importer.ImportFile(fileInfo.FullName, postProcessFlags);
            initialized = true;
            return scene;
        }
    }
    
    /// <summary>
    /// Initialize loader with path
    /// </summary>
    public FileManager(string path)
    { 
        fileInfo = new FileInfo(path);
    }

    /// <summary>
    /// Reset 3D model loading flags to argument provided
    /// </summary>
    public FileManager SetFlags(PostProcessSteps postProcessSteps)
    {
        postProcessFlags = postProcessSteps;
        return this;
    }

    /// <summary>
    /// Bitwise or with the current 3D model loading flags to add more
    /// </summary>
    public FileManager AddFlags(PostProcessSteps postProcessSteps)
    {
        postProcessFlags = postProcessFlags | postProcessSteps;
        return this;
    }

    /// <summary>
    /// Return useful information about the loaded 3D model
    /// </summary>
    public ModelInfo GetInfo()
    {
        // Assimp BoundingBox often not populated
        Bounds[] meshBounds = new Bounds[Scene.MeshCount];
        for (int i = 0; i < Scene.MeshCount; i++)
        {
            Vector3 max = Vector3.NegativeInfinity;
            Vector3 min = Vector3.PositiveInfinity;
            foreach (var vertex in Scene.Meshes[i].Vertices)
            {
                max.X = MathF.Max(max.X, vertex.X);
                max.Y = MathF.Max(max.Y, vertex.Y);
                max.Z = MathF.Max(max.Z, vertex.Z);
                
                min.X = MathF.Min(min.X, vertex.X);
                min.Y = MathF.Min(min.Y, vertex.Y);
                min.Z = MathF.Min(min.Z, vertex.Z);
            }

            meshBounds[i] = new Bounds(min, max);
        }
        
        return new ModelInfo()
        {
            FileName = fileInfo.Name,
            MeshCount = Scene.MeshCount,
            
            MeshNames = Scene.Meshes.Select(a => a.Name).ToArray(),
            VertexCounts = Scene.Meshes.Select(a => a.VertexCount).ToArray(),
            FaceCounts = Scene.Meshes.Select(a => a.FaceCount).ToArray(),
            MeshBounds = meshBounds,
            
            HasNormals = Scene.Meshes.Select(a => a.HasNormals).ToArray(),
            HasTangents = Scene.Meshes.Select(a => a.HasTangentBasis).ToArray(),
            HasTexCoords = Scene.Meshes.Select(a => a.HasTextureCoords(0)).ToArray(),
            HasColours = Scene.Meshes.Select(a => a.HasVertexColors(0)).ToArray(),
        };
    }
    
    
    /// <summary>
    /// Translates Assimp Meshes to LumaDX meshes
    /// </summary>
    /// <returns>Array of meshes in file</returns>
    public Objects.Mesh[] LoadMeshes(int id = -1)
    {
        int meshCount = Scene.Meshes.Count;
        int start = 0;
        var meshList = new List<Objects.Mesh>();

        Mesh mesh;

        if (id != -1) { start = id; meshCount = id + 1; }

        for (int m = start; m < meshCount; m++)
        {
            mesh = Scene.Meshes[m];

            meshList.Add( new Objects.Mesh(
                vertices: mesh.Vertices.Count > 0 ? mesh.Vertices.SelectMany(v => new [] { v.X, v.Y, v.Z }).ToArray() : null,
                indices: mesh.HasFaces ? mesh.Faces.SelectMany(f => f.Indices).ToArray() : null,
                texCoords: mesh.HasTextureCoords(0) ? mesh.TextureCoordinateChannels[0].SelectMany(c => new [] { c.X, c.Y }).ToArray() : null,
                normals: mesh.Normals.Count > 0 ? mesh.Normals.SelectMany(n => new [] { n.X, n.Y, n.Z }).ToArray() : null,
                tangents: mesh.Tangents.Count > 0 ? mesh.Tangents.SelectMany(t => new [] { t.X, t.Y, t.Z }).ToArray() : null
            ));
        }

        meshes = meshList.ToArray();
        return meshes;
    }


    /// <summary>
    /// Translates Assimp meshes to one Single LumaDX mesh
    /// </summary>
    /// <returns>Single mesh of all data</returns>
    public Objects.Mesh LoadMesh() => LoadMeshes().Flatten();
    
    /// <summary>
    /// Translates one Assimp mesh to one LumaDX mesh
    /// </summary>
    /// <returns>Single LumaDX mesh</returns>
    public Objects.Mesh LoadMesh(int i) => LoadMeshes(i)[0];
    
    /// <summary>
    /// Translates Assimp meshes to one single LumaDX model
    /// </summary>
    /// <returns>Single model of all data</returns>
    public Model LoadModel() => new Model(LoadMeshes().Flatten());
    
    /// <summary>
    /// Translates one Assimp mesh to one LumaDX model
    /// </summary>
    /// <returns>Single LumaDX model</returns>
    public Model LoadModel(int i) => new Model(LoadMesh(i));
    
    /// <summary>
    /// Load data from the previously loaded meshes to array of triangles for collisions
    /// </summary>
    public Triangle[] LoadTriangles(Matrix4 transform = default, Vector3 eRadius = default)
    {
        if (eRadius == default) eRadius = Vector3.One;
        if (transform == default) transform = Matrix4.Identity;
        
        var m = meshes.Flatten();
        Vector3[] vertices = m.Vertices.GetVertices();
        int[] indices = m.Indices;
        if (m.Indices != null && m.Indices.Length > 0) vertices = vertices.Decompress(indices);
        
        return vertices.Transform(transform).GetTriangles(eRadius);
    }

    /// <summary>
    /// Load all materials in the entire scene
    /// </summary>
    public Material[] LoadMaterials() => Scene.Meshes.Select(m => Scene.Materials[m.MaterialIndex]).ToArray();

    /// <summary>
    /// Load all textures of a certain type and unit
    /// </summary>
    public Texture[] LoadTextures(TextureType type, int textureUnit, int index = 0)
        => LoadMaterials().Select(m => new Texture(fileInfo.DirectoryName+"/"+m.GetTextureSlot(type, index).FilePath, textureUnit)).ToArray();
}