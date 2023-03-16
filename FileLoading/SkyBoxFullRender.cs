using LumaDX;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;

namespace FileLoading;

public class SkyBoxFullRenderDemo: Game
{
    StateHandler glState;
    ShaderProgram shader;

    Model quad;
    Texture skyBox;

    float rotation = 0f;

    protected override void Initialize()
    {
        glState = new StateHandler();

        shader = new ShaderProgram(
            Program.ShaderLocation + "textureVertex.glsl",
            Program.ShaderLocation + "SkyBoxFullRender/fragment.glsl",
            true
        );

        quad = new Model(PresetMesh.Square);
        
        skyBox = Texture.LoadCubeMap(Program.AssetLocation + "skybox/", ".jpg", 0);
    }

    protected override void Load()
    {
        shader.Use();
        skyBox.Use();

        shader.UniformTexture("cubeMap", skyBox);
    }

    protected override void UpdateFrame(FrameEventArgs args)
    {
        rotation += (float)args.Time;

        // spin and bob up and down
        var direction = new Vector3(MathF.Sin(rotation), MathF.Sin(0.2f * rotation) * 0.3f, -MathF.Cos(rotation));
        var tangent = new Vector3(MathF.Sin(rotation + MathF.PI/4f), 0f, -MathF.Cos(rotation + MathF.PI/4f));
        
        shader.Uniform3("direction", direction);
        shader.Uniform3("tangent", tangent);
    }

    protected override void RenderFrame(FrameEventArgs args)
    {
        glState.Clear();
        
        quad.Draw();
        
        Window.SwapBuffers();
    }

    protected override void Unload()
    {
        glState.Unbind();
        
        quad.Dispose();
        skyBox.Dispose();
        shader.Dispose();
    }
}