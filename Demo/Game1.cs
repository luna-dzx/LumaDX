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

    Objects.Light light;
    Objects.Material material;

    Texture texture;
    Texture normalMap;
    Texture specular;

    private Vector3 rotation = Vector3.Zero;
    
    ImGuiController _controller;
    
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
            .SetPosition(new Vector3(0,0,6))
            .SetDirection(new Vector3(0, 0, 1));
        
        const string BackpackDir = "Assets/backpack/";
        backpack = Model.FromFile(BackpackDir,"backpack.obj",out _ ,
            postProcessFlags: PostProcessSteps.Triangulate | PostProcessSteps.FlipUVs | PostProcessSteps.CalculateTangentSpace);

        texture = new Texture(BackpackDir+"diffuse.bmp",0);
        specular = new Texture(BackpackDir+"specular.bmp",1);
        normalMap = new Texture(BackpackDir+"normal.bmp",2);
        
        light = new Objects.Light().PointMode().SetPosition(new Vector3(-2f,2f,5f)).SetAmbient(0.1f);
        material = PresetMaterial.Silver.SetAmbient(0.01f);
        
        cube = new Model(PresetMesh.Cube)
            .UpdateTransform(shader,light.Position,Vector3.Zero,0.2f);

        glState.DepthTest = true;
        glState.DoCulling = true;
        glState.DepthMask = true;

        shader.EnableGammaCorrection();


        
        shader.UniformMaterial("material",material,texture,specular)
            .UniformLight("light",light)
            .UniformTexture("normalMap",normalMap);


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
        glState.ClearColor = new Color4(bgCol.X,bgCol.Y,bgCol.Z, 1f);
        glState.Clear();

        texture.Use();
        
        shader.SetActive(ShaderType.FragmentShader, "scene");
        backpack.Draw(shader,rotation:rotation,scale:1f);

        shader.SetActive(ShaderType.FragmentShader, "light");
        cube.Draw(shader);

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

        shader.Dispose();
        
        textRenderer.Dispose();
        _controller.Dispose();
    }
}