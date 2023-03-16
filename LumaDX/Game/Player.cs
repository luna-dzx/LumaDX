using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace LumaDX;

/// <summary>
/// Physics player with a Camera
/// </summary>
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

    public Vector3 LastPosition = Vector3.Zero;
    
    
    public bool NoClip;

    /// <summary>
    /// Set NoClip to true with interfacing
    /// </summary>
    public FirstPersonPlayer EnableNoClip()
    {
        NoClip = true;
        return this;
    }
    
    /// <summary>
    /// Set NoClip to false with interfacing
    /// </summary>
    public FirstPersonPlayer DisableNoClip()
    {
        NoClip = false;
        return this;
    }
    

    private Vector3 directionFlat;

    /// <summary>
    /// Update look direction based on mouse movement
    /// </summary>
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

    /// <summary>
    /// Update movement based on keyboard inputs, and physics if NoClip is disabled
    /// </summary>
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
    /// Update camera and player, then load matrices to the GPU
    /// </summary>
    public FirstPersonPlayer Update(ShaderProgram shaderProgram, FrameEventArgs args, KeyboardState keyboardState, Vector2 relativeMousePos)
    {
        UpdateCamera(args, relativeMousePos);
        Update(args, keyboardState);
        UpdateView(shaderProgram);
        return this;
    }
    
    /// <summary>
    /// Update camera, then load matrices to the GPU
    /// </summary>
    public FirstPersonPlayer UpdateCamera(ShaderProgram shaderProgram, FrameEventArgs args, Vector2 relativeMousePos)
    {
        UpdateCamera(args, relativeMousePos);
        UpdateView(shaderProgram);
        return this;
    }
    
    /// <summary>
    /// Update camera and player
    /// </summary>
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

    /// <summary>
    /// Set field of view
    /// </summary>
    public FirstPersonPlayer SetFov(float angle)
    {
        Camera.SetFov(angle);
        return this;
    }

    /// <summary>
    /// Update camera projection matrix to the GPU
    /// </summary>
    public FirstPersonPlayer UpdateProjection(ShaderProgram program)
    {
        Camera.UpdateProjection(program.GetHandle(), program.DefaultProjection);
        return this;
    }
    
    /// <summary>
    /// Update camera view matrix to the GPU
    /// </summary>
    public FirstPersonPlayer UpdateView(ShaderProgram program)
    {
        Camera.UpdateView(program.GetHandle(), program.DefaultView);
        return this;
    }
    
}