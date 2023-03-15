using Assimp;
using ImGuiNET;
using LumaDX;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Physics;

public class FirstPersonCollisionDemo: Game
{
    StateHandler glState;
    TextRenderer textRenderer;

    ShaderProgram shader;

    FirstPersonPlayer player;

    Model[] scene;
    Texture[] textures;
    
    Matrix4 sceneTransform = Maths.CreateTransformation(Vector3.UnitZ * -5f, Vector3.UnitX * MathF.PI/-2f, Vector3.One * 0.01f, true);
    
    Objects.Light light;
    Objects.Material material;

    DepthMap depthMap;
    
    Texture skyBox;
    Model cube;

    protected override void Initialize()
    {
        glState = new StateHandler();

        shader = new ShaderProgram(
            Program.ShaderLocation + "FirstPersonCollisions/vertex.glsl", 
            Program.ShaderLocation + "FirstPersonCollisions/fragment.glsl", 
            true);
        
        player = new FirstPersonPlayer(Window.Size)
            .SetPosition(new Vector3(-3.8289409f, -0.14746195f, -25.35519f))
            .SetDirection(new Vector3(0, 0, 1));
        player.Camera.SetFov(MathHelper.DegreesToRadians(80f));
        player.UpdateProjection(shader);

        player.Radius = new Vector3(0.2f,0.5f,0.2f);

        FileManager fm = new FileManager(Program.AssetLocation+"dust2/source/de_dust2.obj");
        scene = fm.LoadMeshes().GetModels();
        Collision.World = fm.LoadTriangles(sceneTransform, player.Radius);
        textures = fm.LoadTextures(TextureType.Diffuse, 0);
        scene.EnableTranspose().Transform(sceneTransform);
        
        depthMap = new DepthMap((4096,4096),(13.811773f, 24.58587f, 9.137938f),(-0.43924624f, -0.63135237f, -0.63910633f));
        
        depthMap.ProjectOrthographic(60f,50f,3f,100f);
        depthMap.UniformMatrix(shader, "lightSpaceMatrix");
        
        depthMap.UniformTexture(shader,"depthMap",1);
        
        light = new Objects.Light().SunMode().SetAmbient(0.1f).SetDirection(depthMap.Direction);
        material = PresetMaterial.Silver.SetAmbient(0.05f);
        

        glState.DepthTest = true;
        glState.DoCulling = true;
        glState.DepthMask = true;

        shader.DisableGammaCorrection();

        shader.UniformMaterial("material", material, textures[0])
            .UniformLight("light", light);

        glState.Blending = true;

        skyBox = Texture.LoadCubeMap(Program.AssetLocation + "skybox/", ".jpg", 2);
        cube = new Model(PresetMesh.Cube);
    }

    protected override void Load()
    {
        skyBox.Use();
        shader.Uniform1("skyBox", 2);
        
        textRenderer = new TextRenderer(48,Window.Size, Program.AssetLocation+"fonts/IBMPlexSans-Regular.ttf");
        player.UpdateProjection(shader);
        
        Window.CursorState = CursorState.Grabbed;
    }
    
    protected override void Resize(ResizeEventArgs newWin)
    {
        player.Camera.Resize(shader,newWin.Size);
        textRenderer.UpdateScreenSize(newWin.Size);
    }
    

    protected override void UpdateFrame(FrameEventArgs args)
    {
        player.Update(args, Window.KeyboardState, GetPlayerMousePos());

        player.Camera.Position = player.Position + new Vector3(0f, 0.25f, 0f);
        player.UpdateView(shader);
    }
    

    protected override void KeyboardHandling(FrameEventArgs args, KeyboardState k)
    {
        if (k.IsKeyPressed(Keys.N))
        {
            player.NoClip = !player.NoClip;
        }
    }

    protected override void RenderFrame(FrameEventArgs args)
    {
        shader.EnableGammaCorrection();
        
        #region Shadow Map
        
        // culling for better shadows
        glState.DoCulling = true;
        
        depthMap.WriteMode();
        foreach (var model in scene) model.Draw(depthMap.Shader);
        
        shader.Use();
        depthMap.ReadMode();
        
        glState.DoCulling = false;
        GL.Viewport(0,0,Window.Size.X,Window.Size.Y);
        
        #endregion

        shader.SetActive("scene");
        shader.Uniform3("cameraPos", player.Position);
        
        glState.Clear();

        for (int i = 0; i < scene.Length; i++)
        {
            textures[i].Use();
            scene[i].Draw(shader);
        }
        
        glState.DoCulling = false;
        glState.DepthFunc = DepthFunction.Lequal;
        shader.DisableGammaCorrection();
        
        skyBox.Use();
        shader.SetActive("skyBox");
        cube.Draw();
        

        #region UI

        shader.EnableGammaCorrection();
        glState.DepthFunc = DepthFunction.Always;

        // render crosshair
        textRenderer.Draw("+", Window.Size.X/2f, Window.Size.Y/2f, 0.5f, new Vector3(0f));

        glState.DoCulling = true;
        glState.DepthFunc = DepthFunction.Less;
        
        #endregion

        Window.SwapBuffers();
    }

    protected override void Unload()
    {
        glState.Unbind();
        
        for (int i = 0; i < scene.Length; i++)
        {
            textures[i].Dispose();
            scene[i].Dispose();
        }

        shader.Dispose();
        
        cube.Dispose();
        depthMap.Dispose();
        skyBox.Dispose();
        textRenderer.Dispose();
    }
}