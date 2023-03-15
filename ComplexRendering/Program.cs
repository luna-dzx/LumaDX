using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace ComplexRendering;

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
        Size = new Vector2i(1600, 900),
        NumberOfSamples = 4,

        WindowState = WindowState.Normal,
        WindowBorder = WindowBorder.Resizable,
        IsEventDriven = false,
        StartFocused = true
    };

    #endregion

    
    /// <param name="args">'a'-'d' for which demo to run</param>
    public static void Main(string[] args)
    {
        if (args.Length < 1)
        {
            SkyBoxInScene();
            InstancedRendering();

            return;
        }
        
        switch (args[0][0])
        {
            default: SkyBoxInScene(); return;
            case 'c': case 'd': InstancedRendering(); return;
        }
    }
    
    
    /// <summary>
    /// Demos 3.a to 3.b
    /// Render Certain Areas of Screen with SkyBox
    /// Render a SkyBox and a 3D Model Simultaneously
    /// </summary>
    public static void SkyBoxInScene()
    {
        uiSettings.Title = "Complex Rendering - Demos 3.a to 3.b";
        
        using var game = new SkyBoxInSceneDemo();
        game.InitWindow(gameSettings, uiSettings);
        game.Run();
    }
    
    
    /// <summary>
    /// Demos 3.c to 3.d
    /// Transform and Render 1000+ of the Same 3D Model
    /// Render a 3D Model with Refraction based on a CubeMap
    /// </summary>
    public static void InstancedRendering()
    {
        uiSettings.Title = "Complex Rendering - Demos 3.c to 3.d";
        
        using var game = new InstancedRenderingDemo();
        game.InitWindow(gameSettings, uiSettings);
        game.Run();
    }
    
    
}