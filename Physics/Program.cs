using System;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace Physics;

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


    /// <param name="args">'a'-'f' for which demo to run</param>
    public static void Main(string[] args)
    {
        if (args.Length < 1)
        {
            Stairs();

            return;
        }
        
        switch (args[0][0])
        {
            case <= 'c': Stairs(); return;
            case 'd': Stairs(); return;
        }
    }
    
    /// <summary>
    /// Demo 5.d
    /// Climbing Stair Geometry
    /// </summary>
    public static void Stairs()
    {
        using var game = new StairsDemo();
        game.InitWindow(gameSettings, uiSettings)
            .CursorState = CursorState.Normal;
        game.Run();
    }
    
}