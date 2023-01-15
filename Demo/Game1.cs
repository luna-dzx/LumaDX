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
    

    protected override void Initialize()
    {
        glState = new StateHandler();
        glState.ClearColor = Color4.Teal;
        
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
        
        
        _controller = new ImGuiController(ref Window);
    }

    protected override void Load()
    {
        textRenderer = new TextRenderer(48,Window.Size);
        player.UpdateProjection(shader);
    }
    
    protected override void Resize(ResizeEventArgs newWin)
    {
        player.Camera.Resize(shader,newWin.Size);
        textRenderer.UpdateScreenSize(newWin.Size);
    }

    protected override void UpdateFrame(FrameEventArgs args)
    {
        player.Update(shader, args, Window.KeyboardState, GetRelativeMouse()*3f);
        shader.Uniform3("cameraPos", player.Position);
    }
    
    
    protected override void KeyboardHandling(FrameEventArgs args, KeyboardState keyboardState)
    {
        if (keyboardState.IsKeyDown(Keys.Right)) rotation+=Vector3.UnitY*(float)args.Time;
        if (keyboardState.IsKeyDown(Keys.Left))  rotation-=Vector3.UnitY*(float)args.Time;
        if (keyboardState.IsKeyDown(Keys.Up))    rotation+=Vector3.UnitX*(float)args.Time;
        if (keyboardState.IsKeyDown(Keys.Down))  rotation-=Vector3.UnitX*(float)args.Time;
    }


    protected override void RenderFrame(FrameEventArgs args)
    {
        
        _controller.Update(Window, (float)args.Time);
        
        glState.Clear();

        texture.Use();
        
        shader.SetActive(ShaderType.FragmentShader, "scene");
        backpack.Draw(shader,rotation:rotation,scale:1f);

        shader.SetActive(ShaderType.FragmentShader, "light");
        cube.Draw(shader);

        textRenderer.Draw("+", Window.Size.X/2f, Window.Size.Y/2f, 0.5f, new Vector3(0f));
        textRenderer.Draw("Hello World!", 10f, Window.Size.Y - 48f, 1f, new Vector3(0.5f, 0.8f, 0.2f), false);
        
        ImGui.ShowDemoWindow();
        _controller.Render();
        
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