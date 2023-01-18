using LumaDX;
using Assimp;
using ImGuiNET;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Demo;

public class Game1 : Game
{
    StateHandler glState;
    TextRenderer textRenderer;
    
    const string ShaderLocation = "Shaders/";

    ShaderProgram shader;

    FirstPersonPlayer player;
    Model backpack;
    Model cube;
    
    Model[] scene;
    Texture[] textures;
    
    Vector3 scenePosition = new (0f, 0f, -5f);
    Vector3 sceneRotation = new (-MathF.PI/2f, 0f, 0f);
    
    Objects.Light light;
    Objects.Material material;

    private Vector3 rotation = Vector3.Zero;
    
    ImGuiController _controller;
    
    DepthMap depthMap;
    
    bool mouseLocked = true;
    System.Numerics.Vector3 bgCol = new (0, 0.5f, 0.5f);
    

    protected override void Initialize()
    {
        glState = new StateHandler();

        shader = new ShaderProgram(
            ShaderLocation + "vertex.glsl", 
            ShaderLocation + "fragment.glsl", 
            true);
        
        player = new FirstPersonPlayer(Window.Size)
            .SetPosition(new Vector3(-3.8289409f, -0.14746195f, -25.35519f))
            .SetDirection(new Vector3(0, 0, 1));
        player.Camera.SetFov(MathHelper.DegreesToRadians(90f));
        player.UpdateProjection(shader);

        const string BackpackDir = "Assets/backpack/";
        backpack = Model.FromFile(BackpackDir,"backpack.obj",out _ ,
            postProcessFlags: PostProcessSteps.Triangulate | PostProcessSteps.FlipUVs | PostProcessSteps.CalculateTangentSpace);
        
        
        (scene,textures) = Model.FromFile("Assets/dust2/source/","de_dust2.obj", PostProcessSteps.Triangulate| PostProcessSteps.GenerateNormals);
        
        
        depthMap = new DepthMap((4096,4096),(13.811773f, 24.58587f, 9.137938f),(-0.43924624f, -0.63135237f, -0.63910633f));
        
        depthMap.ProjectOrthographic(60f,50f,3f,100f);
        depthMap.UniformMatrix(shader, "lightSpaceMatrix");
        
        depthMap.UniformTexture("depthMap",shader,1);
        

        light = new Objects.Light().SunMode().SetAmbient(0.1f).SetDirection(depthMap.Direction);
        material = PresetMaterial.Silver.SetAmbient(0.05f);
        
        cube = new Model(PresetMesh.Cube).UpdateTransform(shader,light.Position,Vector3.Zero,0.2f);

        glState.DepthTest = true;
        glState.DoCulling = true;
        glState.DepthMask = true;

        shader.EnableGammaCorrection();


        
        shader.UniformMaterial("material", material, textures[0])
            .UniformLight("light", light);

        glState.Blending = true;
        
        
        _controller = new ImGuiController(Window);
    }

    protected override void Load()
    {
        textRenderer = new TextRenderer(48,Window.Size);
        player.UpdateProjection(shader);
        
        Window.CursorState = CursorState.Grabbed;
    }
    
    protected override void Resize(ResizeEventArgs newWin)
    {
        player.Camera.Resize(shader,newWin.Size);
        textRenderer.UpdateScreenSize(newWin.Size);
    }


    Vector2 playerMousePos = Vector2.Zero;

    protected override void UpdateFrame(FrameEventArgs args)
    {
        player.Update(shader, args, Window.KeyboardState, playerMousePos);
        shader.Uniform3("cameraPos", player.Position);
    }

    protected override void MouseMove(MouseMoveEventArgs moveInfo)
    {
        if (mouseLocked)
        {
            playerMousePos += moveInfo.Delta;
        }
    }

    bool focusWindow = false;
    bool checkFocus = false;

    protected override void KeyboardHandling(FrameEventArgs args, KeyboardState k)
    {
        if (k.IsKeyDown(Keys.Right)) rotation+=Vector3.UnitY*(float)args.Time;
        if (k.IsKeyDown(Keys.Left))  rotation-=Vector3.UnitY*(float)args.Time;
        if (k.IsKeyDown(Keys.Up))    rotation+=Vector3.UnitX*(float)args.Time;
        if (k.IsKeyDown(Keys.Down))  rotation-=Vector3.UnitX*(float)args.Time;
        
        if (k.IsKeyPressed(Keys.Enter)) // unlock mouse
        {
            if (mouseLocked)
            {
                mouseLocked = false;
                Window.CursorState = CursorState.Normal;
                focusWindow = true;
            }
        }
    }

    protected override void MouseHandling(FrameEventArgs args, MouseState mouseState)
    {
        if (mouseState.IsButtonPressed(OpenTK.Windowing.GraphicsLibraryFramework.MouseButton.Left))
        {
            checkFocus = true;
        }
    }

    protected override void RenderFrame(FrameEventArgs args)
    {
        GL.Enable(EnableCap.CullFace);
        depthMap.DrawMode();
        GL.PolygonMode(MaterialFace.FrontAndBack,PolygonMode.Fill);
        foreach (var model in scene) model.Draw(depthMap.Shader, scenePosition, sceneRotation, 1f);
        
        shader.Use();
        depthMap.ReadMode();

        //GL.CullFace(CullFaceMode.Back);
        GL.Disable(EnableCap.CullFace);
        GL.Viewport(0,0,Window.Size.X,Window.Size.Y);
        
        
        glState.ClearColor = new Color4(bgCol.X,bgCol.Y,bgCol.Z, 1f);
        glState.Clear();

        backpack.Draw(shader,Vector3.Zero,rotation,1f);
        cube.Draw(shader);

        for (int i = 0; i < scene.Length; i++)
        {
            textures[i].Use();
            scene[i].Draw(shader, scenePosition, sceneRotation, 1f);
        }

        
        
        

        textRenderer.Draw("+", Window.Size.X/2f, Window.Size.Y/2f, 0.5f, new Vector3(0f));
        textRenderer.Draw("Hello World!", 10f, Window.Size.Y - 48f, 1f, new Vector3(0.5f, 0.8f, 0.2f), false);

        glState.SaveState();
        
        _controller.Update((float)args.Time);
        if (focusWindow)
        {
            ImGui.SetWindowFocus();
            focusWindow = false;
        }

        if (checkFocus)
        {
            if (!ImGui.IsWindowFocused(ImGuiFocusedFlags.AnyWindow))
            {
                Window.CursorState = CursorState.Grabbed;
                mouseLocked = true;
            }
        }
        ImGui.ColorPicker3("BgCol", ref bgCol);
        _controller.Render();
        
        // TODO: Implement all parts of the ImGui Save/Restore into this function
        glState.LoadState();
        
        Window.SwapBuffers();
    }

    protected override void Unload()
    {
        GL.BindVertexArray(0);
        GL.UseProgram(0);

        backpack.Dispose();
        cube.Dispose();
        
        for (int i = 0; i < scene.Length; i++)
        {
            textures[i].Dispose();
            scene[i].Dispose();
        }

        shader.Dispose();
        
        depthMap.Dispose();
        
        textRenderer.Dispose();
        _controller.Dispose();
    }
}