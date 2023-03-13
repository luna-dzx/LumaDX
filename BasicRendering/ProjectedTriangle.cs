using LumaDX;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;


namespace BasicRendering;

public class ProjectedTriangleDemo: Game
{
    StateHandler glState;
    ShaderProgram shader;
    
    Model triangle;
    
    FirstPersonPlayer player;

    Vector3 position;
    Vector3 rotation;
    float time = 0f;

    protected override void Initialize()
    {
        glState = new StateHandler();
        glState.DoCulling = false;

        shader = new ShaderProgram(
            Program.ShaderLocation + "ProjectedTriangle/vertex.glsl",
            Program.ShaderLocation + "ProjectedTriangle/fragment.glsl",
            true
        );

        player = new FirstPersonPlayer(Window.Size)
            .SetPosition(Vector3.UnitZ * 5f)
            .SetDirection(-Vector3.UnitZ)
            .UpdateProjection(shader)
            .EnableNoClip();

        triangle = new Model(PresetMesh.Triangle);
    }

    protected override void Resize(ResizeEventArgs newWin) => player.Camera.Resize(shader, newWin.Size);

    protected override void UpdateFrame(FrameEventArgs args)
    {
        time += (float)args.Time;
        position = Vector3.UnitZ * (-3f + MathF.Cos(time));
        rotation = Vector3.UnitY * 5f * time;
    }
    

    protected override void RenderFrame(FrameEventArgs args)
    {
        glState.Clear();

        triangle.Draw(shader, position, rotation, 1f);
        
        Window.SwapBuffers();
    }

    protected override void Unload()
    {
        glState.Unbind();
        
        triangle.Dispose();
        shader.Dispose();
    }
}