// -------------------- Program.cs -------------------- //

using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace PortalRendering;

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
    /// Demos 7.a to 7.d
    /// Render a 3D Portal Sampled from Another Part of the Scene
    /// Render a Matching 2nd Portal on the Other End
    /// Teleport Through Portal upon Collision
    /// Adjust View Direction After Teleportation
    /// </summary>
    public static void Main(string[] args)
    {
        uiSettings.Title = "Portal Rendering - Demos 7.a to 7.d";
        
        using var game = new PortalDemo();
        game.InitWindow(gameSettings, uiSettings);
        game.Run();
    }
}