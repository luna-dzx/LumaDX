using LumaDX;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;


namespace FileLoading;

public class FlattenedRenderDemo : Game
{
    StateHandler glState;
    ShaderProgram shader;

    Model model;
    
    float scale = 1.0f;
    Vector3 rotation;

    protected override void Initialize()
    {
        glState = new StateHandler();
        glState.DepthTest = false;
        glState.DoCulling = false;

        // load shaders
        shader = new ShaderProgram(
            Program.ShaderLocation + "FlattenedRender/vertex.glsl",
            Program.ShaderLocation + "FlattenedRender/fragment.glsl",
            true
        );
        
        // load 3D model
        FileManager fm = new FileManager(Program.AssetLocation + "dingus-the-cat/source/dingus.fbx");
        model = fm.LoadModel(0);
    }


    protected override void MouseHandling(FrameEventArgs args, MouseState mouseState)
    {
        // scale by exponential so we never reach 0 scale
        scale *= MathF.Pow(1.4f,mouseState.ScrollDelta.Y);
    }

    protected override void KeyboardHandling(FrameEventArgs args, KeyboardState k)
    {
        // rotation by keyboard inputs
        if (k.IsKeyDown(Keys.Right)) rotation+=Vector3.UnitY*(float)args.Time;
        if (k.IsKeyDown(Keys.Left))  rotation-=Vector3.UnitY*(float)args.Time;
        if (k.IsKeyDown(Keys.Up))    rotation+=Vector3.UnitX*(float)args.Time;
        if (k.IsKeyDown(Keys.Down))  rotation-=Vector3.UnitX*(float)args.Time;
    }


    protected override void RenderFrame(FrameEventArgs args)
    {
        glState.Clear();
        
        // render at (0,-0.3,0) since this 3D model's centre is at the bottom of the model
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