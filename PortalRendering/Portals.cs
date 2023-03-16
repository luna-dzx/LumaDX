// -------------------- Portals.cs -------------------- //

using Assimp;
using ImGuiNET;
using LumaDX;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace PortalRendering;

public class PortalDemo: Game
{
    ImGuiController imGui;

    StateHandler glState;
    TextRenderer textRenderer;

    ShaderProgram shader;

    FirstPersonPlayer player;

    Model[] scene;
    Texture[] textures;
    
    Model cube;
    Texture skyBox;
    
    Matrix4 sceneTransform = Maths.CreateTransformation(new (0f, 0f, -5f), new (-MathF.PI/2f, 0f, 0f), 0.01f * Vector3.One, true);
    
    Objects.Light light;
    Objects.Material material;

    DepthMap depthMap;

    bool renderPortal1;
    bool renderPortal2;
    bool doTeleportation;
    bool doViewAdjustment;

    Portal portal1;
    Portal portal2;

    protected override void Initialize()
    {
        glState = new StateHandler();
        glState.Blending = true;
        
        imGui = new ImGuiController(Window);

        shader = new ShaderProgram(
            Program.ShaderLocation + "vertex.glsl", 
            Program.ShaderLocation + "fragment.glsl", 
            true);
        
        player = new FirstPersonPlayer(Window.Size)
            .SetPosition(new Vector3(-3.8289409f, -0.14746195f, -25.35519f))
            .SetDirection(new Vector3(0, 0, 1));
        player.Camera.SetFov(MathHelper.DegreesToRadians(80f));

        player.Radius = new Vector3(0.2f,0.5f,0.2f);

        portal1 = new Portal(Window.Size,new Vector3(-3.869124f, -0.679837f, -22.706884f), Vector3.Zero);
        portal2 = new Portal(Window.Size,new Vector3(-15.277727f, 1.8839195f, 0.7277828f), new Vector3(0f, MathF.PI/2f, 0f));
        
        
        FileManager fm = new FileManager(Program.AssetLocation+"dust2/source/de_dust2.obj");
        scene = fm.LoadMeshes().GetModels();
        Collision.World = fm.LoadTriangles(sceneTransform, player.Radius);
        textures = fm.LoadTextures(TextureType.Diffuse, 0);
        scene.EnableTranspose().Transform(sceneTransform);
        
        depthMap = new DepthMap((4096,4096),(13.811773f, 24.58587f, 9.137938f),(-0.43924624f, -0.63135237f, -0.63910633f));

        light = new Objects.Light().SunMode().SetAmbient(0.1f).SetDirection(depthMap.Direction);
        material = PresetMaterial.Silver.SetAmbient(0.05f);

        cube = new Model(PresetMesh.Cube);
        skyBox = Texture.LoadCubeMap(Program.AssetLocation + "skybox/", ".jpg", 3);
        
        textRenderer = new TextRenderer(48,Window.Size, Program.AssetLocation+"fonts/IBMPlexSans-Regular.ttf");
    }

    protected override void Load()
    {
        player.UpdateProjection(shader);

        shader.UniformMaterial("material", material, textures[0])
            .UniformLight("light", light);
        
        depthMap.ProjectOrthographic(60f,50f,3f,100f);
        depthMap.UniformMatrix(shader, "lightSpaceMatrix");
        
        depthMap.UniformTexture(shader,"depthMap",1);
        
        portal1.FrameBuffer.UniformTexture(shader,"sceneSample", 2);
        portal2.FrameBuffer.UniformTexture(shader,"sceneSample", 2);
        
        shader.UniformTexture("skyBox", skyBox);

        LockMouse();
    }
    
    protected override void Resize(ResizeEventArgs newWin)
    {
        player.Camera.Resize(shader,newWin.Size);
        textRenderer.UpdateScreenSize(newWin.Size);
    }

    protected override void UpdateFrame(FrameEventArgs args)
    {
        player.Update(args, Window.KeyboardState, GetPlayerMousePos());

        bool t1 = portal1.Teleport(portal2,player, out Vector3 pos1, out Vector3 dir1);
        bool t2 = portal2.Teleport(portal1,player, out Vector3 pos2, out Vector3 dir2);

        if (t1)
        {
            if (doTeleportation) player.Position = pos1;
            if (doViewAdjustment) player.SetDirection(dir1);
        }
        else if (t2)
        {
            if (doTeleportation) player.Position = pos2;
            if (doViewAdjustment) player.SetDirection(dir2);
        }


        player.Camera.Position = player.Position + new Vector3(0f, 0.25f, 0f);
        player.UpdateView(shader);
        portal1.Update(player.Camera.Position, player.Camera.Direction, portal2);
        portal2.Update(player.Camera.Position, player.Camera.Direction, portal1);
    }

