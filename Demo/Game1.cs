﻿using LumaDX;
using Assimp;
using ImGuiNET;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using PrimitiveType = OpenTK.Graphics.OpenGL4.PrimitiveType;

namespace Demo;

public class Game1 : Game
{
    StateHandler glState;
    TextRenderer textRenderer;
    
    const string ShaderLocation = "Shaders/";

    ShaderProgram shader;

    FirstPersonPlayer player;

    Model[] scene;
    Texture[] textures;
    
    Vector3 scenePosition = new (0f, 0f, -5f);
    Vector3 sceneRotation = new (-MathF.PI/2f, 0f, 0f);
    
    Objects.Light light;
    Objects.Material material;

    private Vector3 rotation = Vector3.Zero;
    
    ImGuiController _controller;
    
    DepthMap depthMap;

    private Model point;


    Portal portal1;
    Portal portal2;


    bool mouseLocked = true;
    System.Numerics.Vector3 bgCol = new (0, 0.5f, 0.5f);

    protected override void Initialize()
    {
        glState = new StateHandler();

        shader = new ShaderProgram(
            ShaderLocation + "vertex.glsl", 
            ShaderLocation + "fragment.glsl", 
            true);
        
        player = new FirstPersonPlayer(Window.Size)
            .SetPosition(new Vector3(-3.8289409f, -0.14746195f, -25.35519f))
            .SetDirection(new Vector3(0, 0, 1));
        //player.Camera.SetFov(MathHelper.DegreesToRadians(90f));

        player.Camera.SetDepth(0.1f,100f);
        player.UpdateProjection(shader);

        player.EllipsoidRadius = new Vector3(0.2f,0.5f,0.2f);

        portal1 = new Portal(Window.Size,new Vector3(-3.869124f, 0.1f-0.67031336f, -22.706884f), Vector3.Zero);
        portal2 = new Portal(Window.Size,new Vector3(-15.277727f, 0.1f+1.8358815f, 0.7277828f), Vector3.Zero);


        AssimpContext importer = new AssimpContext();
        importer.SetConfig(new Assimp.Configs.NormalSmoothingAngleConfig(66.0f));
        var model = importer.ImportFile("Assets/dust2/source/de_dust2.obj",
            PostProcessPreset.TargetRealTimeMaximumQuality);

        Collision.World = Maths.GetTriangles(model,scenePosition,sceneRotation,player.EllipsoidRadius);


        (scene,textures) = Model.FromFile("Assets/dust2/source/","de_dust2.obj", PostProcessSteps.Triangulate| PostProcessSteps.GenerateNormals);


        point = new Model(new float[3]);
        
        
        depthMap = new DepthMap((4096,4096),(13.811773f, 24.58587f, 9.137938f),(-0.43924624f, -0.63135237f, -0.63910633f));
        
        depthMap.ProjectOrthographic(60f,50f,3f,100f);
        depthMap.UniformMatrix(shader, "lightSpaceMatrix");
        
        depthMap.UniformTexture("depthMap",shader,1);


        portal1.FrameBuffer.UniformTexture("sceneSample", shader, 2);
        portal2.FrameBuffer.UniformTexture("sceneSample", shader, 2);
        

        light = new Objects.Light().SunMode().SetAmbient(0.1f).SetDirection(depthMap.Direction);
        material = PresetMaterial.Silver.SetAmbient(0.05f);
        

        glState.DepthTest = true;
        glState.DoCulling = true;
        glState.DepthMask = true;

        shader.DisableGammaCorrection();


        
        shader.UniformMaterial("material", material, textures[0])
            .UniformLight("light", light);

        glState.Blending = true;

        _controller = new ImGuiController(Window);
    }

    protected override void Load()
    {
        textRenderer = new TextRenderer(48,Window.Size);
        player.UpdateProjection(shader);
        
        Window.CursorState = CursorState.Grabbed;
    }
    
