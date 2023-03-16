using ImGuiNET;
using LumaDX;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace DynamicLighting;

public class BasicShadowDemo: Game
{
    ImGuiController imGui;
    
    StateHandler glState;
    TextRenderer textRenderer;

    ShaderProgram shader;
    ShaderProgram frameBufferShader;

    FirstPersonPlayer player;
    Model cube;
    Model quad;

    Objects.Light light;
    Objects.Material material;

    DepthMap depthMap;

    Texture texture;
    
    bool visualiseDepthMap = false;
    bool blurEdges = false;
    
    private Vector3 cubePosition;
    

    protected override void Initialize()
    {
        UnlockMouse();
        
        glState = new StateHandler();
        glState.ClearColor = Color4.Black;
        
        imGui = new ImGuiController(Window);

        shader = new ShaderProgram(
            Program.ShaderLocation + "BasicShadows/vertex.glsl", 
            Program.ShaderLocation + "BasicShadows/fragment.glsl",
            true);

        frameBufferShader = new ShaderProgram(Program.ShaderLocation + "BasicShadows/frameBufferFrag.glsl");

        player = new FirstPersonPlayer(Window.Size)
            .SetPosition(new Vector3(0,0,6))
            .SetDirection(new Vector3(0, 0, -1))
            .EnableNoClip();

        cube = new Model(PresetMesh.Cube);
            
        quad = new Model(PresetMesh.Square).Transform(new Vector3(0f,-5f,0f), new Vector3(MathHelper.DegreesToRadians(-90f),0f,0f),10f * Vector3.One);

        texture = new Texture(Program.AssetLocation+"tiled.jpg",0);
        
        depthMap = new DepthMap((4096,4096),(-3.5f,8.5f,20f),(1f,-4f,-5f));
        
        light = new Objects.Light().SunMode().SetDirection(depthMap.Direction).SetAmbient(0.1f);
        material = PresetMaterial.Silver.SetAmbient(0.1f);

        textRenderer = new TextRenderer(30, Window.Size, Program.AssetLocation+"fonts/migu.ttf");
    }

    protected override void Load()
    {
        player.UpdateProjection(shader);
        
        texture.Use();
        
        shader.UniformMaterial("material",material,texture)
            .UniformLight("light",light);

        depthMap.UniformTexture(shader,"depthMap",1);
        depthMap.UniformTexture(frameBufferShader,"depthMap",1);
        
        depthMap.ProjectOrthographic();
        depthMap.UniformMatrix(shader, "lightSpaceMatrix");
        
        shader.EnableGammaCorrection();
        
        LockMouse();
    }
    
    protected override void Resize(ResizeEventArgs newWin) => player.Camera.Resize(shader,newWin.Size);

    double time = 0.0;
    
    protected override void UpdateFrame(FrameEventArgs args)
    {
        player.Update(shader, args, Window.KeyboardState, GetPlayerMousePos());
        shader.Uniform3("cameraPos", player.Position);

        time += args.Time;
        cubePosition = 4.8f * new Vector3((float)Math.Cos(time),0f,(float)Math.Sin(time)) + new Vector3(0f, -4f, 0f);

        shader.Uniform1("blurEdges", blurEdges ? 1 : 0);
    }

    protected override void KeyboardHandling(FrameEventArgs args, KeyboardState k)
    {
        if (k.IsKeyPressed(Keys.Enter) && MouseLocked) // unlock mouse
        {
            UnlockMouse();
            imGui.FocusWindow();
        }
    }

    void DrawScene()
    {
        shader.Uniform1("texCoordsMult", 4f);
        quad.Draw(shader);
        
        shader.Uniform1("texCoordsMult", 0.4f);
        cube.Draw(shader,cubePosition, new Vector3(0f,0.2f,0f), 1f);
        cube.Draw(shader,new Vector3(-3f,-1f,3f), new Vector3(0.4f,0f,0f), 1f);
    }

    protected override void RenderFrame(FrameEventArgs args)
    {
        glState.SaveState();
        depthMap.WriteMode();

        shader.Uniform1("texCoordsMult", 0.4f);
        cube.Draw(depthMap.Shader, cubePosition, new Vector3(0f,0.2f,0f), 1f);
        cube.Draw(depthMap.Shader,new Vector3(-3f,-1f,3f), new Vector3(0.4f,0f,0f), 1f);
        
        
        shader.Use();
        depthMap.ReadMode();
        
        glState.LoadState();
        glState.Clear();

        GL.Viewport(0,Window.Size.Y/2,Window.Size.X/2,Window.Size.Y/2);
        shader.Uniform1("visualisation", 0);
        DrawScene();

        GL.Viewport(Window.Size.X/2,Window.Size.Y/2,Window.Size.X/2,Window.Size.Y/2);
        frameBufferShader.Use();
        PostProcessing.Draw();
        
        GL.Viewport(0,0,Window.Size.X/2,Window.Size.Y/2);
        shader.Uniform1("visualisation", 1);
        DrawScene();
        
        GL.Viewport(Window.Size.X/2,0,Window.Size.X/2,Window.Size.Y/2);
        shader.Uniform1("visualisation", 2);
        DrawScene();

        
        GL.Viewport(0,0,Window.Size.X,Window.Size.Y);
        glState.Blending = true;
        glState.ClearBuffers = ClearBufferMask.DepthBufferBit;
        glState.Clear();
        textRenderer.Draw("Projected Depth", 10f, 10f, 1f, Vector3.UnitX, false);
        textRenderer.Draw("Final Scene", Window.Size.X/2f + 10f, 10f, 1f, Vector3.UnitX, false);
        textRenderer.Draw("Standard Scene", 10f, Window.Size.Y/2f + 10f, 1f, Vector3.UnitX, false);
        textRenderer.Draw("Depth Sample", Window.Size.X/2f + 10f, Window.Size.Y/2f + 10f, 1f, Vector3.UnitX, false);
        
        
        glState.LoadState();
        texture.Use();
        
        
        #region Debug UI
        shader.DisableGammaCorrection();
        
        imGui.Update((float)args.Time);
        
        if (!imGui.IsFocused()) LockMouse();;

        ImGui.Checkbox("Blur Edges", ref blurEdges);

        imGui.Render();
        
        shader.EnableGammaCorrection();
        #endregion

        Window.SwapBuffers();
    }

    protected override void Unload()
    {
        glState.Unbind();
        
        quad.Dispose();
        cube.Dispose();

        shader.Dispose();

        imGui.Dispose();
        depthMap.Dispose();
        textRenderer.Dispose();
    }
}