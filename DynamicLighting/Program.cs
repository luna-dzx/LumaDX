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

            return;
        }
        
        char demo = args[0][0];

        switch (demo)
        {
            case <= 'e':
                PhongLighting(); return;
            case 'f':
                LightCasters(); return;
            case >= 'g' and <= 'j':
                TextureMapping(); return;
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
    
}