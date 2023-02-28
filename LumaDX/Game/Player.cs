﻿using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Graphics.OpenGL4;

namespace LumaDX;

/*
 
 TODO: make all this not suck

/// <summary>
/// Simplification of movement and camera handling - dependent on Camera class
/// </summary>
public abstract class Player
{
    public Vector3 Position;
    public Camera Camera;
    
    /// <summary>
    /// Create new player and create camera based off window size
    /// </summary>
    /// <param name="windowSize">the screen's size</param>
    /// <param name="fov">the camera's field of view in radians</param>
    public Player(Vector2i windowSize, float fov = MathHelper.PiOver3)
    {
        Camera = new Camera(windowSize, fov);
    }    
    
    /// <summary>
    /// Create new player and create camera based off aspect ratio
    /// </summary>
    /// <param name="aspectRatio">the screen's aspect ratio</param>
    /// <param name="fov">the camera's field of view in radians</param>
    public Player(float aspectRatio, float fov = MathHelper.PiOver3)
    {
        Camera = new Camera(aspectRatio, fov);
    }    
    
    /// <summary>
    /// Create a new player with a pre-existing camera
    /// </summary>
    /// <param name="camera">the camera to attach to the player</param>
    public Player(ref Camera camera)
    {
        Camera = camera;
    }

}
*/


public class FirstPersonPlayer : PhysicsPlayer
{

    //public Vector3 Velocity;
    public float Sensitivity;
    public float Speed;
    public Camera Camera;

    private Vector3 unitGravity = -Vector3.UnitY;
    public void SetGravity(Vector3 direction) => unitGravity = direction;

    /// <summary>
    /// Create first person player with [wasd + space/ctrl] controls
    /// </summary>
    /// <param name="windowSize">the screen's size</param>
    /// <param name="fov">the camera's field of view in radians</param>
    /// <param name="sensitivity">the mouse sensitivity</param>
    /// <param name="speed">player's speed</param>
    public FirstPersonPlayer(Vector2i windowSize, float fov = MathHelper.PiOver3, float sensitivity = 1/20f, float speed = 5f, Vector3 ellipsoidRadius = default)
    {
        if (ellipsoidRadius == default) ellipsoidRadius = new(0.2f, 0.5f, 0.2f);
        
        Sensitivity = sensitivity;
        Speed = speed;
        Camera = new Camera(windowSize, fov);
        Radius = ellipsoidRadius;
    }

    /// <summary>
    /// Manually set the player's position
    /// </summary>
    /// <param name="position">new player position</param>
    /// <returns>current object for ease of use</returns>
    public FirstPersonPlayer SetPosition(Vector3 position)
    {
        Position = position;
        LastPosition = Position;
        return this;
    }
        
    /// <summary>
    /// Manually set the camera's direction
    /// </summary>
    /// <param name="direction">new camera direction</param>
    /// <returns>current object for ease of use</returns>
    public FirstPersonPlayer SetDirection(Vector3 direction)
    {
        Direction = direction;
        Camera.Direction = direction;
        yaw = MathHelper.RadiansToDegrees(MathF.Atan2(Camera.Direction.X, -Camera.Direction.Z));
        pitch = MathHelper.RadiansToDegrees(MathF.Acos(Camera.Direction.Y)) - 90f;
        return this;
    }

    private readonly Matrix3 rightTransform = Matrix3.CreateRotationY(MathHelper.PiOver2);

    private Vector2 lastMousePos;
    private float yaw;
    private float pitch;

    private bool capPitch = true;
    private bool isCameraFlipped = false;
    
    /*
    /// <summary>
    /// Move and rotate the player and camera, as well as updating the camera's view
    /// </summary>
    /// <param name="args">args from the window's update function</param>
    /// <param name="keyboardState">the keyboard state to check inputs from</param>
    /// <param name="relativeMousePos">the relative mouse pos from the last call of SetMouseOrigin()</param>
    public FirstPersonPlayer Update(FrameEventArgs args, KeyboardState keyboardState, Vector2 relativeMousePos)
    {
        var input = Input.DirectionWASD(keyboardState) * Speed * (float)args.Time;
        yaw += (relativeMousePos.X - lastMousePos.X) * Sensitivity;
        pitch += (relativeMousePos.Y - lastMousePos.Y) * Sensitivity;

        yaw %= 360;
        pitch %= 360;

        // 90 degrees gives gimbal locking so lock to 89
        if (capPitch) pitch = Math.Clamp(pitch, -89f, 89f);

        Camera.Direction = Matrix3.CreateRotationY(MathHelper.DegreesToRadians(yaw)) * Matrix3.CreateRotationX(MathHelper.DegreesToRadians(pitch)) * -Vector3.UnitZ;
        
        Vector3 up = ((keyboardState.IsKeyDown(Keys.Space) ?1:0) - (keyboardState.IsKeyDown(Keys.LeftControl) ?1:0)) * Speed * (float)args.Time * Vector3.UnitY;
        
        Vector3 directionFlat = Camera.Direction;
        directionFlat.Y = 0;
        directionFlat.Normalize();

        Velocity = input.Z * directionFlat + input.X * (rightTransform * directionFlat) + up;
        
        Position += Velocity;
        Camera.Position = Position;

        isCameraFlipped = (Math.Abs(pitch) + 90) % 360 >= 180;
        
        lastMousePos = relativeMousePos;

        return this;
    }*/
    
