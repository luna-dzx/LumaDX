using Assimp;
using LumaDX;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace LightingDemo;

public class Game1 : LumaDX.Game
{
    const string ShaderLocation = "Shaders/";

    ShaderProgram shader;
    ShaderProgram sceneShader;
    ShaderProgram ssaoShader;
    ShaderProgram lightingShader;

    FirstPersonPlayer player;
    Model backpack;
    Model cube;

    Objects.Light light;
    Objects.Material material;

    Texture texture;
    Texture specular;

    GeometryBuffer gBuffer;
    Vector3 rotation = Vector3.Zero;

    const int SampleCount = 64;
    const int NoiseWidth = 4;
    Vector3[] ssaoKernel;

    int noiseTexture;
    
    PostProcessing postProcessor;

    bool ambientOcclusion = true;
    
    StateHandler glState;

    protected override void Initialize()
    {
        glState = new StateHandler();
        GL.ClearColor(Color4.Black);

        shader = new ShaderProgram
        (
            ShaderLocation + "vertex.glsl",
            ShaderLocation + "fragment.glsl",
            true
        );
    
        sceneShader = new ShaderProgram()
            .LoadShader(ShaderLocation + "geometryVertex.glsl", ShaderType.VertexShader)
            .LoadShader(ShaderLocation + "geometryFragment.glsl", ShaderType.FragmentShader)
            .Compile()
            .EnableAutoProjection();
    
        player = new FirstPersonPlayer(Window.Size)
            .SetPosition(new Vector3(0,0,6));
        player.NoClip = true;
    
        const string BackpackDir = "Assets/backpack/";
        backpack = Model.FromFile(BackpackDir,"backpack.obj",out _ , postProcessFlags: PostProcessSteps.Triangulate | PostProcessSteps.FlipUVs | PostProcessSteps.CalculateTangentSpace);

        texture = new Texture(BackpackDir+"diffuse.bmp",4);
        specular = new Texture(BackpackDir+"specular.bmp",5);

        material = PresetMaterial.Silver.SetAmbient(0.01f);
        
        cube = new Model(PresetMesh.Cube.FlipNormals());
        
    
        // TODO: make resizing this better, requires re-doing all the framebuffer objects though
        gBuffer = new GeometryBuffer(Window.Size)
            .AddTexture(PixelInternalFormat.Rgb16f)  // position
            .AddTexture(PixelInternalFormat.Rgb16f)  // normal
            .AddTexture(PixelInternalFormat.Rg16f)  // texCoords
            .Construct();
    
    
        ssaoShader = new ShaderProgram(ShaderLocation+"ssaoFragment.glsl");
        lightingShader = new ShaderProgram(ShaderLocation+"lightingFragment.glsl");
        
        light = new Objects.Light().PointMode().SetPosition(3f,5f,6f);

        ssaoKernel = RandUtils.SsaoKernel(SampleCount);
        noiseTexture = TexUtils.GenSsaoNoiseTex(NoiseWidth);

        postProcessor = new PostProcessing(PostProcessing.PostProcessShader.GaussianBlur, Window.Size);
    }

    protected override void Load()
    {
        player.UpdateProjection(sceneShader);

        shader.Use();
        GL.UniformMatrix4(shader.DefaultProjection,false,ref player.Camera.ProjMatrix);

        cube.UpdateTransform(shader,new Vector3(10f,10f,10f),Vector3.Zero,0.2f);

        sceneShader.EnableGammaCorrection();

        texture.Use();

        sceneShader.Use();
        ssaoShader.UniformVec3Array("samples", ssaoKernel);
        ssaoShader.Uniform2("noiseScale", new Vector2(Window.Size.X / 4f, Window.Size.Y / 4f));

        gBuffer.UniformTextures((int)ssaoShader, new[] { "gPosition", "gNormal", "noiseTex"});
        gBuffer.UniformTextures((int)lightingShader, new[] { "gPosition", "gNormal", "gTexCoords", "ssaoTex"});

        lightingShader.UniformMaterial("material", material, texture, specular);
        lightingShader.UniformLight("light", light);
        
        lightingShader.Uniform1("ambientOcclusion", ambientOcclusion ? 1 : 0);
    }

