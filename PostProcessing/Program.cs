using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace PostProcessing;

internal static class Program
{
    private const int FPS = 60;
    
    /// <summary>
    /// Demos 7.a to 7.e
    /// Render Scene with Gaussian Blur Effect
    /// Render Scene with Matrix Text Effect
    /// Render Scene with Night Vision Effect
    /// Render Scene with GreyScale Effect
    /// Combine 2 Effects
    /// </summary>
    public static void Main(string[] args)
    {
        #region settings
    
        var gameSettings = GameWindowSettings.Default;
        gameSettings.RenderFrequency = FPS;
        gameSettings.UpdateFrequency = FPS;

        var uiSettings = NativeWindowSettings.Default;
        uiSettings.APIVersion = Version.Parse("4.1.0");
        uiSettings.Size = new Vector2i(1600,900);
        uiSettings.Title = "LearnOpenGL";
        uiSettings.NumberOfSamples = 4;

        uiSettings.WindowState = WindowState.Normal;
        uiSettings.WindowBorder = WindowBorder.Resizable;
        uiSettings.IsEventDriven = false;
        uiSettings.StartFocused = true;

        #endregion

        using var game = new PostProcessingDemo();
        game.InitWindow(gameSettings, uiSettings)
            .CursorState = CursorState.Normal;
        game.Run();
    }
}