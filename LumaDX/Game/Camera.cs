using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace LumaDX;

public class Camera
{
    // projection matrix vars
    private float aspect;
    private float fov;
    private float depthNear;
    private float depthFar;

    // camera vars
    public Vector3 Position;
    public Vector3 Direction;
    private readonly Vector3 up = Vector3.UnitY;

    // OpenGL
    //private int uProj;
    //private int uView;

    public Matrix4 ProjMatrix;
    public Matrix4 ViewMatrix;

    /// <summary>
    /// Create a new camera object for handling 3D projection
    /// </summary>
    /// <param name="aspectRatio">the screen's aspect ratio</param>
    /// <param name="fieldOfView">the camera's field of view</param>
    /// <param name="clipNear">the closest distance to render</param>
    /// <param name="clipFar">the furthest distance to render</param>
    public Camera(float aspectRatio, float fieldOfView = MathHelper.PiOver3,float clipNear = 0.1f, float clipFar = 100f)
    {
        aspect = aspectRatio;
        fov = fieldOfView;
        depthNear = clipNear;
        depthFar = clipFar;
    
        //uProj = projectionBinding;
        //uView = viewBinding;

        //UpdateProjection();
        //UpdateView();
    }

    /// <summary>
    /// Create a new camera object for handling 3D projection
    /// </summary>
    /// <param name="windowSize">the screen's size</param>
    /// <param name="fieldOfView">the camera's field of view in radians</param>
    /// <param name="clipNear">the closest distance to render</param>
    /// <param name="clipFar">the furthest distance to render</param>
    public Camera(Vector2i windowSize, float fieldOfView = MathHelper.PiOver3, float clipNear = 0.1f, float clipFar = 100f):this((float)windowSize.X/windowSize.Y,fieldOfView,clipNear,clipFar) { }
    

    /// <summary>
    /// Create a new perspective projection matrix and load to the uniform projection matrix binding
    /// </summary>
    public void UpdateProjection(int programId, int binding)
    {
        GL.UseProgram(programId);
        ProjMatrix = Matrix4.CreatePerspectiveFieldOfView(fov, aspect, depthNear, depthFar);
        GL.UniformMatrix4(binding,false,ref ProjMatrix);
    }

    /// <summary>
    /// Update matrices according to a new aspect ratio
    /// </summary>
    /// <param name="newAspect">the new aspect ratio of the screen</param>
    public void Resize(float newAspect)
    {
        aspect = newAspect;
    }

    /// <summary>
    /// Update matrices according to a new screen size
    /// </summary>
    /// <param name="newSize">the new size of the screen</param>
    public void Resize(Vector2i newSize)
    {
        Resize((float)newSize.X / newSize.Y);
    }

    /// <summary>
    /// Update matrices according to a new screen size, and load these matrices to the GPU
    /// </summary>
    /// <param name="shaderProgram">shader to load matrices to</param>
    /// <param name="newSize">the new size of the screen</param>
    public void Resize(ShaderProgram shaderProgram, Vector2i newSize)
    {
        Resize((float)newSize.X / newSize.Y);
        UpdateProjection(shaderProgram.GetHandle(),shaderProgram.DefaultProjection);
    }

    /// <summary>
    /// Set the camera's field of view
    /// </summary>
    /// <param name="fieldOfView">field of view in radians</param>
    public void SetFov(float fieldOfView)
    {
        fov = fieldOfView;
    }

    /// <summary>
    /// Change the near and far clip distances
    /// </summary>
    /// <param name="near">the closest distance to render</param>
    /// <param name="far">the furthest distance to render</param>
    public void SetDepth(float near, float far)
    {
        depthNear = near;
        depthFar = far;
    }

    /// <summary>
    /// Update the view matrix relative to the camera's current position
    /// </summary>
    /// <param name="flipCamera">if ture, the camera will be upside down</param>
    public void UpdateView(int programId, int binding, bool flipCamera = false)
    {
        GL.UseProgram(programId);
        ViewMatrix = Matrix4.LookAt(Position, Position + Direction, ((flipCamera)?-1:1) * up);
        GL.UniformMatrix4(binding,false,ref ViewMatrix);
    }

}