    protected override void Resize(ResizeEventArgs newWin)
    {
        startMousePos = Window.Size / 2;
        
        player.Camera.Resize(sceneShader, newWin.Size);
            
        shader.Use();
        GL.UniformMatrix4(shader.DefaultProjection,false,ref player.Camera.ProjMatrix);
        
        gBuffer.Dispose();
        gBuffer = new GeometryBuffer(Window.Size)
            .AddTexture(PixelInternalFormat.Rgb16f)  // position
            .AddTexture(PixelInternalFormat.Rgb16f)  // normal
            .AddTexture(PixelInternalFormat.Rg16f)  // texCoords
            .Construct();

        postProcessor = new PostProcessing(PostProcessing.PostProcessShader.GaussianBlur, newWin.Size);
        
        ssaoShader.Uniform2("noiseScale", new Vector2(newWin.Size.X / 4f, newWin.Size.Y / 4f));
    }

    protected override void UpdateFrame(FrameEventArgs args)
    {
        player.Update(sceneShader, args, Window.KeyboardState, GetRelativeMouse()*2f);
        shader.Use();
        GL.UniformMatrix4(shader.DefaultView,false,ref player.Camera.ViewMatrix);
    }

    protected override void KeyboardHandling(FrameEventArgs args, KeyboardState k)
    {
        if (k.IsKeyDown(Keys.Right)) rotation+=Vector3.UnitY*(float)args.Time;
        if (k.IsKeyDown(Keys.Left))  rotation-=Vector3.UnitY*(float)args.Time;
        if (k.IsKeyDown(Keys.Up))    rotation+=Vector3.UnitX*(float)args.Time;
        if (k.IsKeyDown(Keys.Down))  rotation-=Vector3.UnitX*(float)args.Time;

        if (k.IsKeyPressed(Keys.Enter))
        {
            ambientOcclusion = !ambientOcclusion;
            lightingShader.Uniform1("ambientOcclusion", ambientOcclusion ? 1 : 0);
        }

        backpack.UpdateTransform(sceneShader, Vector3.Zero, rotation, new Vector3(0.8f));
    }


    protected override void RenderFrame(FrameEventArgs args)
    {
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        
        #region Geometry Render
        
        gBuffer.WriteMode();
        
        sceneShader.Use();
        gBuffer.SetDrawBuffers();


        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        sceneShader.SetActive(ShaderType.FragmentShader, "backpack");
        backpack.Draw(sceneShader);
   
        sceneShader.SetActive(ShaderType.FragmentShader, "cube");
        
        GL.CullFace(CullFaceMode.Front);
        cube.UpdateTransform(sceneShader,Vector3.Zero,Vector3.Zero,3f);
        cube.Draw();

        GL.CullFace(CullFaceMode.Back);


        gBuffer.ReadMode();
        
        #endregion

        
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        
        #region Colour Render
        
        postProcessor.StartSceneRender();
        #region Initial Render
        
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            ssaoShader.Use();
            ssaoShader.UniformMat4("proj", ref player.Camera.ProjMatrix);
            gBuffer.UseTexture();

            OpenGL.BindTexture(2,TextureTarget.Texture2D,noiseTexture);

            PostProcessing.Draw();

        #endregion
        
        GL.Disable(EnableCap.DepthTest);
        postProcessor.EndSceneRender();

        // blur the SSAO texture to remove noise (noise is there to prevent banding)
        postProcessor.RenderEffect(PostProcessing.PostProcessShader.GaussianBlur);

        GL.Enable(EnableCap.DepthTest);

        #region Final Render
        
            lightingShader.UniformMat4("view", ref player.Camera.ViewMatrix);
            lightingShader.Use();
            gBuffer.UseTexture();
            
            // read blurred SSAO texture into slot 3
            GL.ActiveTexture(TextureUnit.Texture3);
            postProcessor.ReadTexture(0);
            
            texture.Use();
            specular.Use();

            PostProcessing.Draw();
            
        #endregion
        
        #endregion
        
        GL.CullFace(CullFaceMode.Back);


        Window.SwapBuffers();
    }

    protected override void Unload()
    {
        GL.BindVertexArray(0);
        GL.UseProgram(0);
        
        backpack.Dispose();
        cube.Dispose();
        
        texture.Dispose();
        specular.Dispose();
        gBuffer.Dispose();
        
        shader.Dispose();
        sceneShader.Dispose();
        ssaoShader.Dispose();
    }
}