    protected override void KeyboardHandling(FrameEventArgs args, KeyboardState k)
    {
        if (k.IsKeyPressed(Keys.N))
        {
            player.NoClip = !player.NoClip;
        }
        
        if (k.IsKeyPressed(Keys.Enter) && MouseLocked) // unlock mouse
        {
            UnlockMouse();
            imGui.FocusWindow();
        }
    }
    
    public void PortalSample(int id) // id = 1 or 2 referring to portal1 and portal2
    {
        var (destination, source) = (id == 1) ? (portal1, portal2) : (portal2, portal1);
        GL.Viewport(0,0,destination.FrameBuffer.Size.X, destination.FrameBuffer.Size.Y);
        
        destination.StartSample();

        shader.Uniform4("clip_plane", source.ClippingPlane.AsVector());
        shader.Uniform1("clip_side", destination.GetClipSide(player.Camera.Position));

        shader.UniformMat4("lx_View", ref destination.ViewMatrix);
        shader.Uniform3("cameraPos", destination.RelativeCameraPos);

        RenderScene();

        destination.EndSample();
        GL.Viewport(0,0,Window.Size.X, Window.Size.Y);
    }

    public void RenderScene()
    {
        glState.Clear();

        for (int i = 0; i < scene.Length; i++)
        {
            textures[i].Use();
            scene[i].Draw(shader);
        }
        
        glState.SaveState();

        skyBox.Use();
        glState.DoCulling = false;
        glState.DepthFunc = DepthFunction.Lequal;
        shader.SetActive("skyBox");
        cube.Draw();

        glState.LoadState();
        shader.SetActive("scene");
    }
    

    protected override void RenderFrame(FrameEventArgs args)
    {
        shader.Uniform4("clip_plane", Vector4.Zero);
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
        

        shader.SetActive(ShaderType.FragmentShader, "scene");


        if (renderPortal1) PortalSample(1);
        if (renderPortal2) PortalSample(2);
        

        shader.Uniform4("clip_plane", Vector4.Zero); // disable clipping plane
        player.UpdateView(shader); // reset view matrix to actual camera pos
        shader.Uniform3("cameraPos", player.Position); // update camera pos

        RenderScene();

        #region Draw Portals

        shader.SetActive(ShaderType.FragmentShader, "portal");
        GL.Enable(EnableCap.DepthClamp); // render triangles no matter how close they are to the camera
        
        // bind portal samples to texture unit 2
        GL.ActiveTexture(TextureUnit.Texture2);

        if (renderPortal1)
        {
            portal1.FrameBuffer.UseTexture(0);
            portal1.Draw(shader);
        }

        if (renderPortal2)
        {
            portal2.FrameBuffer.UseTexture(0);
            portal2.Draw(shader);
        }

        GL.Disable(EnableCap.DepthClamp);
        
        #endregion
        
        #region UI

        // no point in correcting the colours of the UI as they are already as we want them
        shader.DisableGammaCorrection();
        
        // render crosshair
        glState.DepthTest = false;
        textRenderer.Draw("+", Window.Size.X/2f, Window.Size.Y/2f, 0.5f, new Vector3(0f));
        glState.DepthTest = true;

        imGui.Update((float)args.Time);
        
        if (!imGui.IsFocused()) LockMouse();;
        
        ImGui.Checkbox("Render Portal 1", ref renderPortal1);
        ImGui.Checkbox("Render Portal 2", ref renderPortal2);
        ImGui.Checkbox("Teleport", ref doTeleportation);
        ImGui.Checkbox("Adjust View", ref doViewAdjustment);

        imGui.Render();
        
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
        cube.Dispose();

        shader.Dispose();
        
        skyBox.Dispose();
        depthMap.Dispose();

        portal1.Dispose();
        portal2.Dispose();
        
        imGui.Dispose();
        
        textRenderer.Dispose();
    }
}