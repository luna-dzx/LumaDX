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
        PhongLighting();
    }

    
    /// <summary>
    /// Demo 4.a to 4.e
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
 
    
    
}