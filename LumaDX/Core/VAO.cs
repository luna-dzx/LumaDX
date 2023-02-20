using System;
using OpenTK.Graphics.OpenGL4;
using ErrorCode = OpenTK.Graphics.OpenGL4.ErrorCode;

namespace LumaDX;

/// <summary>
/// Simplifying VAOs and VBOs (for managing large amounts of data on the GPU)
/// </summary>
public class VertexArray : IDisposable
{
    private readonly int handle;
    private BufferUsageHint _bufferUsageHint;

    /// <summary>
    /// Self-handled VAO for multiple or dynamic VBOs
    /// </summary>
    /// <param name="usage">how frequently this data is to be used</param>
    public VertexArray(BufferUsageHint usage = BufferUsageHint.StaticDraw)
    {
        _bufferUsageHint = usage;
        handle = GL.GenVertexArray();
        this.Use();
    }

    /// <summary>
    /// Setup a VAO for static loading of standard vertices
    /// </summary>
    /// <param name="layoutLocation">shader layout location of vertex input</param>
    /// <param name="vertices">array of vertices to load</param>
    /// <param name="usage">how frequently this data is to be used</param>
    public VertexArray(int layoutLocation, float[] vertices, BufferUsageHint usage = BufferUsageHint.StaticDraw) : this(usage)
    {
        LoadData(layoutLocation, vertices);
    }

    /// <summary>
    /// Setup a VAO for loading standard elements (vertices + indices)
    /// </summary>
    /// <param name="layoutLocation">shader layout location of vertex input</param>
    /// <param name="vertices">array of vertices to load</param>
    /// <param name="indices">array of indices connecting the vertices as triangles</param>
    /// <param name="usage">how frequently this data is to be used</param>
    public VertexArray(int layoutLocation, float[] vertices, int[] indices, BufferUsageHint usage = BufferUsageHint.StaticDraw) : this(layoutLocation,vertices, usage)
    {
        CreateBuffer(indices, BufferTarget.ElementArrayBuffer);
    }

    /// <summary>
    /// Creates a buffer for loading data to GPU Memory
    /// </summary>
    /// <param name="data">the data to send to the GPU</param>
    /// <param name="target">the type of data we are sending</param>
    /// <param name="buffer">what VBO to load this data to (-1 means create new buffer)</param>
    /// <typeparam name="T">general type for loading up any variable type</typeparam>
    /// <returns>the VBO id that this data was loaded to</returns>
    public int CreateBuffer<T>(T[] data,BufferTarget target, int buffer = -1) where T : struct
    {
        this.Use();
        
        if (buffer == -1) { buffer = GL.GenBuffer(); }
        
        // bind buffer for storing data
        GL.BindBuffer(target,buffer);

        // copy vertex data to buffer memory
        GL.BufferData(target,data.Length*OpenGL.GetSizeInBytes(data[0].GetType()),data,_bufferUsageHint);

        return buffer;
    }

    /// <summary>
    /// Creates an empty buffer for loading data to GPU Memory
    /// </summary>
    /// <param name="size">the size of the buffer in bytes</param>
    /// <param name="target">the type of data we are sending</param>
    /// <param name="buffer">what VBO to load this data to (-1 means create new buffer)</param>
    /// <returns>the VBO id where we allocated this memory</returns>
    public int EmptyBuffer(int size, BufferTarget target, int buffer = -1)
    {
        this.Use();
        
        if (buffer == -1) { buffer = GL.GenBuffer(); }
        // bind buffer for storing data
        GL.BindBuffer(target,buffer);
        
        // allocate space for the buffer in memory
        GL.BufferData(target,size,IntPtr.Zero,_bufferUsageHint);
        
        return buffer;
    }

    /// <summary>
    /// To resize an index buffer you need to bind another vertex buffer from the VAO
    /// </summary>
    /// <param name="size">New size for the buffer</param>
    /// <param name="vertexBuffer">A vertex buffer on this VAO</param>
    /// <param name="target">The target of the vertex buffer</param>
    public void ResizeIndexBuffer(int size, int vertexBuffer, BufferTarget target = BufferTarget.ArrayBuffer)
    {
        GL.BindBuffer(target, vertexBuffer);
        this.Use();
        GL.BufferData(BufferTarget.ElementArrayBuffer, size, IntPtr.Zero, BufferUsageHint.DynamicDraw);
    }
    
    

    /// <summary>
    /// Creates a buffer on the GPU and then sets up that buffer for data storage
    /// </summary>
    /// <param name="layoutLocation">shader layout location of data input</param>
    /// <param name="data">the data to send to the GPU</param>
    /// <param name="target">the type of data we are sending</param>
    /// <param name="buffer">what VBO to load this data to (-1 means create new buffer)</param>
    /// <param name="dataLength">number of variables per one group of data</param>
    /// <param name="stride">number of variable between each group of data</param>
    /// <param name="offset">number of variables to offset the start of the reading from</param>
    /// <param name="normalized">sets all data to length 1</param>
    /// <typeparam name="T">general type for loading up any variable type</typeparam>
    /// <returns>the VBO id that this data was loaded to</returns>
    /// <exception cref="Exception">length of data must be > 0</exception>
    public int LoadData<T>(int layoutLocation, T[] data, BufferTarget target = BufferTarget.ArrayBuffer, int buffer = -1, int dataLength=3, int stride = 3, int offset=0, bool normalized = false) where T : struct
    {
        if (data.Length < 1) throw new Exception("Invalid Input Data (data length must be > 0)");
        if (buffer == -1) { buffer = GL.GenBuffer(); }
        
        CreateBuffer(data,target,buffer);
        SetupBuffer(layoutLocation,data[0].GetType(),dataLength,stride,offset,normalized);

        return buffer;
    }

