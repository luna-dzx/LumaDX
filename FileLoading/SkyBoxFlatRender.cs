// -------------------- SkyBoxFlatRender.cs -------------------- //

using LumaDX;
using OpenTK.Windowing.Common;

namespace FileLoading;

public class SkyBoxFlatRenderDemo: Game
{
    StateHandler glState;
    ShaderProgram shader;

    Model quad;
    Texture skyBox;

    protected override void Initialize()
    {
        glState = new StateHandler();

        shader = new ShaderProgram(
            Program.ShaderLocation + "textureVertex.glsl",
            Program.ShaderLocation + "SkyBoxFlatRender/fragment.glsl",
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