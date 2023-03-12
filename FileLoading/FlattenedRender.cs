using Assimp;
using LumaDX;
using ImGuiNET;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using MouseClick = OpenTK.Windowing.GraphicsLibraryFramework.MouseButton;
using PrimitiveType = OpenTK.Graphics.OpenGL4.PrimitiveType;


namespace FileLoading;

public class FlattenedRender : Game
{
    StateHandler glState;
    ShaderProgram shader;

    Model model;
    float scale = 1.0f;
    Vector3 rotation;


    protected override void Initialize()
    {
        glState = new StateHandler();
        
        glState.ClearColor = Color4.Black;
        glState.DepthTest = false;
        glState.DoCulling = false;

        shader = new ShaderProgram(
            Program.ShaderLocation + "vertex.glsl",
            Program.ShaderLocation + "fragment.glsl",
            true
        );

        FileManager fm = new FileManager(Program.AssetLocation + "dingus-the-cat/source/dingus.fbx");
        model = fm.LoadModel(0);

    }


    protected override void MouseHandling(FrameEventArgs args, MouseState mouseState)
    {
        scale *= MathF.Pow(1.4f,mouseState.ScrollDelta.Y);
    }

    protected override void KeyboardHandling(FrameEventArgs args, KeyboardState k)
    {
        if (k.IsKeyDown(Keys.Right)) rotation+=Vector3.UnitY*(float)args.Time;
        if (k.IsKeyDown(Keys.Left))  rotation-=Vector3.UnitY*(float)args.Time;
        if (k.IsKeyDown(Keys.Up))    rotation+=Vector3.UnitX*(float)args.Time;
        if (k.IsKeyDown(Keys.Down))  rotation-=Vector3.UnitX*(float)args.Time;
    }


    protected override void RenderFrame(FrameEventArgs args)
    {
        glState.Clear();
        
        model.Draw(shader,Vector3.UnitY * -0.3f, rotation, scale);
        
        Window.SwapBuffers();
    }

    protected override void Unload()
    {
        glState.Unbind();
        
        model.Dispose();
        shader.Dispose();
    }
}