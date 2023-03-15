using LumaDX;
using ImGuiNET;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using MouseClick = OpenTK.Windowing.GraphicsLibraryFramework.MouseButton;
using Vector3 = OpenTK.Mathematics.Vector3;
using Triangle = LumaDX.Maths.Triangle;

namespace Physics;

public class BasicCollisionDemo: Game
{
    ImGuiController imGui;
    
    StateHandler glState;

    ShaderProgram shader;

    Model dingus;
    Model ellipsoid;

    FirstPersonPlayer player;
    Texture texture;

    PhysicsPlayer physicsPlayer;
    
    Matrix4 sceneTransform = Maths.CreateTransformation(Vector3.UnitY * -0.7f, new Vector3(MathF.PI / -2f, MathF.PI / 4f, 0f), 0.1f * Vector3.One, true);

    bool collisionResponse = false;
    bool highlightTriangles = false;
    bool highlightPlayer = false;
    bool playerColliding = false;

    void ResetPhysicsPlayer() => physicsPlayer.Position = new (0f, 0f, 5f);
    

    protected override void Initialize()
    {
        UnlockMouse();

        imGui = new ImGuiController(Window);
        
        glState = new StateHandler();
        glState.ClearColor = Color4.Black;

        shader = new ShaderProgram(
            Program.ShaderLocation + "BasicCollisions/vertex.glsl",
            Program.ShaderLocation + "BasicCollisions/fragment.glsl",
            true
        );

        player = new FirstPersonPlayer(Window.Size)
            .SetPosition(new Vector3(0f,0f,5f))
            .SetDirection(-Vector3.UnitZ)
            .UpdateProjection(shader)
            .EnableNoClip();
        
        physicsPlayer = new PhysicsPlayer(Vector3.Zero, new(0.4f, 1f, 0.4f));

        FileManager fm = new FileManager(Program.AssetLocation + "dingus-the-cat/source/dingus.fbx");
        dingus = fm.LoadModel(0);
        Collision.World = fm.LoadTriangles(sceneTransform, physicsPlayer.Radius);
        ellipsoid = new Model(Program.EllipsoidMesh);
        
        texture = new Texture(Program.AssetLocation + "dingus-the-cat/textures/dingus_nowhiskers.jpg", 0);

        ResetPhysicsPlayer();
    }

    protected override void Load()
    {
        texture.Use();
        shader.UniformTexture("dingus", texture);
        LockMouse();
    }
    
    protected override void Resize(ResizeEventArgs newWin) => player.Camera.Resize(newWin.Size);
    
    
    protected override void UpdateFrame(FrameEventArgs args)
    {
        player.Update(shader, args, Window.KeyboardState, GetPlayerMousePos());
        shader.Uniform3("cameraPos", player.Camera.Position);
        player.UpdateProjection(shader);

        physicsPlayer.Velocity = -Vector3.UnitZ * 2f * (float)args.Time;
        var ePos = physicsPlayer.Position / physicsPlayer.Radius;
        var eVel = physicsPlayer.Velocity / physicsPlayer.Radius;
        
        if (collisionResponse) physicsPlayer.PhysicsUpdate((float)args.Time, checkGravity: false);
        else
        {
            // transform by ellipsoid radius
            var closeTriangles = Collision.GetCloseTriangles(ePos, eVel);
            
            eVel *= 3f; // for visualization, since velocity is extremely small after multiplying it by args.Time
            var (_,distance) = Collision.SceneIntersection(ePos, eVel, ref closeTriangles);
            playerColliding = !(distance  >= float.PositiveInfinity);
            physicsPlayer.Position += physicsPlayer.Velocity;
        }

        if (highlightTriangles)
        {
            shader.Uniform3("playerPos", ePos);
            shader.Uniform3("playerRadius", physicsPlayer.Radius);
            shader.Uniform1("triangleDistance", eVel.Length);
            shader.Uniform1("renderCloseTriangles", 1);
        }
        else shader.Uniform1("renderCloseTriangles", 0);


        if (physicsPlayer.Position.Z < -5f) ResetPhysicsPlayer();
    }

    protected override void KeyboardHandling(FrameEventArgs args, KeyboardState k)
    {
        if (k.IsKeyPressed(Keys.Backspace)) ResetPhysicsPlayer();
        
        if (k.IsKeyPressed(Keys.Enter) && MouseLocked) // unlock mouse
        {
            UnlockMouse();
            imGui.FocusWindow();
        }
    }

    protected override void RenderFrame(FrameEventArgs args)
    {
        glState.Clear();

        ellipsoid.LoadTransformation(shader, physicsPlayer.Position, Vector3.Zero, physicsPlayer.Radius);

        shader.SetActive(ShaderType.FragmentShader,"sphere");
        shader.Uniform3("colour", (playerColliding && highlightPlayer)? Vector3.UnitX : (Vector3.One * 0.8f));
        ellipsoid.Draw();
        shader.Uniform3("colour", Vector3.Zero);
        ellipsoid.DrawShell(glState);
        
        shader.SetActive(ShaderType.FragmentShader,"dingus");
        dingus.EnableTranspose();
        dingus.LoadTransformation(shader, sceneTransform);
        dingus.Draw();
        
        #region Debug UI
        
        imGui.Update((float)args.Time);
        
        if (!imGui.IsFocused()) LockMouse();;
        
        ImGui.Checkbox("Change Velocity", ref collisionResponse);
        ImGui.Checkbox("Highlight Player", ref highlightPlayer);
        ImGui.Checkbox("Highlight Triangles", ref highlightTriangles);

        imGui.Render();
        
        #endregion

        Window.SwapBuffers();
    }

    protected override void Unload()
    {
        glState.Unbind();
        
        imGui.Dispose();
        dingus.Dispose();
        ellipsoid.Dispose();
        shader.Dispose();
    }
}