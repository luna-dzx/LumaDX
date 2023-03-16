// -------------------- PrimitivesRender.cs -------------------- //

using LumaDX;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;

namespace BasicRendering;

public class PrimitivesRenderDemo: Game
{
    StateHandler glState;
    ShaderProgram shader;
    
    Model cube;
    Model quad;
    
    FirstPersonPlayer player;

    Vector3 rotation;
    float angle = 0f;

    protected override void Initialize()
    {
        glState = new StateHandler();

        shader = new ShaderProgram(
            Program.ShaderLocation + "PrimitivesRender/vertex.glsl",
            Program.ShaderLocation + "PrimitivesRender/fragment.glsl",
            true
        );

        player = new FirstPersonPlayer(Window.Size)
            .SetPosition(Vector3.UnitZ * 5f)
            .SetDirection(-Vector3.UnitZ)
            .EnableNoClip();

        cube = new Model(PresetMesh.Cube);
        quad = new Model(PresetMesh.Square);
    }

    protected override void Load()
    {
        LockMouse();
    }
    
    protected override void Resize(ResizeEventArgs newWin) => player.Camera.Resize(newWin.Size);

    protected override void UpdateFrame(FrameEventArgs args)
    {
        player.Update(shader, args, Window.KeyboardState, GetPlayerMousePos());
        player.UpdateProjection(shader);

        angle += (float)args.Time;
        rotation = new Vector3(angle*0.2f, angle, angle*0.7f);
    }

    

    protected override void RenderFrame(FrameEventArgs args)
    {
        glState.Clear();

        shader.SetActive(ShaderType.FragmentShader, "quad");
        quad.Draw(shader, Vector3.UnitY * -2f, Vector3.UnitX * MathF.PI / -2f, 3f);
        
        shader.SetActive(ShaderType.FragmentShader, "cube");
        cube.Draw(shader, Vector3.Zero, rotation, 1f);

        Window.SwapBuffers();
    }

    protected override void Unload()
    {
        glState.Unbind();
        
        quad.Dispose();
        cube.Dispose();
        shader.Dispose();
    }
}