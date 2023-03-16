using LumaDX;
using ImGuiNET;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using MouseClick = OpenTK.Windowing.GraphicsLibraryFramework.MouseButton;
using Vector3 = OpenTK.Mathematics.Vector3;
using PostProcessShader = LumaDX.PostProcessing.PostProcessShader;

namespace ComplexRendering;

public class SkyBoxInSceneDemo: Game
{
    StateHandler glState;

    ImGuiController imGui;

    ShaderProgram shader;
    
    Texture skyBox;
    Texture texture;

    Model dingus;
    Model cube;

    FirstPersonPlayer player;

    bool renderScene = false;
    


    protected override void Initialize()
    {
        UnlockMouse();
        
        glState = new StateHandler();
        
        glState.ClearColor = Color4.Black;

        imGui = new ImGuiController(Window);

        shader = new ShaderProgram(
            Program.ShaderLocation + "SkyBoxInScene/vertex.glsl",
            Program.ShaderLocation + "SkyBoxInScene/fragment.glsl",
            true
        );

        player = new FirstPersonPlayer(Window.Size)
            .SetPosition(new Vector3(0f,0f,5f))
            .SetDirection(-Vector3.UnitZ)
            .EnableNoClip();
        

        skyBox = Texture.LoadCubeMap(Program.AssetLocation + "skybox/", ".jpg", 0);
        cube = new Model(PresetMesh.Cube);

        texture = new Texture(Program.AssetLocation + "dingus-the-cat/textures/dingus_nowhiskers.jpg", 1);

        FileManager fm = new FileManager(Program.AssetLocation + "dingus-the-cat/source/dingus.fbx");
        dingus = fm.LoadModel(0);
    }

    protected override void Load()
    {
        shader.UniformTexture("skyBox", skyBox);
        shader.UniformTexture("dingus", texture);
    }
    
    protected override void Resize(ResizeEventArgs newWin) => player.Camera.Resize(newWin.Size);
    
    
    protected override void UpdateFrame(FrameEventArgs args)
    {
        player.Update(shader, args, Window.KeyboardState, GetPlayerMousePos());
        shader.Uniform3("cameraPos", player.Camera.Position);
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
        dingus.Draw(shader, new Vector3(0f,0f,-10f), new Vector3(-MathF.PI/2f,0f,0f), 0.2f);
        
        if (!renderScene) GL.Clear(ClearBufferMask.ColorBufferBit);
        
        glState.DoCulling = false;
        shader.SetActive("skyBox");
        cube.Draw();
        
        glState.DoCulling = true;



        #region Debug UI
        
        imGui.Update((float)args.Time);
        
        if (!imGui.IsFocused()) LockMouse();;

        ImGui.Checkbox("Render 3D Model", ref renderScene);

        
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