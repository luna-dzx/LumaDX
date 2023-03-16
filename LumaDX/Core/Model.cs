using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using PrimitiveType = OpenTK.Graphics.OpenGL4.PrimitiveType;

namespace LumaDX;

/// <summary>
/// More abstracted way of handling VAOs - dependent on the VAO class.
/// This class is more geared towards very simple VAO usage, and is a
/// faster way of creating the "general case" of simply loading a mesh
/// </summary>
public class Model : VertexArray
{
    private Objects.Mesh mesh;

    //private int uTransform;
    private Matrix4 transform = Matrix4.Identity;

    public float[]? GetVertices => mesh.Vertices;
    public float[]? GetTexCoords => mesh.TexCoords;
    public float[]? GetNormals => mesh.Normals;
    public int[]? GetIndices => mesh.Indices;

    public Matrix4 GetTransform() => transform;

    private bool transposeMatrix = false;

    /// <summary>
    /// Create a new model, which contains an empty mesh object and a transformation matrix
    /// </summary>
    public Model()
    {
        mesh = new Objects.Mesh();
    }

    /// <summary>
    /// Create a new model, then load vertex data to the vao and mesh object
    /// </summary>
    /// <param name="vertices">vertex data to load</param>
    /// <param name="vertexBinding">glsl vertex binding to load the vertex data to in the vertex shader</param>

    public Model(float[] vertices, int vertexBinding = 0)
        :this()
    {
        LoadVertices(vertexBinding,vertices);
    }
        
    /// <summary>
    /// Create a new model, then load vertex data to the vao and mesh object, as well as lading index data for rendering
    /// </summary>
    /// <param name="vertices">vertex data to load</param>
    /// <param name="indices">index data for rendering</param>
    /// <param name="vertexBinding">glsl vertex binding to load the vertex data to in the vertex shader</param>
    public Model(float[] vertices, int[] indices, int vertexBinding = 0)
        :this(vertices,vertexBinding)
    {
        LoadIndices(indices);
    }

    /// <summary>
    /// Create a model from a pre-existing mesh
    /// </summary>
    /// <param name="meshData">Mesh to load (loads all data to VAO)</param>
    public Model(Objects.Mesh meshData)
        :this()
    {
        LoadMesh(meshData);
    }
    
    /// <summary>
    /// Enable transposing the Model matrix when loading to the gpu
    /// </summary>
    public Model EnableTranspose()
    {
        transposeMatrix = true;
        return this;
    }
    
    /// <summary>
    /// Disable transposing the Model matrix when loading to the gpu
    /// </summary>
    public Model DisableTranspose()
    {
        transposeMatrix = false;
        return this;
    }
    
    /// <summary>
    /// Loads all mesh data to the VAO
    /// </summary>
    /// <param name="meshData">the mesh to load from</param>
    /// <returns>current object for ease of use</returns>
    public Model LoadMesh(Objects.Mesh meshData)
    {
        if (meshData.Vertices != null) LoadData(meshData.VertexBinding, meshData.Vertices);
        if (meshData.TexCoords != null) LoadData(meshData.TexCoordBinding, meshData.TexCoords, BufferTarget.ArrayBuffer, -1, 2, 2);
        if (meshData.Normals != null) LoadData(meshData.NormalBinding, meshData.Normals);
        if (meshData.Tangents != null) LoadData(meshData.TangentBinding, meshData.Tangents);
        if (meshData.Indices != null) CreateBuffer(meshData.Indices, BufferTarget.ElementArrayBuffer);

        mesh = meshData;
        return this;
    }

    /// <summary>
    /// Load vertex data to the VAO
    /// </summary>
    /// <param name="layoutLocation">the glsl binding that the data will be sent to</param>
    /// <param name="vertices">vertex data to load</param>
    /// <returns>current object for ease of use</returns>
    public Model LoadVertices(int layoutLocation,float[] vertices)
    {
        mesh.Vertices = vertices;
        LoadData(layoutLocation, mesh.Vertices);
        return this;
    }

    /// <summary>
    /// Loads indices to the VAO for rendering vertex data
    /// </summary>
    /// <param name="indices">index data to load</param>
    /// <returns>current object for ease of use</returns>
    public Model LoadIndices(int[] indices)
    {
        mesh.Indices = indices; 
        CreateBuffer(mesh.Indices, BufferTarget.ElementArrayBuffer);
        return this;
    }