    public Vector3 LastPosition = Vector3.Zero;
    
    
    public bool NoClip;

    private Vector3 directionFlat;

    public PhysicsPlayer UpdateCamera(FrameEventArgs args, Vector2 relativeMousePos)
    {
        yaw += (relativeMousePos.X - lastMousePos.X) * Sensitivity;
        pitch += (relativeMousePos.Y - lastMousePos.Y) * Sensitivity;

        yaw %= 360;
        pitch %= 360;

        // 90 degrees gives gimbal locking so lock to 89
        if (capPitch) pitch = Math.Clamp(pitch, -89f, 89f);

        Camera.Direction = Matrix3.CreateRotationY(MathHelper.DegreesToRadians(yaw)) * Matrix3.CreateRotationX(MathHelper.DegreesToRadians(pitch)) * -Vector3.UnitZ;
        
        
        directionFlat = Camera.Direction;
        directionFlat.Y = 0;
        directionFlat.Normalize();
        
        isCameraFlipped = (Math.Abs(pitch) + 90) % 360 >= 180;
        
        lastMousePos = relativeMousePos;

        return this;
    }

    public PhysicsPlayer Update(FrameEventArgs args, KeyboardState keyboardState)
    {
        LastPosition = Position;
        
        var input = Input.DirectionWASD(keyboardState) * Speed * (float)args.Time;
        Vector3 up = ((keyboardState.IsKeyDown(Keys.Space) ?1:0) - (keyboardState.IsKeyDown(Keys.LeftControl) ?1:0)) * Speed * (float)args.Time* Vector3.UnitY;
        

        if (NoClip)
        {
            Velocity = (input.Z * Camera.Direction.Normalized() + input.X * (rightTransform * directionFlat))*2f + up;
            
            Position += Velocity;
            Gravity = Vector3.Zero;
        }
        else
        {
            Velocity = input.Z * directionFlat + input.X * (rightTransform * directionFlat);
            PhysicsUpdate((float)args.Time);
            if (keyboardState.IsKeyDown(Keys.Space)) Jump();
        }
        
        
        Camera.Position = Position + new Vector3(0f, 0.25f, 0f);

        return this;
    }
    
    

    
    /// <summary>
    /// ... - only for use with single shader programs
    /// </summary>
    /// <param name="shaderProgram"></param>
    /// <param name="args"></param>
    /// <param name="keyboardState"></param>
    /// <param name="relativeMousePos"></param>
    /// <returns></returns>
    public FirstPersonPlayer Update(ShaderProgram shaderProgram, FrameEventArgs args, KeyboardState keyboardState, Vector2 relativeMousePos)
    {
        UpdateCamera(args, relativeMousePos);
        Update(args, keyboardState);
        UpdateView(shaderProgram);
        return this;
    }
    
    
    public FirstPersonPlayer UpdateCamera(ShaderProgram shaderProgram, FrameEventArgs args, Vector2 relativeMousePos)
    {
        UpdateCamera(args, relativeMousePos);
        UpdateView(shaderProgram);
        return this;
    }
    
    public void Update(FrameEventArgs args, KeyboardState keyboardState, Vector2 relativeMousePos)
    {
        UpdateCamera(args, relativeMousePos);
        Update(args, keyboardState);
    }
    
    

    /// <summary>
    /// Direction handled in the player's camera
    /// </summary>
    public Vector3 Direction
    {
        get => Camera.Direction;
        set => Camera.Direction=value;
    }

    public FirstPersonPlayer SetFov(float angle)
    {
        Camera.SetFov(angle);
        return this;
    }
    
    public FirstPersonPlayer UpdateProjection(int programId, int binding)
    {
        Camera.UpdateProjection(programId,binding);
        return this;
    }
    public FirstPersonPlayer UpdateProjection(int programId, string name)
    {
        Camera.UpdateProjection(programId, GL.GetUniformLocation(programId, name));
        return this;
    }
    public FirstPersonPlayer UpdateProjection(ShaderProgram program)
    {
        Camera.UpdateProjection(program.GetHandle(), program.DefaultProjection);
        return this;
    }
    
    public FirstPersonPlayer UpdateView(int programId, int binding)
    {
        Camera.UpdateView(programId,binding,isCameraFlipped);
        return this;
    }
    public FirstPersonPlayer UpdateView(int programId, string name)
    {
        Camera.UpdateView(programId, GL.GetUniformLocation(programId, name));
        return this;
    }
    public FirstPersonPlayer UpdateView(ShaderProgram program)
    {
        Camera.UpdateView(program.GetHandle(), program.DefaultView);
        return this;
    }
    
}