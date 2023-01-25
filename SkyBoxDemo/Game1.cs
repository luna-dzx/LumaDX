using LumaDX;
using Assimp;
using ImGuiNET;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using MouseClick = OpenTK.Windowing.GraphicsLibraryFramework.MouseButton;


namespace SkyBoxDemo;

public class Game1 : Game
{
    StateHandler glState;

    ImGuiController imGui;

    const string ShaderLocation = "Shaders/";
    ShaderProgram shader;
    
    const string AssetLocation = "Assets/";
    Texture skyBox;

    private const int backpackCount = 100;
    Matrix4[] backpackTransforms;
    Model backpack;
    Model cube;

    FirstPersonPlayer player;
    
    float refractionRatio = 1f / 1.33f;


    protected override void Initialize()
    {
        glState = new StateHandler();
        
        glState.ClearColor = Color4.Black;

        imGui = new ImGuiController(Window);
        
        LockMouse();



        shader = new ShaderProgram(
            ShaderLocation + "vertex.glsl",
            ShaderLocation + "fragment.glsl",
            true
        );

        player = new FirstPersonPlayer(Window.Size)
            .SetPosition(new Vector3(0f,0f,5f))
            .SetDirection(-Vector3.UnitZ);
        player.NoClip = true;

        skyBox = Texture.LoadCubeMap(AssetLocation + "skybox/", ".jpg", 0);
        cube = new Model(PresetMesh.Cube);
        
        backpack = Model.FromFile(AssetLocation + "backpack/", "backpack.obj", out _);


        backpackTransforms = new Matrix4[backpackCount];
        for (int i = 0; i < backpackCount; i++)
        {
            float angle = 5f * i * MathF.Tau / backpackCount;
            backpackTransforms[i] = Maths.CreateTransformation(
                new Vector3(MathF.Sin(angle*3f) * 20f, MathF.Sin(angle) * 3f,MathF.Cos(angle*3f) * 20f),
                new Vector3(0f,angle,0f),
                0.8f * Vector3.One);
        }
    }

    protected override void Load()
    {
        shader.UniformTexture("skyBox", skyBox);
        player.UpdateProjection(shader);

        backpack.LoadMatrix(3, backpackTransforms, 4, 4, 1);
    }
    
    protected override void Resize(ResizeEventArgs newWin) => player.Camera.Resize(newWin.Size);

    Vector2 playerMousePos = Vector2.Zero;
    protected override void MouseMove(MouseMoveEventArgs moveInfo)
    {
        if (MouseLocked)
        {
            playerMousePos += moveInfo.Delta;
        }
    }

    protected override void UpdateFrame(FrameEventArgs args)
    {
        player.Update(shader, args, Window.KeyboardState, playerMousePos);
        shader.Uniform3("cameraPos", player.Camera.Position);
        shader.Uniform1("refractionRatio", refractionRatio);
    }

    protected override void KeyboardHandling(FrameEventArgs args, KeyboardState k)
    {
        if (k.IsKeyPressed(Keys.Enter) && MouseLocked) // unlock mouse
        {
            UnlockMouse();
            imGui.FocusWindow();
        }
    }

    protected override void RenderFrame(FrameEventArgs args)
    {
        glState.Clear();
        
        // skyBox is maximum depth, so we want to render if it's <= instead of just <
        glState.DepthFunc = DepthFunction.Lequal;

        skyBox.Use();

        glState.DoCulling = true;
        shader.SetActive("scene");
        backpack.Draw(backpackCount);
        
        
        glState.DoCulling = false;
        shader.SetActive("skyBox");
        cube.Draw();
        
        glState.DoCulling = true;
        


        #region Debug UI
        
        imGui.Update((float)args.Time);
        
        if (!imGui.IsFocused()) LockMouse();;
        
        ImGui.SliderFloat("Refraction Ratio", ref refractionRatio, 0f, 1f);
        imGui.Render();
        
        #endregion
        

        Window.SwapBuffers();
    }

    protected override void Unload()
    {
        GL.BindVertexArray(0);
        GL.UseProgram(0);
        
        backpack.Dispose();
        cube.Dispose();
        
        shader.Dispose();
    }
}