using LumaDX;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;

namespace BasicRenderingDemo;

public class PrimitivesRenderDemo: Game
{
    StateHandler glState;
    ShaderProgram shader;
    
    Model cube;
    
    FirstPersonPlayer player;


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
            .SetDirection(-Vector3.UnitZ);
        player.NoClip = true;
        
        player.UpdateProjection(shader);

        cube = new Model(PresetMesh.Cube);
    }

    protected override void Load()
    {
        player.UpdateProjection(shader);

        LockMouse();
    }
    
    protected override void Resize(ResizeEventArgs newWin) => player.Camera.Resize(newWin.Size);

    protected override void UpdateFrame(FrameEventArgs args)
    {
        player.Update(shader, args, Window.KeyboardState, GetPlayerMousePos());
        shader.Uniform3("cameraPos", player.Camera.Position);
        player.UpdateProjection(shader);
    }

    

    protected override void RenderFrame(FrameEventArgs args)
    {
        glState.Clear();

        cube.Draw();

        Window.SwapBuffers();
    }

    protected override void Unload()
    {
        glState.Unbind();
        
        
        cube.Dispose();
        shader.Dispose();
    }
}