    protected override void Resize(ResizeEventArgs newWin)
    {
        player.Camera.Resize(shader,newWin.Size);
        textRenderer.UpdateScreenSize(newWin.Size);
    }


    Vector2 playerMousePos = Vector2.Zero;
    
    private double deltaCounter = 0.0;
    private int framesCounted = 0;

    int frameRate = 0;

    protected override void UpdateFrame(FrameEventArgs args)
    {
        player.Update(shader, args, Window.KeyboardState, playerMousePos);

        deltaCounter += args.Time;
        framesCounted++;
        if (deltaCounter > 1.0)
        {
            frameRate = framesCounted;
            deltaCounter -= 1.0;
            framesCounted = 0;
        }
        
        portal1.Update(player.Camera.Position, player.Camera.Direction, portal2);
        portal2.Update(player.Camera.Position, player.Camera.Direction, portal1);
        
        
        bool t1 = portal1.Teleport(portal2,player, out Vector3 pos1);
        bool t2 = portal2.Teleport(portal1,player, out Vector3 pos2);

        if (t1)
        {
            player.Position = pos1;
        }
        else if (t2)
        {
            player.Position = pos2;
        }
        
        Console.WriteLine(player.Position);
    }

    protected override void MouseMove(MouseMoveEventArgs moveInfo)
    {
        if (mouseLocked)
        {
            playerMousePos += moveInfo.Delta;
        }
    }

    bool focusWindow = false;
    bool checkFocus = false;

    protected override void KeyboardHandling(FrameEventArgs args, KeyboardState k)
    {
        if (k.IsKeyDown(Keys.Right)) rotation+=Vector3.UnitY*(float)args.Time;
        if (k.IsKeyDown(Keys.Left))  rotation-=Vector3.UnitY*(float)args.Time;
        if (k.IsKeyDown(Keys.Up))    rotation+=Vector3.UnitX*(float)args.Time;
        if (k.IsKeyDown(Keys.Down))  rotation-=Vector3.UnitX*(float)args.Time;
        
        if (k.IsKeyPressed(Keys.Enter)) // unlock mouse
        {
            if (mouseLocked)
            {
                mouseLocked = false;
                Window.CursorState = CursorState.Normal;
                focusWindow = true;
            }
        }
        
        if (k.IsKeyPressed(Keys.N))
        {
            player.NoClip = !player.NoClip;
        }
    }

    protected override void MouseHandling(FrameEventArgs args, MouseState mouseState)
    {
        if (mouseState.IsButtonPressed(OpenTK.Windowing.GraphicsLibraryFramework.MouseButton.Left))
        {
            checkFocus = true;
        }
    }

