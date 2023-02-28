using LumaDX;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;


namespace FrameBufferDemo;

public class Game1 : Game
{
    const string ShaderLocation = "Shaders/";
    const string DepthMapShaderLocation = Constants.LibraryShaderPath+"DepthMap/";
 
    //StateHandler glState;
    //ImGuiController imGui;

    ShaderProgram shader;

    FirstPersonPlayer player;
    Model cube;
    Model quad;

    Objects.Light light;
    Objects.Material material;

    DepthMap depthMap;

    Texture texture;
    
    bool visualiseDepthMap = false;
    
    private Vector3 cubePosition = new (1f, -4f, -5f);
    

    protected override void Initialize()
    {
        GL.ClearColor(0.1f, 0.1f, 0.1f, 1.0f);

        shader = new ShaderProgram(
            ShaderLocation + "vertex.glsl", 
            ShaderLocation + "fragment.glsl",
            true);

        player = new FirstPersonPlayer(Window.Size)
            .SetPosition(new Vector3(0,0,6))
            .SetDirection(new Vector3(0, 0, -1));
        player.UpdateProjection(shader);
        player.NoClip = true;

        cube = new Model(PresetMesh.Cube);
            
        quad = new Model(PresetMesh.Square).Transform(new Vector3(0f,-5f,0f), new Vector3(MathHelper.DegreesToRadians(-90f),0f,0f),10f);

        texture = new Texture("Assets/wood.png",0);
        
        depthMap = new DepthMap(DepthMapShaderLocation,(4096,4096),(-3.5f,8.5f,20f),(1f,-4f,-5f));
        
        light = new Objects.Light().SunMode().SetDirection(depthMap.Direction).SetAmbient(0.1f);
        material = PresetMaterial.Silver.SetAmbient(0.1f);

        GL.Enable(EnableCap.DepthTest);
        GL.Enable(EnableCap.CullFace);

        shader.EnableGammaCorrection();

        depthMap.ProjectOrthographic();
        depthMap.UniformMatrix(shader, "lightSpaceMatrix");

        texture.Use();
        
        shader.UniformMaterial("material",material,texture)
            .UniformLight("light",light);

        depthMap.UniformTexture("depthMap",shader,1);
    }

    protected override void Load()
    {
        LockMouse();
    }
    
    protected override void Resize(ResizeEventArgs newWin) => player.Camera.Resize(shader,newWin.Size);

    protected override void UpdateFrame(FrameEventArgs args)
    {
        player.Update(shader, args, Window.KeyboardState, GetPlayerMousePos());
        shader.Uniform3("cameraPos", player.Position);
    }

    protected override void KeyboardHandling(FrameEventArgs args, KeyboardState k)
    {
        Vector3 direction = Vector3.Zero;
        if (k.IsKeyDown(Keys.Up)) direction -= Vector3.UnitZ;
        if (k.IsKeyDown(Keys.Down)) direction += Vector3.UnitZ;
        if (k.IsKeyDown(Keys.Left)) direction -= Vector3.UnitX;
        if (k.IsKeyDown(Keys.Right)) direction += Vector3.UnitX;

        cubePosition += direction * (float)args.Time * 5f;


        if (k.IsKeyPressed(Keys.Enter))
        {
            visualiseDepthMap = !visualiseDepthMap;
            shader.Uniform1("visualiseDepthMap", visualiseDepthMap ? 1 : 0);
        }

    }

    protected override void RenderFrame(FrameEventArgs args)
    {
        depthMap.DrawMode();
        
        cube.Draw(depthMap.Shader, cubePosition, new Vector3(0f,0.2f,0f), 1f);
        cube.Draw(depthMap.Shader,new Vector3(-3f,-3f,3f), new Vector3(0.4f,0f,0f), 1f);
        
        
        shader.Use();
        depthMap.ReadMode();
        
        GL.CullFace(CullFaceMode.Back);
        GL.Viewport(0,0,Window.Size.X,Window.Size.Y);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        
        quad.Draw(shader);
        cube.Draw(shader,cubePosition, new Vector3(0f,0.2f,0f), 1f);
        cube.Draw(shader,new Vector3(-3f,-3f,3f), new Vector3(0.4f,0f,0f), 1f);

        Window.SwapBuffers();
    }

    protected override void Unload()
    {
        GL.BindVertexArray(0);
        GL.UseProgram(0);
        
        quad.Dispose();
        cube.Dispose();

        shader.Dispose();

        depthMap.Dispose();
    }
}