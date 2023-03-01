using System.Drawing;
using LumaDX;
using ImGuiNET;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using MouseClick = OpenTK.Windowing.GraphicsLibraryFramework.MouseButton;
using Light = LumaDX.Objects.Light;
using Material = LumaDX.Objects.Material;
using SysVec3 = System.Numerics.Vector3;

namespace MappingDemo;

public class Game1 : Game
{
    StateHandler glState;

    ShaderProgram shader;
    Model quad;

    Light light;
    Material material;

    Texture texture;

    FrameBuffer[] frameBuffers; // stores the diffuse, specular and normal maps
    
    ImGuiController imGui;
    
    SysVec3 brushColour = SysVec3.UnitX;
    
    const string ShaderLocation = "Shaders/";
    const string AssetsLocation = "Assets/";

    Matrix4 projMatrix;
    Matrix4 viewMatrix;

    Vector3 rotation = new (-0.383f,  -0.483f, MathF.PI);
    
    float heightScale = 0.0f;
    bool normalMap = false;

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
        
        imGui = new ImGuiController(Window);

        shader = new ShaderProgram(ShaderLocation + "vertex.glsl",
            ShaderLocation + "fragment.glsl",
            true
        );

        quad = new Model(PresetMesh.Square);

        light = new Light()
            .SetPosition(-3f,1f,0f)
            .SetAmbient(0.2f, 0.2f, 0.2f);
        material = PresetMaterial.Default;

        frameBuffers = new FrameBuffer[4];

        GL.ActiveTexture(TextureUnit.Texture0);
        frameBuffers[0] = new FrameBuffer(AssetsLocation+"diffuse.bmp").UseTexture(0);
        GL.ActiveTexture(TextureUnit.Texture1);
        frameBuffers[1] = new FrameBuffer(AssetsLocation+"specular.bmp").UseTexture(0);
        GL.ActiveTexture(TextureUnit.Texture2);
        frameBuffers[2] = new FrameBuffer(AssetsLocation+"normal.bmp").UseTexture(0);
        GL.ActiveTexture(TextureUnit.Texture3);
        frameBuffers[3] = new FrameBuffer(AssetsLocation+"bricks.bmp").UseTexture(0);

        UnlockMouse();
    }

    protected override void Load()
    {
        UpdateProjection(Window.Size);
        UpdateView(Vector3.UnitZ);
        shader.UniformLight("light", light)
            .UniformMaterial("material", material)
            .UniformFrameBuffer("material.baseTex", 0)
            .UniformFrameBuffer("material.specTex", 1)
            .UniformFrameBuffer("normalMap", 2)
            .UniformFrameBuffer("displaceMap", 3)
            .EnableGammaCorrection();
    }

    protected override void Resize(ResizeEventArgs newWin) => UpdateProjection(newWin.Size);

    protected override void UpdateFrame(FrameEventArgs args)
    {
        shader.Uniform1("heightScale", heightScale);
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
        // squares on right hand side
        Vector2 screenSize = (Window.Size.X,Window.Size.Y); // (convert to floats/Vector2)
        Vector2 squareSize = (Window.Size.Y / 4f - 6f, Window.Size.Y / 4f - 6f);
        shader.Uniform2("squareSize", squareSize);
        shader.Uniform2("screenSize", screenSize);
        shader.Uniform1("doNormalMapping", normalMap?1:0);
        float x = screenSize.X - squareSize.X - 10f;
        
        /*#region FrameBuffers
        
        shader.SetActive("frameBuffer");
        for (int i = 0; i < 3; i++)
        {
            Vector2 textureSize = (frameBuffers[i].Size.X, frameBuffers[i].Size.Y);
            GL.Viewport(0,0,(int)textureSize.X,(int)textureSize.Y);
            frameBuffers[i].WriteMode();
            float y = screenSize.Y - squareSize.Y - 10f - (squareSize.Y*2f + 10f) * i;
            shader.Uniform2("squarePos", x, y);
            shader.Uniform2("mousePos", new Vector2(Window.MousePosition.X - x, screenSize.Y - Window.MousePosition.Y - y/2f) / squareSize);
            shader.Uniform2("textureSize", textureSize);
            quad.Draw();
            frameBuffers[i].ReadMode();
        }
        GL.Viewport(0,0,(int)screenSize.X,(int)screenSize.Y);
        #endregion*/
        
        
        
        glState.SaveState();
        glState.Clear();

        shader.SetActive("mapping");
        shader.EnableGammaCorrection();
        // draw both sides of square separately so each side can have a different normal for lighting calculations
        quad.Draw(shader, 4f * Vector3.UnitZ, rotation, 3f);
        quad.Draw(shader, 4f * Vector3.UnitZ, rotation + MathF.PI * Vector3.UnitX, 3f);

        shader.SetActive("2d");
        glState.DepthTest = false;
        
        // maybe or maybe don't do this? not sure
        //shader.DisableGammaCorrection();
        
        for (int i = 0; i < 4; i++)
        {
            shader.Uniform1("visualise", i);
            float y = screenSize.Y - squareSize.Y - 10f - (squareSize.Y*2f + 10f) * i;
            shader.Uniform2("squarePos", x, y);
            quad.Draw();
        }

        glState.LoadState();
        
        #region Debug UI
        
        imGui.Update((float)args.Time);

        ImGui.Button("Normalize");
        if (ImGui.IsItemActive()) brushColour = SysVec3.Normalize(brushColour);
        
        ImGui.ColorPicker3("Brush Colour", ref brushColour);
        ImGui.SliderFloat("Height Displacement", ref heightScale, 0f, 0.2f);

        ImGui.Checkbox("Normal Map", ref normalMap);
        imGui.Render();
        #endregion
        
        Window.SwapBuffers();
    }

    protected override void Unload()
    {
        glState.Unbind();
        
        quad.Dispose();
        shader.Dispose();
    }
}