    /// <summary>
    /// Load a matrix into GPU memory as its individual vector components, NOTE: this takes up *matrixWidth* number of layout locations
    /// </summary>
    /// <param name="layoutLocation">shader layout location of data input</param>
    /// <param name="data">the data to send to the GPU</param>
    /// <param name="matrixWidth">how many columns are in the matrix you are loading</param>
    /// <param name="matrixHeight">how many rows are in the matrix you are loading</param>
    /// <param name="countPerInstance">how many matrices should be loaded per instance (-1 for all)</param>
    /// <param name="buffer">what VBO to load this data to (-1 means create new buffer)</param>
    /// <param name="stride">number of variable between each group of data</param>
    /// <param name="offset">number of variables to offset the start of the reading from</param>
    /// <param name="normalized">sets all data to length 1</param>
    /// <typeparam name="T">general type for loading up any variable type</typeparam>
    /// <returns>the VBO id that this data was loaded to</returns>
    public int LoadMatrix<T>(int layoutLocation, T[] data, int matrixWidth, int matrixHeight, int countPerInstance = -1, int buffer = -1, int stride=0, int offset=0, bool normalized = false) where T : struct
    {
        if (buffer == -1) { buffer = GL.GenBuffer(); }
        CreateBuffer(data, BufferTarget.ArrayBuffer, buffer);

        for (int vectorIndex = 0; vectorIndex < matrixWidth; vectorIndex++)
        {
            SetupBuffer(layoutLocation+vectorIndex, typeof(float), matrixHeight, matrixWidth*matrixHeight+stride, vectorIndex*matrixHeight+offset);
            if (countPerInstance != -1) GL.VertexAttribDivisor(layoutLocation+vectorIndex,countPerInstance);
        }

        return buffer;
    }
    
    // TODO: Comments Here
    public int LoadVector<T>(int layoutLocation, T[] data, int vectorSize, int countPerInstance = -1, int buffer = -1, int stride=0, int offset=0) where T : struct
    {
        if (buffer == -1) { buffer = GL.GenBuffer(); }
        CreateBuffer(data, BufferTarget.ArrayBuffer, buffer);
        
        SetupBuffer(layoutLocation, typeof(float), vectorSize, vectorSize*stride, vectorSize*offset);
        if (countPerInstance != -1) GL.VertexAttribDivisor(layoutLocation,countPerInstance);

        return buffer;
    }


    /// <summary>
    /// Sets up a buffer for data storage on GPU memory
    /// </summary>
    /// <param name="layoutLocation">shader layout location of data input</param>
    /// <param name="type">the type of data that is to be loaded to this buffer</param>
    /// <param name="dataLength">number of variables per one group of data</param>
    /// <param name="stride">number of variables between each group of data</param>
    /// <param name="offset">number of variables to offset the start of the reading from</param>
    /// <param name="normalized">sets all data to length 1</param>
    public void SetupBuffer(int layoutLocation, Type type, int dataLength=3, int stride = 3, int offset=0, bool normalized = false)
    {
        this.Use();
        
        int dataSizeInBytes = OpenGL.GetSizeInBytes(type);
        
        GL.VertexAttribPointer(
            layoutLocation, // shader layout location
            dataLength, // size (num values)
            OpenGL.GetAttribPointerType(type), // variable type
            normalized, // normalize data (set to "length 1")
            stride*dataSizeInBytes, // space in bytes between each vertex attrib
            offset*dataSizeInBytes // data offset
        );

        GL.EnableVertexAttribArray(layoutLocation);
    }
    
    public void SetupBuffer(int layoutLocation, VertexAttribPointerType type, int dataLength=3, int stride = 3, int offset=0, bool normalized = false)
    {
        this.Use();

        GL.VertexAttribPointer(
            layoutLocation, // shader layout location
            dataLength, // size (num values)
            type, // variable type
            normalized, // normalize data (set to "length 1")
            stride, // space in bytes between each vertex attrib
            offset // data offset
        );

        GL.EnableVertexAttribArray(layoutLocation);
    }
    


    /// <summary>
    /// Activate this VAO for reading/writing
    /// </summary>
    public void Use()
    {
        GL.BindVertexArray(handle);
    }


    /// <summary>
    /// Remove VAO from video memory
    /// </summary>
    public void Dispose()
    {
        GL.DeleteVertexArray(handle);
        ErrorCode error = GL.GetError();
        if (error != ErrorCode.NoError) throw new Exception(error.ToString());
    }

    /// <summary>
    /// Get the VAOs handle
    /// </summary>
    /// <returns>the OpenGL VAO handle</returns>
    public int GetHandle() => handle;
    

}