    /// <summary>
    /// Loads texture coordinates to the VAO
    /// </summary>
    /// <param name="layoutLocation">the glsl binding that the data will be sent to</param>
    /// <param name="texCoords">the texture coordinates to load</param>
    /// <returns>current object for ease of use</returns>
    public Model LoadTexCoords(int layoutLocation, float[] texCoords)
    {
        mesh.TexCoords = texCoords;
        LoadData(layoutLocation, mesh.TexCoords, BufferTarget.ArrayBuffer, 2, 2);
        return this;
    }

    /// <summary>
    /// Loads per vertex normal data to the VAO
    /// </summary>
    /// <param name="layoutLocation">the glsl binding that the data will be sent to</param>
    /// <param name="normals">normals data to load</param>
    /// <returns>current object for ease of use</returns>
    public Model LoadNormals(int layoutLocation, float[] normals)
    {
        mesh.Normals = normals;
        LoadData(layoutLocation, normals);
        return this;
    }

    /// <summary>
    /// Set a new transformation matrix based on parameters
    /// </summary>
    public Model Transform(Vector3 translation, Vector3 rotation, Vector3 scale)
    {
        transform = Maths.CreateTransformation(translation, rotation, scale);
        return this;
    }

    /// <summary>
    /// Replace transformation matrix with supplied matrix
    /// </summary>
    public Model Transform(Matrix4 matrix)
    {
        transform = matrix;
        return this;
    }
    
    /// <summary>
    /// Replace transformation matrix with supplied matrix and load to GPU
    /// </summary>
    public Model LoadTransformation(ShaderProgram shader, Matrix4 matrix)
    {
        Transform(matrix);
        LoadTransformation(shader);
        return this;
    }
    
    /// <summary>
    /// Set a new transformation matrix based on parameters and load to GPU
    /// </summary>
    public Model LoadTransformation(ShaderProgram shader, Vector3 translation, Vector3 rotation, Vector3 scale)
    {
        Transform(translation, rotation, scale);
        LoadTransformation(shader);
        return this;
    }
    
    /// <summary>
    /// Load current transformation matrix to the GPU
    /// </summary>
    public Model LoadTransformation(ShaderProgram shader)
    {
        shader.Use();
        GL.UniformMatrix4(shader.DefaultModel, transposeMatrix, ref transform);
        return this;
    }

    /// <summary>
    /// Draw the object based on the current configuration
    /// </summary>
    /// <exception cref="Exception"></exception>
    public void Draw(int instanceCount = 1, PrimitiveType renderMode = PrimitiveType.Triangles)
    {
        Use();

        if (mesh.Indices != null && mesh.Indices.Length != 0)
        {
            if (instanceCount > 1)
            {
                GL.DrawElementsInstanced(renderMode,mesh.Indices.Length,DrawElementsType.UnsignedInt,IntPtr.Zero,instanceCount); return;
            }

            GL.DrawElements(renderMode,mesh.Indices.Length,DrawElementsType.UnsignedInt,IntPtr.Zero); return;
            
            
        }
        if (mesh.Vertices != null && mesh.Vertices.Length != 0)
        {

            if (instanceCount > 1)
            {
                GL.DrawArraysInstanced(renderMode,0,mesh.Vertices.Length/3,instanceCount); return;
            }
            
            GL.DrawArrays(renderMode,0,mesh.Vertices.Length/3); return;
        }

        throw new Exception("Invalid Mesh");


    }
    
    
    /// <summary>
    /// Draw with a new Model Matrix made from the parameters
    /// </summary>
    public void Draw(ShaderProgram program, Vector3 position, Vector3 rotation, float scale, int instanceCount = 1, PrimitiveType renderMode = PrimitiveType.Triangles)
    {
        LoadTransformation(program, position, rotation, Vector3.One * scale);
        Draw(instanceCount, renderMode);
    }
    
    /// <summary>
    /// Draw after loading the transformation matrix to the GPU
    /// </summary>
    public void Draw(ShaderProgram program, int instanceCount = 1, PrimitiveType renderMode = PrimitiveType.Triangles)
    {
        LoadTransformation(program);
        Draw(instanceCount, renderMode);
    }

    /// <summary>
    /// Draw a WireFrame shell around an already rendered object
    /// </summary>
    public void DrawShell(StateHandler glState)
    {
        glState.SaveState();
        glState.DepthFunc = DepthFunction.Always;
        GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
        Draw();
        GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
        glState.LoadState();
    }

}