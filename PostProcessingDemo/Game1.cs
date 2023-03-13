using Assimp;
using LumaDX;
using ImGuiNET;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using MouseClick = OpenTK.Windowing.GraphicsLibraryFramework.MouseButton;
using Vector3 = OpenTK.Mathematics.Vector3;
using PostProcessShader = LumaDX.PostProcessing.PostProcessShader;


namespace PostProcessingDemo;

public class Game1 : Game
{
    StateHandler glState;

    ImGuiController imGui;

    const string ShaderLocation = "Shaders/";
    ShaderProgram shader;
    
    const string AssetLocation = "Assets/";
    Texture skyBox;
    Texture texture;

    Model dingus;
    Model cube;

    FirstPersonPlayer player;

    PostProcessing postProcessor;

    string[] effectNames;


    protected override void Initialize()
    {
        glState = new StateHandler();
        
        glState.ClearColor = Color4.Black;

        imGui = new ImGuiController(Window);
        
        UnlockMouse();

        shader = new ShaderProgram(
            ShaderLocation + "vertex.glsl",
            ShaderLocation + "fragment.glsl",
            true
        );

        player = new FirstPersonPlayer(Window.Size)
            .SetPosition(new Vector3(0f,0f,5f))
            .SetDirection(-Vector3.UnitZ)
            .EnableNoClip();
        

        skyBox = Texture.LoadCubeMap(AssetLocation + "skybox/", ".jpg", 0);
        cube = new Model(PresetMesh.Cube);

        texture = new Texture(AssetLocation + "dingus-the-cat/textures/dingus_nowhiskers.jpg", 1);

        FileManager fm = new FileManager(AssetLocation + "dingus-the-cat/source/dingus.fbx");
        dingus = fm.LoadModel(0);
        

        postProcessor = new PostProcessing(
            PostProcessShader.GaussianBlur
            | PostProcessShader.MatrixText
            | PostProcessShader.NightVision
            | PostProcessShader.GreyScale
            ,
            Window.Size, fontFile: AssetLocation + "fonts/migu.ttf");

        effectNames = new[] { "None", "Blur", "GreyScale", "NightVision", "Matrix" };
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

    int currentEffect = 0;

    protected override void RenderFrame(FrameEventArgs args)
    {
        postProcessor.StartSceneRender();
        
        glState.Clear();
        
        // skyBox is maximum depth, so we want to render if it's <= instead of just <
        glState.DepthFunc = DepthFunction.Lequal;

        skyBox.Use();
        texture.Use();

        glState.DoCulling = true;
        shader.SetActive("scene");
        dingus.Draw(shader, new Vector3(0f,0f,-10f), new Vector3(-MathF.PI/2f,0f,0f), 0.2f);
        
        
        glState.DoCulling = false;
        shader.SetActive("skyBox");
        cube.Draw();
        
        glState.DoCulling = true;

        postProcessor.EndSceneRender();


        switch (effectNames[currentEffect])
        {
            case "None": break;
            case "Blur": postProcessor.RenderEffect(PostProcessShader.GaussianBlur); break;
            case "GreyScale": postProcessor.RenderEffect(PostProcessShader.GreyScale); break;
            case "NightVision": postProcessor.RenderEffect(PostProcessShader.NightVision); break;
            case "Matrix": postProcessor.RenderEffect(PostProcessShader.MatrixText); break;
        }
        
        postProcessor.DrawFbo();


        #region Debug UI
        
        imGui.Update((float)args.Time);
        
        if (!imGui.IsFocused()) LockMouse();;

        ImGui.ListBox("Effect", ref currentEffect, effectNames, effectNames.Length);
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
        
        postProcessor.Dispose();
        
        shader.Dispose();
    }
}