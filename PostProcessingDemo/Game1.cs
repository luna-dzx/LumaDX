﻿using System;
using System.Numerics;
using LumaDX;
using Assimp;
using ImGuiNET;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using MouseClick = OpenTK.Windowing.GraphicsLibraryFramework.MouseButton;
using Vector3 = OpenTK.Mathematics.Vector3;


namespace PostProcessingDemo;

public class Game1 : Game
{
    StateHandler glState;

    ImGuiController imGui;

    const string ShaderLocation = "Shaders/";
    ShaderProgram shader;
    
    const string AssetLocation = "Assets/";
    Texture skyBox;
    Texture texture;

    Model dingus;
    Model cube;

    FirstPersonPlayer player;

    protected override void Initialize()
    {
        glState = new StateHandler();
        
        glState.ClearColor = Color4.Black;

        imGui = new ImGuiController(Window);
        
        LockMouse();

        shader = new ShaderProgram(
            ShaderLocation + "vertex.glsl",
            ShaderLocation + "fragment.glsl",
            true
        );

        player = new FirstPersonPlayer(Window.Size)
            .SetPosition(new Vector3(0f,0f,5f))
            .SetDirection(-Vector3.UnitZ);
        player.NoClip = true;
        
        player.UpdateProjection(shader);

        skyBox = Texture.LoadCubeMap(AssetLocation + "skybox/", ".jpg", 0);
        cube = new Model(PresetMesh.Cube);

        texture = new Texture(AssetLocation + "/dingus-the-cat/textures/dingus_nowhiskers.jpg", 1, flipOnLoad: false);
        dingus = Model.FromFile(AssetLocation + "dingus-the-cat/source/", "dingus.fbx", out _);
    }

    protected override void Load()
    {
        shader.UniformTexture("skyBox", skyBox);
        shader.UniformTexture("dingus", texture);
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
        if (k.IsKeyPressed(Keys.Enter) && MouseLocked) // unlock mouse
        {
            UnlockMouse();
            imGui.FocusWindow();
        }
    }

    protected override void RenderFrame(FrameEventArgs args)
    {
        glState.Clear();
        
        // skyBox is maximum depth, so we want to render if it's <= instead of just <
        glState.DepthFunc = DepthFunction.Lequal;

        skyBox.Use();
        texture.Use();

        glState.DoCulling = true;
        shader.SetActive("scene");
        dingus.Draw(shader, new Vector3(0f,0f,-10f), new Vector3(-MathF.PI/2f,0f,0f), 0.2f);
        
        
        glState.DoCulling = false;
        shader.SetActive("skyBox");
        cube.Draw();
        
        glState.DoCulling = true;
        


        #region Debug UI
        
        imGui.Update((float)args.Time);
        
        if (!imGui.IsFocused()) LockMouse();;
        
        ImGui.Image(new IntPtr(texture.GetHandle()), new System.Numerics.Vector2(100f,100f));
        imGui.Render();
        
        #endregion
        

        Window.SwapBuffers();
    }

    protected override void Unload()
    {
        GL.BindVertexArray(0);
        GL.UseProgram(0);
        
        dingus.Dispose();
        cube.Dispose();
        
        skyBox.Dispose();
        texture.Dispose();
        
        shader.Dispose();
    }
}