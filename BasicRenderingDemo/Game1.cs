using Assimp;
using LumaDX;
using ImGuiNET;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using MouseClick = OpenTK.Windowing.GraphicsLibraryFramework.MouseButton;
using PrimitiveType = OpenTK.Graphics.OpenGL4.PrimitiveType;


namespace BasicRenderingDemo;

public class Game1 : Game
{
    StateHandler glState;

    ImGuiController imGui;

    const string ShaderLocation = "Shaders/";
    ShaderProgram shader;
    
    const string AssetLocation = "Assets/";
    Texture texture;

    Model dingus;
    
    FirstPersonPlayer player;
    
    int numTriangles;
    int maxTriangles;
    int primitiveType;
    private string[] primitiveNames;
    
    int animationDirection = 1;
    bool animating = true;
    

    protected override void Initialize()
    {
        glState = new StateHandler();
        
        glState.ClearColor = Color4.Black;

        imGui = new ImGuiController(Window);

        shader = new ShaderProgram(
            ShaderLocation + "vertex.glsl",
            ShaderLocation + "fragment.glsl",
            true
        );

        player = new FirstPersonPlayer(Window.Size)
            .SetPosition(new Vector3(4.26f, 6.65f, 10.33f))
            .SetDirection(new Vector3(-0.30f, -0.26f, -0.92f));
        player.NoClip = true;
        
        player.UpdateProjection(shader);

        texture = new Texture(AssetLocation + "/dingus-the-cat/textures/dingus_nowhiskers.jpg", 1);

        FileManager fm = new FileManager(AssetLocation + "dingus-the-cat/source/dingus.fbx");
        dingus = fm.LoadModel(0);

        maxTriangles = dingus.GetIndices.Length / 3;
        
        primitiveNames = new [] { "Points", "WireFrame", "Filled", "Coloured", "Textured" };
        glState.DoCulling = true;
    }

    protected override void Load()
    {
        shader.UniformTexture("dingus", texture);
        player.UpdateProjection(shader);

        LockMouse();
    }
    
    protected override void Resize(ResizeEventArgs newWin) => player.Camera.Resize(newWin.Size);

    protected override void UpdateFrame(FrameEventArgs args)
    {
        player.Update(shader, args, Window.KeyboardState, GetPlayerMousePos());
        shader.Uniform3("cameraPos", player.Camera.Position);
        player.UpdateProjection(shader);

        shader.Uniform1("numTriangles", numTriangles);

        if (animating)
        {
            numTriangles += animationDirection*5;
            if (numTriangles > maxTriangles) animationDirection = -1;
            if (numTriangles < 0) animationDirection = 1;
            
            numTriangles = Math.Clamp(numTriangles, 0, maxTriangles);            
        }


    }

    protected override void KeyboardHandling(FrameEventArgs args, KeyboardState k)
    {
        if (k.IsKeyPressed(Keys.Enter) && MouseLocked) // unlock mouse
        {
            UnlockMouse();
            imGui.FocusWindow();
        }

    }


    void DrawModel()
    {
        shader.SetActive("flat");
        shader.Uniform3("colour", Vector3.UnitX); // red
        glState.DepthFunc = DepthFunction.Less;
        
        dingus.UpdateTransform(shader, Vector3.Zero, Vector3.UnitX * MathF.PI * -0.5f, 0.4f);

        switch (primitiveType)
        {
            case 0: // points
                dingus.Draw(renderMode: PrimitiveType.Points);
                return;
            case 1: // lines
                
                GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);

                // draw lines at rear
                glState.DepthFunc = DepthFunction.Always;
                glState.DoCulling = false;
                dingus.Draw();
                
                
                // draw lines at front in green
                glState.DoCulling = true;
                shader.Uniform3("colour", Vector3.UnitY);
                dingus.Draw();
                
                
                GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);

                return;
            case 2: // triangles
                
                // draw red triangles
                dingus.Draw();
                
                // draw black lines
                glState.DepthFunc = DepthFunction.Lequal;
                shader.Uniform3("colour", Vector3.Zero);
                GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
                dingus.Draw();
                GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);

                return;
            case 3: // coloured
                shader.SetActive("coloured");
                dingus.Draw();
                return;
            case 4: // textured
                shader.SetActive("textured");
                dingus.Draw();
                return;
        }

        
    }
    

    protected override void RenderFrame(FrameEventArgs args)
    {
        glState.Clear();

        texture.Use();
        
        DrawModel();


        #region Debug UI
        
        imGui.Update((float)args.Time);
        
        if (!imGui.IsFocused()) LockMouse();;
        
        ImGui.ListBox("Render Mode", ref primitiveType, primitiveNames, 5);
        ImGui.Checkbox("Animating", ref animating);
        ImGui.SliderInt("Num. Triangles", ref numTriangles, 0, maxTriangles);
        
        imGui.Render();
        
        #endregion
        

        Window.SwapBuffers();
    }

    protected override void Unload()
    {
        GL.BindVertexArray(0);
        GL.UseProgram(0);
        
        dingus.Dispose();
        texture.Dispose();
        
        shader.Dispose();
    }
}