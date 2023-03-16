// -------------------- InstancedRendering.cs -------------------- //

using LumaDX;
using ImGuiNET;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using MouseClick = OpenTK.Windowing.GraphicsLibraryFramework.MouseButton;

namespace ComplexRendering;

public class InstancedRenderingDemo: Game
{
    StateHandler glState;

    ImGuiController imGui;

    ShaderProgram shader;
    
    Texture skyBox;
    Texture texture;

    Model dingus;
    Model cube;

    FirstPersonPlayer player;
    
    int dingusCount = 1806;
    float refractionRatio = 1f / 1.33f;
    float fieldOfView = MathHelper.DegreesToRadians(80f);
    float speed = 1f;
    bool renderRefraction = false;

    protected override void Initialize()
    {
        UnlockMouse();
        
        glState = new StateHandler();
        
        glState.ClearColor = Color4.Black;

        imGui = new ImGuiController(Window);

        shader = new ShaderProgram(
            Program.ShaderLocation + "InstancedRendering/vertex.glsl",
            Program.ShaderLocation + "InstancedRendering/fragment.glsl",
            true
        );

        player = new FirstPersonPlayer(Window.Size)
            .SetPosition(new Vector3(0f,0f,5f))
            .SetDirection(-Vector3.UnitZ)
            .EnableNoClip()
            .SetFov(fieldOfView);

        skyBox = Texture.LoadCubeMap(Program.AssetLocation + "skybox/", ".jpg", 0);
        cube = new Model(PresetMesh.Cube);

        texture = new Texture(Program.AssetLocation + "/dingus-the-cat/textures/dingus_nowhiskers.jpg", 1);

        FileManager fm = new FileManager(Program.AssetLocation + "dingus-the-cat/source/dingus.fbx");
        dingus = fm.LoadModel(0);
    }

    protected override void Load()
    {
        shader.UniformTexture("skyBox", skyBox);
        shader.UniformTexture("dingus", texture);
        shader.Uniform1("dingusCount", dingusCount);
        
        LockMouse();
    }
    
    protected override void Resize(ResizeEventArgs newWin) => player.Camera.Resize(newWin.Size);


    float time = 0f;
    
    protected override void UpdateFrame(FrameEventArgs args)
    {
        player.UpdateCamera(shader, args, GetPlayerMousePos());
        shader.Uniform3("cameraPos", player.Camera.Position);
        shader.Uniform1("refractionRatio", refractionRatio);

        time += speed * (float)args.Time;
        shader.Uniform1("time", time);
        
        shader.Uniform1("dingusCount", dingusCount);
        shader.Uniform1("renderRefraction", renderRefraction?1:0);
        
        player.Camera.SetFov(fieldOfView);
        player.UpdateProjection(shader);
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
        texture.Use();

        glState.DoCulling = true;
        shader.SetActive("scene");
        dingus.Draw(dingusCount);
        
        
        glState.DoCulling = false;
        shader.SetActive("skyBox");
        cube.Draw();
        
        glState.DoCulling = true;
        


        #region Debug UI
        
        imGui.Update((float)args.Time);
        
        if (!imGui.IsFocused()) LockMouse();;

        ImGui.Checkbox("Render Refraction", ref renderRefraction);
        ImGui.SliderFloat("Refraction Ratio", ref refractionRatio, 0f, 1f);
        ImGui.SliderInt("Num. Dingus", ref dingusCount, 1, 100000);
        ImGui.SliderFloat("Field of View", ref fieldOfView, 0.01f * MathF.PI, 0.99f * MathF.PI);
        ImGui.SliderFloat("Rotation Speed", ref speed, -30f, 30f);
        imGui.Render();
        
        #endregion
        

        Window.SwapBuffers();
    }

    protected override void Unload()
    {
        glState.Unbind();
        
        dingus.Dispose();
        cube.Dispose();
        
        skyBox.Dispose();
        texture.Dispose();
        
        shader.Dispose();
    }
}