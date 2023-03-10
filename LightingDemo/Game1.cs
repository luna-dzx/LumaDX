using Assimp;
using LumaDX;
using ImGuiNET;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using MouseClick = OpenTK.Windowing.GraphicsLibraryFramework.MouseButton;
using Vector3 = OpenTK.Mathematics.Vector3;
using PostProcessShader = LumaDX.PostProcessing.PostProcessShader;

namespace LightingDemo;

public class Game1 : LumaDX.Game
{
    StateHandler glState;

    const string ShaderLocation = "Shaders/";
    ShaderProgram shader;
    ShaderProgram hdrShader;
    
    const string AssetLocation = "Assets/";
    Texture skyBox;
    
    Texture backpackTexture;
    Texture backpackSpecular;
    Texture backpackNormal;
    
    Texture woodTexture;
    Texture woodSpecular;
    Texture woodNormal;

    Model backpack;
    Model cube;

    FirstPersonPlayer player;

    PostProcessing postProcessor;
    
    float exposure = 1f;
    Vector3 rotation = Vector3.Zero;
    
    bool bloomEnabled;

    Objects.Light light;
    Objects.Material material;

    DrawBuffersEnum[] colourAttachments;
    DrawBuffersEnum[] brightColourAttachment;

    CubeDepthMap depthMap;


    protected override void Initialize()
    {
        glState = new StateHandler();
        glState.ClearColor = Color4.Transparent;

        colourAttachments = new [] { DrawBuffersEnum.ColorAttachment0, DrawBuffersEnum.ColorAttachment1 };
        brightColourAttachment = new [] { DrawBuffersEnum.ColorAttachment1 };
        
        UnlockMouse();

        shader = new ShaderProgram(
            ShaderLocation + "vertex.glsl",
            ShaderLocation + "fragment.glsl",
            true
        );

        player = new FirstPersonPlayer(Window.Size)
            .SetPosition(new Vector3(0f,0f,10f))
            .SetDirection(-Vector3.UnitZ);
        player.NoClip = true;
        
        player.UpdateProjection(shader);

        skyBox = Texture.LoadCubeMap(AssetLocation + "skybox/", ".jpg", 0);
        cube = new Model(PresetMesh.Cube);

        backpackTexture = new Texture(AssetLocation + "backpack/diffuse.bmp", 1);
        backpackSpecular = new Texture(AssetLocation + "backpack/specular.bmp", 2);
        backpackNormal = new Texture(AssetLocation + "backpack/normal.bmp", 3);

        woodTexture = new Texture(AssetLocation + "wood-diffuse.jpg", 1);
        woodSpecular = new Texture(AssetLocation + "wood-specular.jpg", 2);
        woodNormal = new Texture(AssetLocation + "wood-normal.jpg", 3);

        FileManager fm = new FileManager(AssetLocation + "backpack/backpack.obj")
            .AddFlag(PostProcessSteps.FlipUVs | PostProcessSteps.CalculateTangentSpace);
        backpack = fm.LoadModel();
        
        light = new Objects.Light().PointMode().SetPosition(new Vector3(-2f,2f,5f))
            .SetAmbient(0.01f).SetSpecular(Vector3.One*12f).SetDiffuse(Vector3.One*12f);
        material = PresetMaterial.Silver.SetAmbient(0.01f);

        backpackTexture.Use();
        
        shader.EnableGammaCorrection();
        
        shader.UniformMaterial("material",material,backpackTexture,backpackSpecular)
            .UniformLight("light",light)
            .UniformTexture("normalMap",backpackNormal);

        // custom shader for handling blitting
        hdrShader = new ShaderProgram(ShaderLocation+"postProcess.glsl");
        postProcessor = new PostProcessing(PostProcessShader.GaussianBlur, Window.Size, PixelInternalFormat.Rgba16f, colourAttachments)
            .UniformTextures(hdrShader, new []{"sampler", "brightSample"});;
        postProcessor.BlurTexture = 1;

        depthMap = new CubeDepthMap((2048, 2048), Vector3.Zero);
        depthMap.Position = light.Position;
        depthMap.UpdateMatrices();
        
        depthMap.UniformTexture(shader,"cubeMap", 0);
        shader.Uniform1("shadowThreshold", 0.9f);
    }

