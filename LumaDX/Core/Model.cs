using Assimp;
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

    public Model SetMesh(Objects.Mesh newMesh)
    {
        mesh = newMesh;
        return this;
    }

    //private int uTransform;
    private Matrix4 transform = Matrix4.Identity;

    public float[]? GetVertices => mesh.Vertices;
    public float[]? GetTexCoords => mesh.TexCoords;
    public float[]? GetNormals => mesh.Normals;
    public int[]? GetIndices => mesh.Indices;

    public Matrix4 GetTransform() => transform;


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


    
    public static Model FromFile(
        string directory,
        string fileName,
        out Dictionary<TextureType,List<Texture>> textures,
        TextureType[] filters = null,
        PostProcessSteps postProcessFlags = PostProcessSteps.Triangulate | PostProcessSteps.FlipUVs
        )
    {
        Scene scene; //"Assets/backpack/backpack.obj"
        AssimpContext importer = new AssimpContext();
        //importer.SetConfig(new Assimp.Configs.NormalSmoothingAngleConfig(66.0f));
        //Console.WriteLine("Loading Model...");
        scene = importer.ImportFile(directory+fileName, postProcessFlags);//,PostProcessPreset.TargetRealTimeMaximumQuality);
        //Console.WriteLine("Model Loaded");
        
        int vertexCount = scene.Meshes.Sum(mesh => mesh.VertexCount);
        int indexCount = scene.Meshes.Sum(mesh => mesh.Faces.Sum(face => face.IndexCount));


        textures = new();

        if (filters != null)
        {
            // load all textures of each type from all materials (yeah idk why the structure has to be this convoluted it's annoying)
            int offset = 0;
            foreach (TextureType type in Enum.GetValues(typeof(TextureType)))
            {
                // filter out certain texture types because images take a while to load
                if (!filters.Contains(type)) continue;

                foreach (var material in scene.Materials)
                {
                    List<Texture> currentTextures = new List<Texture>();
        
                    for (int j = 0; j < material.GetMaterialTextureCount(type); j++)
                    {
                        material.GetMaterialTexture(type, j, out var slot);
                        string path = directory + slot.FilePath;
                        var texture = new Texture(path, (offset + slot.TextureIndex));
                        currentTextures.Add(texture);
                        Console.WriteLine(type);
                    }

                    offset += currentTextures.Count;
                    if (!textures.ContainsKey(type))
                    {
                        textures.Add(type,currentTextures);
                    }
                    else
                    {
                        textures[type] = currentTextures;
                    }
                }

            }
        }

        var vertices = new float[vertexCount * 3];
        var normals = new float[vertexCount * 3];
        var tangents = new float[vertexCount * 3];
        var texCoords = new float[vertexCount * 2];
        var indices = new int[indexCount];
        
        int indexOffset = 0;
        int vertexIndex = 0;

        foreach (Mesh mesh in scene.Meshes)
        {
            for (int i = 0; i < mesh.VertexCount; i++)
            {
                vertices[vertexIndex*3] = mesh.Vertices[i].X;
                vertices[vertexIndex*3 +1] = mesh.Vertices[i].Y;
                vertices[vertexIndex*3 +2] = mesh.Vertices[i].Z;
                
                normals[vertexIndex*3] = mesh.Normals[i].X;
                normals[vertexIndex*3 +1] = mesh.Normals[i].Y;
                normals[vertexIndex*3 +2] = mesh.Normals[i].Z;

                if (mesh.Tangents.Count > 0)
                {
                    tangents[vertexIndex*3] = mesh.Tangents[i].X;
                    tangents[vertexIndex*3 +1] = mesh.Tangents[i].Y;
                    tangents[vertexIndex*3 +2] = mesh.Tangents[i].Z;
                }

                
                texCoords[vertexIndex*2] = mesh.TextureCoordinateChannels[0][i].X;
                texCoords[vertexIndex*2 +1] = mesh.TextureCoordinateChannels[0][i].Y;

                vertexIndex++;
            }

            int numIndices = 0;
            foreach (Face face in mesh.Faces)
            {
                foreach (int index in face.Indices)
                {
                    indices[indexOffset+numIndices] = indexOffset+index;
                    numIndices++;
                }
            }

            indexOffset += numIndices;
        }
        
        
        var finalMesh = new Objects.Mesh(
            vertices: vertices,
            indices: indices,
            texCoords: texCoords,
            normals: normals,
            tangents: tangents
        );

        return new Model(finalMesh);

    }
    
    
    // THIS IS TEMPORARY, FIX PLS
    
    
    public static (Model[],Texture[]) FromFile(
        string directory,
        string fileName,
        PostProcessSteps postProcessFlags = PostProcessSteps.Triangulate | PostProcessSteps.FlipUVs
        )
    {
        Scene scene; //"../../../../../../0 Assets/backpack/backpack.obj"
        AssimpContext importer = new AssimpContext();

        scene = importer.ImportFile(directory+fileName, postProcessFlags);//,PostProcessPreset.TargetRealTimeMaximumQuality);
        


        Model[] models = new Model[scene.Meshes.Count];
        Texture[] textures = new Texture[scene.Meshes.Count];
        int meshCount = 0;

        foreach (Mesh mesh in scene.Meshes)
        {
            int vertexCount = mesh.VertexCount;
            int indexCount = mesh.Faces.Sum(face => face.IndexCount);
            
            
            textures[meshCount] = new Texture(directory+scene.Materials[mesh.MaterialIndex].TextureDiffuse.FilePath, 0);
            
            
            var vertices = new float[vertexCount * 3];
            var normals = new float[vertexCount * 3];
            var tangents = new float[vertexCount * 3];
            var texCoords = new float[vertexCount * 2];
            var indices = new int[indexCount];
        
            int indexOffset = 0;
            int vertexIndex = 0;
            
            
            
            for (int i = 0; i < mesh.VertexCount; i++)
            {
                vertices[vertexIndex*3] = mesh.Vertices[i].X / 100f;
                vertices[vertexIndex*3 +1] = mesh.Vertices[i].Y / 100f;
                vertices[vertexIndex*3 +2] = mesh.Vertices[i].Z / 100f;


                normals[vertexIndex*3] = mesh.Normals[i].X;
                normals[vertexIndex*3 +1] = mesh.Normals[i].Y;
                normals[vertexIndex*3 +2] = mesh.Normals[i].Z;



                if (mesh.Tangents.Count > 0)
                {
                    tangents[vertexIndex*3] = mesh.Tangents[i].X;
                    tangents[vertexIndex*3 +1] = mesh.Tangents[i].Y;
                    tangents[vertexIndex*3 +2] = mesh.Tangents[i].Z;
                }

                
                texCoords[vertexIndex*2] = mesh.TextureCoordinateChannels[0][i].X;
                texCoords[vertexIndex*2 +1] = mesh.TextureCoordinateChannels[0][i].Y;

                vertexIndex++;
            }

            int numIndices = 0;
            foreach (Face face in mesh.Faces)
            {
                foreach (int index in face.Indices)
                {
                    indices[indexOffset+numIndices] = indexOffset+index;
                    numIndices++;
                }
            }

            indexOffset += numIndices;
            
            
            var finalMesh = new Objects.Mesh(
                vertices: vertices,
                indices: indices,
                texCoords: texCoords,
                normals: normals,
                tangents: tangents
            );

            models[meshCount] = new Model(finalMesh);
            meshCount++;


        }
        
        


        return (models,textures);

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
    /// Load the transformation matrix onto the GPU
    /// </summary>
    public Model UpdateTransform(int programId, int binding)
    {
        GL.UseProgram(programId);
        GL.UniformMatrix4(binding,false,ref transform);
        return this;
    }
    public Model UpdateTransform(int programId, string name)
    {
        GL.UseProgram(programId);
        GL.UniformMatrix4(GL.GetUniformLocation(programId,name),false,ref transform);
        return this;
    }
    public Model UpdateTransform(ShaderProgram program)
    {
        program.Use();
        GL.UniformMatrix4(program.DefaultModel,false,ref transform);
        return this;
    }

    /// <summary>
    /// Set a new transformation matrix
    /// </summary>
    /// <param name="translation">position relative to the origin</param>
    /// <param name="rotation">rotation in the x,y and z axis</param>
    /// <param name="scale">scale in x,y and z</param>
    public Model Transform(Vector3 translation = default, Vector3 rotation = default, Vector3 scale = default)
    {
        transform = Maths.CreateTransformation(translation, rotation, scale);
        return this;
    }

    public Model UpdateTransform(ShaderProgram shader, Vector3 translation, Vector3 rotation = default, Vector3 scale = default)
    {
        if (scale == default) scale = Vector3.One;
        Transform(translation, rotation, scale);
        UpdateTransform(shader);
        return this;
    }
    public Model UpdateTransform(ShaderProgram shader, Vector3 translation, Vector3 rotation, float scale)
    {
        Transform(translation, rotation, scale);
        UpdateTransform(shader);
        return this;
    }

    /// <summary>
    /// Set a new transformation matrix
    /// </summary>
    /// <param name="translation">position relative to the origin</param>
    /// <param name="rotation">rotation in the x,y and z axis</param>
    /// <param name="scale">scale of the overall object in all 3 dimensions</param>
    public Model Transform(Vector3 translation = default, Vector3 rotation = default, float scale = 1f)
    {
        transform = Maths.CreateTransformation(translation, rotation, new Vector3(scale,scale,scale));
        return this;
    }



    /// <summary>
    /// Multiply the current object's scale by this value
    /// </summary>
    /// <param name="scale">scale in x,y and z</param>
    public Model Scale(Vector3 scale)
    {
        transform = Matrix4.CreateScale(scale) * transform;
        return this;
    }

    /// <summary>
    /// Multiply the current object's scale by this value
    /// </summary>
    /// <param name="scale">scale of the overall object in all 3 dimensions</param>
    public Model Scale(float scale)
    {
        transform = Matrix4.CreateScale(scale,scale,scale) * transform;
        return this;
    }


    /// <summary>
    /// Sets the transform to default (at origin, no rotation, scale 1)
    /// </summary>
    public Model ResetTransform()
    {
        transform = Matrix4.Identity;
        return this;
    }

    public Model SetMatrix(ShaderProgram shader, Matrix4 matrix)
    {
        transform = matrix;
        UpdateTransform(shader);
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
        UpdateTransform(program, position, rotation, scale);
        Draw(instanceCount, renderMode);
    }
    
    /// <summary>
    /// Draw after loading the transformation matrix to the GPU
    /// </summary>
    public void Draw(ShaderProgram program, int instanceCount = 1, PrimitiveType renderMode = PrimitiveType.Triangles)
    {
        UpdateTransform(program);
        Draw(instanceCount, renderMode);
    }

}