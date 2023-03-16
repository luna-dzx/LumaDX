using LumaDX;
using OpenTK.Windowing.Common;

namespace FileLoading;

public class RenderTextureDemo: Game
{
    StateHandler glState;
    ShaderProgram shader;

    Model quad;
    Texture texture;

    protected override void Initialize()
    {
        glState = new StateHandler();

        shader = new ShaderProgram(
            Program.ShaderLocation + "textureVertex.glsl",
            Program.ShaderLocation + "RenderTexture/fragment.glsl",
            true
        );

        quad = new Model(PresetMesh.Square);
        texture = new Texture(Program.AssetLocation + "dingus-the-cat/textures/dingus_nowhiskers.jpg", 0);
    }

    protected override void Load()
    {
        shader.Use();
        texture.Use();

        shader.UniformTexture("texture0", texture);
    }

    protected override void Resize(ResizeEventArgs newWin)
    {
        float x, y; (x, y) = newWin.Size; // convert to floats
        shader.Uniform2("screenSize", x,y);
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
        texture.Dispose();
        shader.Dispose();
    }
}