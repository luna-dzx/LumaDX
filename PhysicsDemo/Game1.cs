using LumaDX;
using ImGuiNET;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using MouseClick = OpenTK.Windowing.GraphicsLibraryFramework.MouseButton;
using Vector3 = OpenTK.Mathematics.Vector3;
using Triangle = LumaDX.Maths.Triangle;

namespace PhysicsDemo;

public class Game1 : Game
{
    StateHandler glState;

    ImGuiController imGui;

    const string ShaderLocation = "Shaders/";
    ShaderProgram shader;

    Model cube;
    Model ellipsoid;

    FirstPersonPlayer player;

    Matrix4[] cubeTransforms; // make entire scene from transformations of a cube
    float[] cubeColours;    // + colours per cube

    PhysicsPlayer physicsPlayer;

    Matrix4 worldTransform = Matrix4.Identity;
    
    float angle;
    bool updateTransform;
    

    void ResetPhysicsPlayer()
    {
        physicsPlayer.Position = new (0f, 3f, 2f);
        physicsPlayer.Gravity = -Vector3.UnitY;
    }

    void ConstructWorld()
    {
        List<Triangle> collisionTriangles = new List<Triangle>();
        
        for (int i = 0; i < 10; i++)
        {
            var transform = cubeTransforms[i] * worldTransform;
            transform.Transpose();
            var vertices = cube.GetVertices;

            for (int j = 0; j < vertices.Length/3; j+=3)
            {
                var triangle = new Triangle
                (
                    (transform * new Vector4(vertices.GetVertex(j), 1f)).Xyz,
                    (transform * new Vector4(vertices.GetVertex(j+1), 1f)).Xyz,
                    (transform * new Vector4(vertices.GetVertex(j+2), 1f)).Xyz,
                    physicsPlayer.Radius
                );
                if (triangle.Plane.Normal != Vector3.Zero) collisionTriangles.Add(triangle);
            }
        }

        Collision.World = collisionTriangles.ToArray();
        shader.UniformMat4("worldTransform", ref worldTransform);
    }
    

    protected override void Initialize()
    {
        glState = new StateHandler();
        
        glState.ClearColor = Color4.Black;

        imGui = new ImGuiController(Window);
        
        UnlockMouse();

        shader = new ShaderProgram(
            ShaderLocation + "vertex.glsl",
            ShaderLocation + "fragment.glsl",
            true
        );

        player = new FirstPersonPlayer(Window.Size)
            .SetPosition(new Vector3(0f,0f,5f))
            .SetDirection(-Vector3.UnitZ)
            .UpdateProjection(shader);
        
        cube = new Model(PresetMesh.Cube);
        ellipsoid = new Model(Maths.GenerateIcoSphere(3));

        physicsPlayer = new PhysicsPlayer(
            new(0f, 1f, 1.5f), 
            new(0.4f, 1f, 0.4f)
        );

        cubeTransforms = new Matrix4[10];

        Vector3 position = new Vector3(0f, -0.5f, 0f);
        Vector3 scale = new Vector3(1f, 0.05f, 2.5f);

        cubeTransforms[0] = Maths.CreateTransformation(position, Vector3.Zero, scale);

        position = new Vector3(0f, -0.4f, -1.5f);
        scale.Z = 1f;
        for (int i = 1; i < 10; i++)
        {
            cubeTransforms[i] = Maths.CreateTransformation(position, Vector3.Zero, scale);

            position.Y += 0.1f;
            position.Z -= 0.1f;
            scale.Z -= 0.1f;
        }

        // TODO: HSV <-> RGB Vector Conversion
        cubeColours = new float[30];
        var hsv = new Vector4(0, 1f, 1f, 1f);
        for (int i = 0; i < 10; i++)
        {
            var colour = Color4.FromHsv(hsv);
            cubeColours[i * 3] = colour.R;
            cubeColours[i * 3 +1] = colour.G;
            cubeColours[i * 3 +2] = colour.B;
            hsv.X += 0.1f;
        }

        ResetPhysicsPlayer();
        ConstructWorld();
    }

    protected override void Load()
    {
        cube.LoadMatrix(3, cubeTransforms, 4, 4, 1); // load instance transforms
        cube.LoadVector(7, cubeColours, 3, 1); // load cube colours
    }
    
    protected override void Resize(ResizeEventArgs newWin) => player.Camera.Resize(newWin.Size);
    
    
    protected override void UpdateFrame(FrameEventArgs args)
    {
        player.Update(shader, args, Window.KeyboardState, GetPlayerMousePos());
        shader.Uniform3("cameraPos", player.Camera.Position);
        player.UpdateProjection(shader);

        physicsPlayer.Velocity = -Vector3.UnitZ * (float)args.Time;
        physicsPlayer.PhysicsUpdate((float)args.Time);
        
        if (physicsPlayer.Position.Y < -4f) ResetPhysicsPlayer();
        
        if (updateTransform)
        {
            worldTransform = Matrix4.CreateRotationX(angle);
            ResetPhysicsPlayer();
            ConstructWorld();
            updateTransform = false;
        }
        
    }

    protected override void KeyboardHandling(FrameEventArgs args, KeyboardState k)
    {
        if (k.IsKeyPressed(Keys.Enter) && MouseLocked) // unlock mouse
        {
            UnlockMouse();
            imGui.FocusWindow();
        }

        if (k.IsKeyPressed(Keys.Backspace)) ResetPhysicsPlayer();
        if (k.IsKeyPressed(Keys.J)) physicsPlayer.Jump();

    }

    protected override void RenderFrame(FrameEventArgs args)
    {

        glState.Clear();

        ellipsoid.UpdateTransform(shader, physicsPlayer.Position, Vector3.Zero, physicsPlayer.Radius);

        shader.SetActive("sphere");
        shader.Uniform3("colour", Vector3.One);
        ellipsoid.Draw();
        
        glState.DepthFunc = DepthFunction.Always;
        shader.Uniform3("colour", Vector3.Zero);
        GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
        ellipsoid.Draw();
        GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
        glState.DepthFunc = DepthFunction.Less;
        
        shader.SetActive("cube");
        cube.Draw(shader,instanceCount: 10);


        #region Debug UI
        
        imGui.Update((float)args.Time);
        
        if (!imGui.IsFocused()) LockMouse();;

        //ImGui.ListBox("Effect", ref currentEffect, effectNames, effectNames.Length);
        float lastAngle = angle;
        ImGui.SliderAngle("Angle", ref angle, -180f, 180f);
        if (lastAngle != angle) updateTransform = true;
        imGui.Render();
        
        
                
        #endregion
        

        Window.SwapBuffers();
    }

    protected override void Unload()
    {
        glState.Unbind();
        
        cube.Dispose();
        ellipsoid.Dispose();
        shader.Dispose();
    }
}