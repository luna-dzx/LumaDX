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
    StateHandler glState;
    TextRenderer textRenderer;

    ShaderProgram shader;

    FirstPersonPlayer player;

    Model[] scene;
    Texture[] textures;
    
    Matrix4 sceneTransform = Maths.CreateTransformation(new (0f, 0f, -5f), new (-MathF.PI/2f, 0f, 0f), 0.01f * Vector3.One, true);
    
    Objects.Light light;
    Objects.Material material;

    DepthMap depthMap;


    Portal portal1;
    Portal portal2;

    protected override void Initialize()
    {
        glState = new StateHandler();

        shader = new ShaderProgram(
            Program.ShaderLocation + "vertex.glsl", 
            Program.ShaderLocation + "fragment.glsl", 
            true);
        
        player = new FirstPersonPlayer(Window.Size)
            .SetPosition(new Vector3(-3.8289409f, -0.14746195f, -25.35519f))
            .SetDirection(new Vector3(0, 0, 1));
        player.Camera.SetFov(MathHelper.DegreesToRadians(80f));
        player.UpdateProjection(shader);

        player.Radius = new Vector3(0.2f,0.5f,0.2f);

        portal1 = new Portal(Window.Size,new Vector3(-3.869124f, -0.679837f, -22.706884f), Vector3.Zero);
        portal2 = new Portal(Window.Size,new Vector3(-15.277727f, 1.8839195f, 0.7277828f), new Vector3(0f, MathF.PI/2f, 0f));
        
        
        FileManager fm = new FileManager(Program.AssetLocation+"dust2/source/de_dust2.obj");
        scene = fm.LoadMeshes().GetModels();
        Collision.World = fm.LoadTriangles(sceneTransform, player.Radius);
        textures = fm.LoadTextures(TextureType.Diffuse, 0);
        scene.EnableTranspose().Transform(sceneTransform);
        

        // TODO: Higher Res for Videos (possibly move sample relative to player, or apply a gaussian blur to the shadow from the player's perspective)
        depthMap = new DepthMap((4096,4096),(13.811773f, 24.58587f, 9.137938f),(-0.43924624f, -0.63135237f, -0.63910633f));
        
        depthMap.ProjectOrthographic(60f,50f,3f,100f);
        depthMap.UniformMatrix(shader, "lightSpaceMatrix");
        
        depthMap.UniformTexture(shader,"depthMap",1);


        portal1.FrameBuffer.UniformTexture(shader,"sceneSample", 2);
        portal2.FrameBuffer.UniformTexture(shader,"sceneSample", 2);
        

        light = new Objects.Light().SunMode().SetAmbient(0.1f).SetDirection(depthMap.Direction);
        material = PresetMaterial.Silver.SetAmbient(0.05f);
        

        glState.DepthTest = true;
        glState.DoCulling = true;
        glState.DepthMask = true;

        shader.DisableGammaCorrection();

        
        shader.UniformMaterial("material", material, textures[0])
            .UniformLight("light", light);

        glState.Blending = true;
    }

    protected override void Load()
    {
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

        bool t1 = portal1.Teleport(portal2,player, out Vector3 pos1, out Vector3 dir1);
        bool t2 = portal2.Teleport(portal1,player, out Vector3 pos2, out Vector3 dir2);

        if (t1)
        {
            player.Position = pos1;
            player.SetDirection(dir1);
        }
        else if (t2)
        {
            player.Position = pos2;
            player.SetDirection(dir2);
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
    }

    protected override void RenderFrame(FrameEventArgs args)
    {
        shader.Uniform4("clip_plane", Vector4.Zero);
        shader.EnableGammaCorrection();
        
        #region Shadow Map
        
        // culling for better shadows
        GL.Enable(EnableCap.CullFace);
        
        depthMap.WriteMode();
        foreach (var model in scene) model.Draw(depthMap.Shader);
        
        shader.Use();
        depthMap.ReadMode();
        
        // TODO: use glState for this
        GL.Disable(EnableCap.CullFace);
        GL.Viewport(0,0,Window.Size.X,Window.Size.Y);
        
        #endregion
        

        #region Portal 2 Sample

        portal2.StartSample();
        
        shader.Uniform4("clip_plane", portal1.ClippingPlane.AsVector());
        shader.Uniform1("clip_side", Vector3.Dot(portal2.Position - player.Camera.Position, portal2.ClippingPlane.Normal) < 0 ? 1:-1);


        shader.UniformMat4("lx_View", ref portal2.ViewMatrix);
        shader.SetActive(ShaderType.FragmentShader, "scene");
        shader.Uniform3("cameraPos",portal2.RelativeCameraPos);
        
        glState.Clear();

        for (int i = 0; i < scene.Length; i++)
        {
            textures[i].Use();
            scene[i].Draw(shader);
        }
        
        shader.UniformMat4("lx_View", ref player.Camera.ViewMatrix);

        portal2.EndSample();
        #endregion
        
        
        #region Portal 1 Sample
        portal1.StartSample();
        
        shader.Uniform4("clip_plane", portal2.ClippingPlane.AsVector());
        shader.Uniform1("clip_side", Vector3.Dot(portal1.Position - player.Camera.Position, portal1.ClippingPlane.Normal) < 0 ? 1:-1);

        shader.UniformMat4("lx_View", ref portal1.ViewMatrix);
        shader.SetActive(ShaderType.FragmentShader, "scene");
        shader.Uniform3("cameraPos",portal1.RelativeCameraPos);
        
        glState.Clear();

        for (int i = 0; i < scene.Length; i++)
        {
            textures[i].Use();
            scene[i].Draw(shader);
        }
        
        shader.UniformMat4("lx_View", ref player.Camera.ViewMatrix);

        portal1.EndSample();
        #endregion
        

        shader.Uniform4("clip_plane", Vector4.Zero);
        

        shader.SetActive(ShaderType.FragmentShader, "scene");
        shader.Uniform3("cameraPos", player.Position);
        
        glState.Clear();

        for (int i = 0; i < scene.Length; i++)
        {
            textures[i].Use();
            scene[i].Draw(shader);
        }
        
        GL.Enable(EnableCap.DepthClamp);

        #region Draw Portals
        
        GL.ActiveTexture(TextureUnit.Texture0);
        textures[0].Use();
        
        GL.ActiveTexture(TextureUnit.Texture2);
        
        shader.SetActive(ShaderType.FragmentShader, "portal");
        
        portal1.FrameBuffer.UseTexture(0);
        portal1.Draw(shader);
        
        portal2.FrameBuffer.UseTexture(0);
        portal2.Draw(shader);
        
        GL.Disable(EnableCap.DepthClamp);

        
        /*
        glState.DepthTest = false;
        shader.SetActive(ShaderType.FragmentShader, "point");
        GL.PointSize(10f);
        
        portal2.Transformation.Transpose();
        
        var p0 = portal2.Transformation * new Vector4(PresetMesh.Square.Vertices[0], PresetMesh.Square.Vertices[1], PresetMesh.Square.Vertices[2],1f);
        var p1 = portal2.Transformation * new Vector4(PresetMesh.Square.Vertices[3], PresetMesh.Square.Vertices[4], PresetMesh.Square.Vertices[5],1f);
        var p2 = portal2.Transformation * new Vector4(PresetMesh.Square.Vertices[6], PresetMesh.Square.Vertices[7], PresetMesh.Square.Vertices[8],1f);
        
        portal2.Transformation.Transpose();
        
        point.Draw(shader,p0.Xyz, Vector3.Zero, 1f, renderMode: PrimitiveType.Points);
        point.Draw(shader,p1.Xyz, Vector3.Zero, 1f, renderMode: PrimitiveType.Points);
        point.Draw(shader,p2.Xyz, Vector3.Zero, 1f, renderMode: PrimitiveType.Points);
        glState.DepthTest = true;
        */

        #endregion
        
        #region UI

        glState.DepthTest = false;
        textRenderer.Draw("+", Window.Size.X/2f, Window.Size.Y/2f, 0.5f, new Vector3(0f));
        //textRenderer.Draw(""+frameRate, 10f, Window.Size.Y - 48f, 1f, new Vector3(0.5f, 0.8f, 0.2f), false);
        glState.DepthTest = true;

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
        
        depthMap.Dispose();

        portal1.Dispose();
        portal2.Dispose();
        
        textRenderer.Dispose();
    }
}