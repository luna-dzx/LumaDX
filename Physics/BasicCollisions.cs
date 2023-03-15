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
    StateHandler glState;

    ShaderProgram shader;

    Model dingus;
    Model ellipsoid;

    FirstPersonPlayer player;
    Texture texture;

    PhysicsPlayer physicsPlayer;
    
    Matrix4 sceneTransform = Maths.CreateTransformation(Vector3.UnitY * -0.7f, new Vector3(MathF.PI / -2f, MathF.PI / 4f, 0f), 0.1f * Vector3.One, true);
    

    void ResetPhysicsPlayer() => physicsPlayer.Position = new (0f, 0f, 5f);
    

    protected override void Initialize()
    {
        UnlockMouse();
        
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
        physicsPlayer.PhysicsUpdate((float)args.Time, checkGravity: false);
    }

    protected override void KeyboardHandling(FrameEventArgs args, KeyboardState k)
    {
        if (k.IsKeyPressed(Keys.Backspace)) ResetPhysicsPlayer();
    }

    protected override void RenderFrame(FrameEventArgs args)
    {
        glState.Clear();

        ellipsoid.UpdateTransform(shader, physicsPlayer.Position, Vector3.Zero, physicsPlayer.Radius);

        shader.SetActive(ShaderType.FragmentShader,"sphere");
        shader.Uniform3("colour", Vector3.One * 0.8f);
        ellipsoid.Draw();
        shader.Uniform3("colour", Vector3.Zero);
        ellipsoid.DrawShell(glState);
        
        shader.SetActive(ShaderType.FragmentShader,"dingus");
        dingus.EnableTranspose();
        dingus.Transform(shader, sceneTransform);
        dingus.Draw();

        Window.SwapBuffers();
    }

    protected override void Unload()
    {
        glState.Unbind();
        
        dingus.Dispose();
        ellipsoid.Dispose();
        shader.Dispose();
    }
}