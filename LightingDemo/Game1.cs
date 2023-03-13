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
    ShaderProgram aoShader;
    
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
    DrawBuffersEnum[] aoColourAttachment;

    CubeDepthMap depthMap;
    
    
    const int SampleCount = 64;
    const int NoiseWidth = 4;
    Vector3[] ssaoKernel;
    int noiseTexture;

    bool ambientOcclusion = false;
    bool visualizeAO = false;


    protected override void Initialize()
    {
        glState = new StateHandler();
        glState.ClearColor = Color4.Transparent;

        colourAttachments = OpenGL.GetDrawBuffers(4);
        brightColourAttachment = OpenGL.GetDrawBuffers(1,1);
        aoColourAttachment = OpenGL.GetDrawBuffers(1,2);
        
        UnlockMouse();

        shader = new ShaderProgram(
            ShaderLocation + "vertex.glsl",
            ShaderLocation + "fragment.glsl",
            true
        );

        player = new FirstPersonPlayer(Window.Size)
            .SetPosition(new Vector3(0f,0f,10f))
            .SetDirection(-Vector3.UnitZ)
            .EnableNoClip();

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

        aoShader = new ShaderProgram(ShaderLocation + "ambientOcclusion.glsl");
        aoShader.Uniform1("samplePosition", 2);
        aoShader.Uniform1("sampleNormal", 3);
        aoShader.Uniform1("noiseTex", 4);
        
        ssaoKernel = RandUtils.SsaoKernel(SampleCount);
        noiseTexture = TexUtils.GenSsaoNoiseTex(NoiseWidth);
        
        aoShader.UniformVec3Array("samples", ssaoKernel);
        aoShader.Uniform2("noiseScale", new Vector2(Window.Size.X / 4f, Window.Size.Y / 4f));

        

        // custom shader for handling blitting
        hdrShader = new ShaderProgram(ShaderLocation+"postProcess.glsl");


        postProcessor = new PostProcessing(PostProcessShader.GaussianBlur, Window.Size, PixelInternalFormat.Rgba16f,
                colourAttachments)
            .UniformTextures(hdrShader, new[] { "sampler", "brightSample", "occlusionSample" });

        depthMap = new CubeDepthMap((2048, 2048), Vector3.Zero);
        depthMap.Position = light.Position;
        depthMap.UpdateMatrices();
        
        depthMap.UniformTexture(shader,"cubeMap", 0);
        shader.Uniform1("shadowThreshold", 0.9f);
    }

    protected override void Load()
    {
        shader.UniformTexture("cubeMap", skyBox);
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

        if (k.IsKeyPressed(Keys.Backspace)) ambientOcclusion = !ambientOcclusion;
        if (k.IsKeyPressed(Keys.RightBracket)) visualizeAO = !visualizeAO;
            

        if (k.IsKeyDown(Keys.Right)) rotation+=Vector3.UnitY*(float)args.Time;
        if (k.IsKeyDown(Keys.Left))  rotation-=Vector3.UnitY*(float)args.Time;
        if (k.IsKeyDown(Keys.Up))    rotation+=Vector3.UnitX*(float)args.Time;
        if (k.IsKeyDown(Keys.Down))  rotation-=Vector3.UnitX*(float)args.Time;
        
        
        if (k.IsKeyDown(Keys.L)) light.Position+=Vector3.UnitY*(float)args.Time;
        if (k.IsKeyDown(Keys.J))  light.Position-=Vector3.UnitY*(float)args.Time;
        if (k.IsKeyDown(Keys.I))    light.Position+=Vector3.UnitX*(float)args.Time;
        if (k.IsKeyDown(Keys.K))  light.Position-=Vector3.UnitX*(float)args.Time;

        shader.UniformLight("light", light);
        depthMap.Position = light.Position;
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
        
        glState.DoCulling = true;
        shader.Use();
        GL.Viewport(0,0,Window.Size.X,Window.Size.Y);


        postProcessor.StartSceneRender(colourAttachments);
        
        glState.Clear();
        
        // skyBox is maximum depth, so we want to render if it's <= instead of just <
        glState.DepthFunc = DepthFunction.Lequal;
        
        backpackTexture.Use();

        depthMap.UseTexture();
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
        
        postProcessor.BlurTexture = 1;
        if (bloomEnabled) postProcessor.RenderEffect(PostProcessShader.GaussianBlur, brightColourAttachment);

        
        if (ambientOcclusion || visualizeAO)
        {

            postProcessor.ReadFbo.ReadMode();
            aoShader.Use();
            OpenGL.BindTexture(4,TextureTarget.Texture2D,noiseTexture);
            aoShader.UniformMat4("proj", ref player.Camera.ProjMatrix);

            postProcessor.WriteFbo.SetDrawBuffers(aoColourAttachment);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer,(int)postProcessor.WriteFbo);
            postProcessor.ReadFbo.UseTexture();

            PostProcessing.Draw();
            
            postProcessor.EndSceneRender();
            
            postProcessor.BlurTexture = 2;
            postProcessor.RenderEffect(PostProcessShader.GaussianBlur, aoColourAttachment);
        }
        
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
        hdrShader.Uniform1("aoEnabled",ambientOcclusion?1:0);
        hdrShader.Uniform1("visualizeAO",visualizeAO?1:0);

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