    protected override void RenderFrame(FrameEventArgs args)
    {
        // we shouldn't really be doing this... but it looks good!
        shader.EnableGammaCorrection();
        
        #region Shadow Map
        
        // culling for better shadows
        GL.Enable(EnableCap.CullFace);
        
        depthMap.DrawMode();
        foreach (var model in scene) model.Draw(depthMap.Shader, scenePosition, sceneRotation, 1f);
        
        shader.Use();
        depthMap.ReadMode();
        
        // TODO: use glState for this
        GL.Disable(EnableCap.CullFace);
        GL.Viewport(0,0,Window.Size.X,Window.Size.Y);
        
        #endregion
        
        //shader.DisableGammaCorrection();

        
        #region Portal 2 Sample
        portal2.StartSample();

        shader.UniformMat4("lx_View", ref portal2.ViewMatrix);
        shader.SetActive(ShaderType.FragmentShader, "scene");
        shader.Uniform3("cameraPos",portal2.RelativeCameraPos);
        
        glState.ClearColor = new Color4(bgCol.X,bgCol.Y,bgCol.Z, 1f);
        glState.Clear();

        for (int i = 0; i < scene.Length; i++)
        {
            textures[i].Use();
            scene[i].Draw(shader, scenePosition, sceneRotation, 1f);
        }
        
        shader.UniformMat4("lx_View", ref player.Camera.ViewMatrix);

        portal2.EndSample();
        #endregion
        
        
        #region Portal 1 Sample
        portal1.StartSample();

        shader.UniformMat4("lx_View", ref portal1.ViewMatrix);
        shader.SetActive(ShaderType.FragmentShader, "scene");
        shader.Uniform3("cameraPos",portal1.RelativeCameraPos);
        
        glState.ClearColor = new Color4(bgCol.X,bgCol.Y,bgCol.Z, 1f);
        glState.Clear();

        for (int i = 0; i < scene.Length; i++)
        {
            textures[i].Use();
            scene[i].Draw(shader, scenePosition, sceneRotation, 1f);
        }
        
        shader.UniformMat4("lx_View", ref player.Camera.ViewMatrix);

        portal1.EndSample();
        #endregion
        


        shader.SetActive(ShaderType.FragmentShader, "scene");
        shader.Uniform3("cameraPos", player.Position);
        
        
        glState.ClearColor = new Color4(bgCol.X,bgCol.Y,bgCol.Z, 1f);
        glState.Clear();

        for (int i = 0; i < scene.Length; i++)
        {
            textures[i].Use();
            scene[i].Draw(shader, scenePosition, sceneRotation, 1f);
        }

        #region Draw Portals
        
        GL.ActiveTexture(TextureUnit.Texture0);
        textures[0].Use();
        
        GL.ActiveTexture(TextureUnit.Texture2);

        shader.SetActive(ShaderType.FragmentShader, "portal");
        
        portal1.FrameBuffer.UseTexture(0);
        portal1.Draw(shader);
        
        portal2.FrameBuffer.UseTexture(0);
        portal2.Draw(shader);
        
        

        
        /*
        glState.DepthTest = false;
        shader.SetActive(ShaderType.FragmentShader, "point");
        GL.PointSize(10f);
        point.Draw(shader,_triangle0.Point0, Vector3.Zero, 1f, renderMode: PrimitiveType.Points);
        point.Draw(shader,_triangle0.Point1, Vector3.Zero, 1f, renderMode: PrimitiveType.Points);
        point.Draw(shader,_triangle0.Point2, Vector3.Zero, 1f, renderMode: PrimitiveType.Points);
        
        point.Draw(shader,_triangle1.Point0, Vector3.Zero, 1f, renderMode: PrimitiveType.Points);
        point.Draw(shader,_triangle1.Point1, Vector3.Zero, 1f, renderMode: PrimitiveType.Points);
        point.Draw(shader,_triangle1.Point2, Vector3.Zero, 1f, renderMode: PrimitiveType.Points);
        glState.DepthTest = true;
        */

        #endregion
        
        #region UI

        glState.DepthTest = false;
        textRenderer.Draw("+", Window.Size.X/2f, Window.Size.Y/2f, 0.5f, new Vector3(0f));
        //textRenderer.Draw(""+frameRate, 10f, Window.Size.Y - 48f, 1f, new Vector3(0.5f, 0.8f, 0.2f), false);
        glState.DepthTest = true;
        
        glState.SaveState();
        
        _controller.Update((float)args.Time);
        if (focusWindow)
        {
            ImGui.SetWindowFocus();
            focusWindow = false;
        }

        if (checkFocus)
        {
            if (!ImGui.IsWindowFocused(ImGuiFocusedFlags.AnyWindow))
            {
                Window.CursorState = CursorState.Grabbed;
                mouseLocked = true;
            }
        }
        ImGui.ColorPicker3("BgCol", ref bgCol);
        _controller.Render();
        
        // TODO: Implement all parts of the ImGui Save/Restore into this function
        glState.LoadState();
        
        #endregion

        Window.SwapBuffers();
    }

    protected override void Unload()
    {
        GL.BindVertexArray(0);
        GL.UseProgram(0);

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
        _controller.Dispose();
    }
}