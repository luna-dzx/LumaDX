using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace PostProcessing;

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
    
    
    /// <summary>
    /// Demos 6.a to 6.e
    /// Render Scene with Gaussian Blur Effect
    /// Render Scene with Matrix Text Effect
    /// Render Scene with Night Vision Effect
    /// Render Scene with GreyScale Effect
    /// Combine 2 Effects
    /// </summary>
    public static void Main(string[] args)
    {
        uiSettings.Title = "Post Processing - Demos 6.a to 6.e";

        using var game = new PostProcessingDemo();
        game.InitWindow(gameSettings, uiSettings);
        game.Run();
    }
}