using ImGuiNET;
using LumaDX;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Light = LumaDX.Objects.Light;
using Material = LumaDX.Objects.Material;
using static LumaDX.PresetMaterial;

namespace DynamicLighting;

public class LightCasterDemo: Game
{
    ImGuiController imGui;
    
    StateHandler glState;
    ShaderProgram shader;
    
    Model cube;

    FirstPersonPlayer player;

    Light light;

    Vector3 direction = Vector3.UnitZ;
    Vector3 rotation = Vector3.Zero;
    
    float innerAngle = 0.09f;
    float outerAngle = 0.48f;

    Material[] materials;
    int randomSeed = 0;

    string[] lightCasters;
    int currentLightCaster = 0;
    

    protected override void Initialize()
    {
        glState = new StateHandler();
        
        imGui = new ImGuiController(Window);

        shader = new ShaderProgram(
            Program.ShaderLocation + "LightCasters/vertex.glsl",
            Program.ShaderLocation + "LightCasters/fragment.glsl",
            true
        );

        player = new FirstPersonPlayer(Window.Size)
            .SetPosition(Vector3.UnitZ * 5f)
            .SetDirection(-Vector3.UnitZ)
            .EnableNoClip();

        cube = new Model(PresetMesh.Cube);

        light = new Light()
            .SetPosition(-2f,0.5f,2f)
            .SetAmbient(0.2f, 0.2f, 0.2f);

        materials = new []
        {
            Default,
            Brass, Bronze, Silver,
            Chrome, Gold,
            Emerald, Jade, Obsidian, Pearl, Ruby, Turquoise,
            BlackPlastic, CyanPlastic, GreenPlastic, RedPlastic, WhitePlastic, YellowPlastic,
            BlackRubber, CyanRubber, GreenRubber, RedRubber, WhiteRubber, YellowRubber,
        };

        lightCasters = new[] { "Point", "SpotLight", "Sun" };

    }

    protected override void Load()
    {
        LockMouse();
    }
    
    protected override void Resize(ResizeEventArgs newWin) => player.Camera.Resize(newWin.Size);

    protected override void KeyboardHandling(FrameEventArgs args, KeyboardState k)
    {
        if (k.IsKeyDown(Keys.Right)) rotation+=Vector3.UnitY*(float)args.Time;
        if (k.IsKeyDown(Keys.Left))  rotation-=Vector3.UnitY*(float)args.Time;
        if (k.IsKeyDown(Keys.Up))    rotation+=Vector3.UnitX*(float)args.Time;
        if (k.IsKeyDown(Keys.Down))  rotation-=Vector3.UnitX*(float)args.Time;

        direction = Matrix3.CreateRotationX(rotation.X) * Matrix3.CreateRotationY(rotation.Y) * Matrix3.CreateRotationZ(rotation.Z) * Vector3.UnitZ;
        
        if (k.IsKeyPressed(Keys.Enter) && MouseLocked) // unlock mouse
        {
            UnlockMouse();
            imGui.FocusWindow();
        }

    }

    protected override void UpdateFrame(FrameEventArgs args)
    {
        player.Update(shader, args, Window.KeyboardState, GetPlayerMousePos());
        player.UpdateProjection(shader);
        shader.Uniform3("cameraPos", player.Camera.Position);

        switch (currentLightCaster)
        {
            case 0: light.PointMode(); break;
            case 1: light.SpotlightMode(innerAngle,outerAngle); break;
            case 2: light.SunMode(); break;
        }

        light.Direction = direction.Normalized();
        
        shader.UniformLight("light", light);
    }

    

    protected override void RenderFrame(FrameEventArgs args)
    {
        glState.Clear();
        
        
        shader.SetActive(ShaderType.FragmentShader, "scene");
        
        Random rand = new Random(randomSeed);
        
        for (int i = 0; i < 100; i++)
        {
            shader.UniformMaterial("material", materials[rand.Next(0,materials.Length-1)]);
            var pos = new Vector3(rand.NextSingle() - 0.5f, rand.NextSingle() - 0.5f, rand.NextSingle()) * 40f;
            var rot = new Vector3(rand.NextSingle() - 0.5f, rand.NextSingle() - 0.5f, rand.NextSingle()) * MathF.Tau;
            cube.Draw(shader, pos, rot, 1f);
        }

        switch (currentLightCaster)
        {
            case 0:
                shader.SetActive(ShaderType.FragmentShader, "light");
                cube.Draw(shader, light.Position, Vector3.Zero, 0.2f);
                break;
            case 1:
                shader.SetActive(ShaderType.FragmentShader, "light");
                cube.Draw(shader, light.Position, Vector3.Zero, 0.1f);
                cube.Draw(shader, light.Position + light.Direction * 0.2f, Vector3.Zero, 0.1f);
                break;
        }


        #region Debug UI
        
        imGui.Update((float)args.Time);
        
        if (!imGui.IsFocused()) LockMouse();;
        
        ImGui.ListBox("Light Caster", ref currentLightCaster, lightCasters, lightCasters.Length);
        ImGui.SliderFloat("Spotlight Inner Angle", ref innerAngle, 0f, MathF.PI / 2f - 0.1f);
        ImGui.SliderFloat("Spotlight Outer Angle", ref outerAngle, innerAngle, MathF.PI / 2f);

        imGui.Render();
        
        #endregion

        Window.SwapBuffers();
    }

    protected override void Unload()
    {
        glState.Unbind();
        
        cube.Dispose();
        shader.Dispose();
    }
}