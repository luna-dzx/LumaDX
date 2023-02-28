using System.Numerics;
using Assimp;
using OpenTK.Mathematics;
using Steps = Assimp.PostProcessSteps;
using Triangle = LumaDX.Maths.Triangle;
using Vector3 = OpenTK.Mathematics.Vector3;
using Vector4 = OpenTK.Mathematics.Vector4;

namespace LumaDX;

public class FileManager
{
    private FileInfo fileInfo;
    private PostProcessSteps postProcessFlags = PostProcessSteps.Triangulate | PostProcessSteps.GenerateNormals;
    private bool initialized = false;
    private Scene scene;
    private Objects.Mesh[]? meshes = null;

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
    

    public FileManager(string path)
    { 
        fileInfo = new FileInfo(path);
    }

    public FileManager SetFlags(PostProcessSteps postProcessSteps)
    {
        postProcessFlags = postProcessSteps;
        return this;
    }

    public FileManager AddFlag(PostProcessSteps postProcessSteps)
    {
        postProcessFlags = postProcessFlags | postProcessSteps;
        return this;
    }
    
    
    
    /// <summary>
    /// Translates Assimp Meshes to LumaDX Meshes
    /// </summary>
    /// <returns>Array of Meshes in File</returns>
    public Objects.Mesh[] LoadMeshes()
    {
        int meshCount = Scene.Meshes.Count;
        meshes = new Objects.Mesh[meshCount];
        Mesh mesh;

        for (int m = 0; m < meshCount; m++)
        {
            mesh = Scene.Meshes[m];

            meshes[m] = new Objects.Mesh(
                vertices: mesh.Vertices.Count > 0 ? mesh.Vertices.SelectMany(v => new [] { v.X, v.Y, v.Z }).ToArray() : null,
                indices: mesh.HasFaces ? mesh.Faces.SelectMany(f => f.Indices).ToArray() : null,
                texCoords: mesh.HasTextureCoords(0) ? mesh.TextureCoordinateChannels[0].SelectMany(c => new [] { c.X, c.Y }).ToArray() : null,
                normals: mesh.Normals.Count > 0 ? mesh.Normals.SelectMany(n => new [] { n.X, n.Y, n.Z }).ToArray() : null,
                tangents: mesh.Tangents.Count > 0 ? mesh.Tangents.SelectMany(t => new [] { t.X, t.Y, t.Z }).ToArray() : null
            );
        }
        
        return meshes;
    }


    /// <summary>
    /// Translates Assimp Meshes to One Single LumaDX Mesh
    /// </summary>
    /// <returns>Single Mesh of All Data</returns>
    public Objects.Mesh LoadMesh() => LoadMeshes().Flatten();
    
    /// <summary>
    /// Translates One Assimp Mesh to One LumaDX Mesh
    /// </summary>
    /// <returns>Single LumaDX Mesh</returns>
    public Objects.Mesh LoadMesh(int i) => LoadMeshes()[i];
    
    /// <summary>
    /// Translates Assimp Meshes to One Single LumaDX Model
    /// </summary>
    /// <returns>Single Model of All Data</returns>
    public Model LoadModel() => new Model(LoadMeshes().Flatten());
    
    /// <summary>
    /// Translates One Assimp Mesh to One LumaDX Model
    /// </summary>
    /// <returns>Single LumaDX Model</returns>
    public Model LoadModel(int i) => new Model(LoadMeshes()[i]);
    
    
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

    public Material[] LoadMaterials() => Scene.Meshes.Select(m => Scene.Materials[m.MaterialIndex]).ToArray();

    public Texture[] LoadTextures(TextureType type, int textureUnit, int index = 0)
        => LoadMaterials().Select(m => new Texture(fileInfo.DirectoryName+"/"+m.GetTextureSlot(type, index).FilePath, textureUnit)).ToArray();
    
    
    
    
    
}