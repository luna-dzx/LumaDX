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

public class PhongLightingDemo: Game
{
    ImGuiController imGui;
    
    StateHandler glState;
    ShaderProgram shader;
    
    Model cube;
    Model quad;
    
    FirstPersonPlayer player;

    Light light;

    Vector3 rotation;

    Material[] materials;
    string[] materialNames;
    int currentMaterial = 3;

    bool ambientLighting = true;
    bool diffuseLighting = false;
    bool specularLighting = false;
    
    System.Numerics.Vector3 imGuiLightColour = System.Numerics.Vector3.One;
    Vector3 lightColour = Vector3.One;

    protected override void Initialize()
    {
        glState = new StateHandler();
        
        imGui = new ImGuiController(Window);

        shader = new ShaderProgram(
            Program.ShaderLocation + "PhongLighting/vertex.glsl",
            Program.ShaderLocation + "PhongLighting/fragment.glsl",
            true
        );

        player = new FirstPersonPlayer(Window.Size)
            .SetPosition(Vector3.UnitZ * 5f)
            .SetDirection(-Vector3.UnitZ)
            .EnableNoClip();

        cube = new Model(PresetMesh.Cube);
        quad = new Model(PresetMesh.Square);
        
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
        materialNames = materials.Select(a => a.Name).ToArray();

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

        light.Diffuse = lightColour;
        light.Specular = lightColour;
        shader.UniformLight("light", light);
        shader.UniformMaterial("material", materials[currentMaterial]);

        shader.Uniform1("ambientLighting", ambientLighting ? 1 : 0);
        shader.Uniform1("diffuseLighting", diffuseLighting ? 1 : 0);
        shader.Uniform1("specularLighting", specularLighting ? 1 : 0);
    }

    

    protected override void RenderFrame(FrameEventArgs args)
    {
        glState.Clear();

        shader.SetActive(ShaderType.FragmentShader, "quad");
        quad.Draw(shader, Vector3.Zero, rotation, 1f);
        
        shader.SetActive(ShaderType.FragmentShader, "cube");
        cube.Draw(shader, light.Position, Vector3.Zero, 0.2f);

        #region Debug UI
        
        imGui.Update((float)args.Time);
        
        if (!imGui.IsFocused()) LockMouse();;

        ImGui.Checkbox("Ambient Lighting", ref ambientLighting);
        ImGui.Checkbox("Diffuse Lighting", ref diffuseLighting);
        ImGui.Checkbox("Specular Lighting", ref specularLighting);
        ImGui.ListBox("Material", ref currentMaterial, materialNames, materialNames.Length);
        ImGui.ColorPicker3("Light Colour", ref imGuiLightColour);
        lightColour = new Vector3(imGuiLightColour.X, imGuiLightColour.Y, imGuiLightColour.Z);
            
        imGui.Render();
        
        #endregion

        Window.SwapBuffers();
    }

    protected override void Unload()
    {
        glState.Unbind();
        
        imGui.Dispose();
        quad.Dispose();
        cube.Dispose();
        shader.Dispose();
    }
}