    protected override void Load()
    {
        shader.UniformTexture("cubeMap", skyBox);
        player.UpdateProjection(shader);
    }
    
    protected override void Resize(ResizeEventArgs newWin) => player.Camera.Resize(newWin.Size);
    
    
    protected override void UpdateFrame(FrameEventArgs args)
    {
        player.Update(shader, args, Window.KeyboardState, GetPlayerMousePos());
        shader.Uniform3("cameraPos", player.Camera.Position);
        player.UpdateProjection(shader);
    }

    protected override void KeyboardHandling(FrameEventArgs args, KeyboardState k)
    {
        if (k.IsKeyPressed(Keys.Enter)) { bloomEnabled = !bloomEnabled; }

        if (k.IsKeyDown(Keys.Right)) rotation+=Vector3.UnitY*(float)args.Time;
        if (k.IsKeyDown(Keys.Left))  rotation-=Vector3.UnitY*(float)args.Time;
        if (k.IsKeyDown(Keys.Up))    rotation+=Vector3.UnitX*(float)args.Time;
        if (k.IsKeyDown(Keys.Down))  rotation-=Vector3.UnitX*(float)args.Time;
    }
    
    protected override void MouseHandling(FrameEventArgs args, MouseState mouseState)
    {
        exposure += mouseState.ScrollDelta.Y * (float)args.Time;
        hdrShader.Uniform1("exposure", exposure);
    }

    int currentEffect = 0;

    protected override void RenderFrame(FrameEventArgs args)
    {
        glState.ClearColor = Color4.Transparent;
        glState.Clear();

        depthMap.DrawMode();
        
        backpack.Draw(depthMap.Shader, Vector3.Zero,  rotation, 2f);
        cube.Draw(depthMap.Shader, Vector3.Zero,  Vector3.Zero, 10f);

        depthMap.ReadMode();
        
        GL.Enable(EnableCap.CullFace);
        shader.Use();
        GL.Viewport(0,0,Window.Size.X,Window.Size.Y);


        postProcessor.StartSceneRender(colourAttachments);
        
        glState.Clear();
        
        // skyBox is maximum depth, so we want to render if it's <= instead of just <
        glState.DepthFunc = DepthFunction.Lequal;
        
        backpackTexture.Use();

        glState.DoCulling = true;
        
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.TextureCubeMap,depthMap.TextureHandle);
        depthMap.UniformClipFar(shader, "farPlane");

        backpackTexture.Use();
        backpackSpecular.Use();
        backpackNormal.Use();

        shader.SetActive("scene");
        backpack.Draw(shader, Vector3.Zero,  rotation, 2f);
        
        woodTexture.Use();
        woodSpecular.Use();
        woodNormal.Use();
        
        glState.CullFace = CullFaceMode.Front;
        shader.Uniform1("flipNormals", 1);
        cube.Draw(shader, Vector3.Zero,  Vector3.Zero, 10f);
        glState.CullFace = CullFaceMode.Back;
        shader.Uniform1("flipNormals", 0);
        

        shader.SetActive(ShaderType.FragmentShader,"light");
        cube.Draw(shader, light.Position,  Vector3.Zero, 0.2f);


        postProcessor.EndSceneRender();
        
        if (bloomEnabled) postProcessor.RenderEffect(PostProcessShader.GaussianBlur, brightColourAttachment);
        
        #region render to screen with HDR
        
        // combine scene with blurred bright component using HDR

        GL.Clear(ClearBufferMask.ColorBufferBit);


        shader.DisableGammaCorrection();
        skyBox.Use();
        glState.DoCulling = false;
        shader.SetActive("skyBox");
        cube.Draw(shader, Vector3.Zero,  Vector3.Zero, 1f);
    
        glState.DoCulling = true;
        shader.EnableGammaCorrection();

        glState.Blending = true;
        
        hdrShader.Use();
        postProcessor.DrawFbo();
        
        glState.Blending = false;
        
        #endregion




        Window.SwapBuffers();
    }

    protected override void Unload()
    {
        glState.Unbind();
        
        backpack.Dispose();
        cube.Dispose();
        
        skyBox.Dispose();
        backpackTexture.Dispose();
        
        postProcessor.Dispose();
        
        hdrShader.Dispose();
        shader.Dispose();
    }
}