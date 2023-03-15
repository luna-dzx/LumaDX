using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace DynamicLighting;

internal static class Program
{
    public const string ShaderLocation = "Shaders/";
    public const string AssetLocation = "Assets/";


    #region Window Settings
    
    private const int FPS = 60;

    static GameWindowSettings gameSettings = new()
    {
        RenderFrequency = FPS,
        UpdateFrequency = FPS
    };
    
    static NativeWindowSettings uiSettings = new()
    {
        APIVersion = Version.Parse("4.1.0"),
        Size = new Vector2i(1600,900),
        NumberOfSamples = 4,

        WindowState = WindowState.Normal,
        WindowBorder = WindowBorder.Resizable,
        IsEventDriven = false,
        StartFocused = true
    };
    
    #endregion


    /// <param name="args">'a'-'t' for which demo to run</param>
    public static void Main(string[] args)
    {
        if (args.Length < 1)
        {
            PhongLighting();
            LightCasters();
            TextureMapping();
            BasicShadows();
            ComplexLighting();

            return;
        }

        switch (args[0][0])
        {
            case <= 'e':
                PhongLighting(); return;
            case 'f':
                LightCasters(); return;
            case >= 'g' and <= 'j':
                TextureMapping(); return;
            case >= 'k' and <= 'm':
                BasicShadows(); return;
            default: // n-t
                ComplexLighting(); return;
        }
        
    }

    
    /// <summary>
    /// Demos 4.a to 4.e
    /// Render Quad with Ambient Lighting
    /// Render Quad with Diffuse Lighting
    /// Render Quad with Specular Highlights
    /// Change Light Colour
    /// Alter Material of Quad
    /// </summary>
    public static void PhongLighting()
    {
        uiSettings.Title = "Dynamic Real-Time Lighting/Shadows - Demos 4.a to 4.e";
        
        using var game = new PhongLightingDemo();
        game.InitWindow(gameSettings, uiSettings);
        game.Run();
    }
    
    /// <summary>
    /// Demo 4.f
    /// Render Scene of Cubes with Different Light Casting Modes
    /// </summary>
    public static void LightCasters()
    {
        uiSettings.Title = "Dynamic Real-Time Lighting/Shadows - Demo 4.f";
        
        using var game = new LightCasterDemo();
        game.InitWindow(gameSettings, uiSettings);
        game.Run();
    }
    
    /// <summary>
    /// Demos 4.g to 4.j
    /// Render Lit, Textured Quad with a Specular Lighting Map
    /// Render Quad with a Normal Map
    /// Render Quad with a Displacement Map
    /// Draw On Texture Maps Using FrameBuffers
    /// </summary>
    public static void TextureMapping()
    {
        uiSettings.Title = "Dynamic Real-Time Lighting/Shadows - Demos 4.g to 4.j";
        
        using var game = new TextureMappingDemo();
        game.InitWindow(gameSettings, uiSettings);
        game.Run();
    }

    /// <summary>
    /// Demos 4.k to 4.m
    /// Render 2D Depth Sample of Scene from Light’s Perspective
    /// Render Directional Shadows Calculated from Scene Depth
    /// Slightly Blur Edges of Shadows
    /// </summary>
    public static void BasicShadows()
    {
        uiSettings.Title = "Dynamic Real-Time Lighting/Shadows - Demos 4.k to 4.m";
        
        using var game = new BasicShadowDemo();
        game.InitWindow(gameSettings, uiSettings);
        game.Run();
    }

    /// <summary>
    /// Demos 4.n to 4.t
    /// Render 3D CubeMap Depth Sample of Scene
    /// Render Omni-Directional Shadows
    /// Render Exclusively the Bright Parts of the Scene
    /// Render Bright Parts of the Scene Blurred
    /// Render Full Scene with Bright Areas Blurred (Bloom)
    /// Render Parts of the Scene where Light is Unable to Reach
    /// Overlay Dark Areas of Scene onto Full Scene (SSAO)
    /// </summary>
    public static void ComplexLighting()
    {
        uiSettings.Title = "Dynamic Real-Time Lighting/Shadows - Demos 4.n to 4.t";
        
        using var game = new ComplexLightingDemo();
        game.InitWindow(gameSettings, uiSettings);
        game.Run();
    }
    
}