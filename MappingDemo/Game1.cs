using LumaDX;
using ImGuiNET;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using MouseClick = OpenTK.Windowing.GraphicsLibraryFramework.MouseButton;
using Light = LumaDX.Objects.Light;
using Material = LumaDX.Objects.Material;

namespace MappingDemo;

public class Game1 : Game
{
    StateHandler glState;

    ShaderProgram shader;
    Model quad;

    Light light;
    Material material;

    Texture diffuse;
    Texture specular;
    Texture normal;
    
    const string ShaderLocation = "Shaders/";
    const string AssetsLocation = "Assets/";

    Matrix4 projMatrix;
    Matrix4 viewMatrix;

    Vector3 rotation = new (-0.383f,  0.483f, 0f);

    void UpdateProjection(Vector2i screenSize)
    {
        projMatrix = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(75f), screenSize.X/(float)screenSize.Y, 0.05f, 50f);
        shader.UniformMat4("lx_Proj", ref projMatrix);
    }
    
    void UpdateView(Vector3 direction)
    {
        viewMatrix = Matrix4.LookAt(Vector3.Zero, direction, Vector3.UnitY);
        shader.UniformMat4("lx_View", ref viewMatrix);
    }
    

    protected override void Initialize()
    {
        glState = new StateHandler();
        glState.ClearColor = Color4.Black;

        shader = new ShaderProgram(ShaderLocation + "vertex.glsl",
            ShaderLocation + "fragment.glsl",
            true
        );

        quad = new Model(PresetMesh.Square);

        light = new Light()
            .SetPosition(-3f,1f,0f)
            .SetAmbient(0.2f, 0.2f, 0.2f);
        material = PresetMaterial.Default;

        diffuse = new Texture(AssetsLocation+"diffuse.bmp", 0);
        specular = new Texture(AssetsLocation+"specular.bmp", 1);
        normal = new Texture(AssetsLocation+"normal.bmp", 2);
    }

    protected override void Load()
    {
        UpdateProjection(Window.Size);
        UpdateView(Vector3.UnitZ);
        shader.UniformLight("light", light);
        shader.UniformMaterial("material", material, diffuse, specular);
        shader.UniformTexture("normalMap", normal);
        shader.EnableGammaCorrection();;
    }

    protected override void Resize(ResizeEventArgs newWin) => UpdateProjection(newWin.Size);

    protected override void UpdateFrame(FrameEventArgs args)
    {
        
    }

    protected override void KeyboardHandling(FrameEventArgs args, KeyboardState k)
    {
        if (k.IsKeyDown(Keys.Up)) rotation += Vector3.UnitX * (float)args.Time;
        if (k.IsKeyDown(Keys.Down)) rotation -= Vector3.UnitX * (float)args.Time;
        if (k.IsKeyDown(Keys.Right)) rotation += Vector3.UnitY * (float)args.Time;
        if (k.IsKeyDown(Keys.Left)) rotation -= Vector3.UnitY * (float)args.Time;
    }

    protected override void RenderFrame(FrameEventArgs args)
    {
        glState.Clear();
        // draw both sides of square separately so each side can have a different normal for lighting calculations
        quad.Draw(shader, 4f * Vector3.UnitZ, rotation, 3f);
        quad.Draw(shader, 4f * Vector3.UnitZ, rotation + MathF.PI * Vector3.UnitX, 3f);
        Window.SwapBuffers();
    }

    protected override void Unload()
    {
        glState.Unbind();
        
        quad.Dispose();
        shader.Dispose();
    }
}