// -------------------- Program.cs -------------------- //

using LumaDX;
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

    // don't want to generate this each time we do a demo so cache it here
    public static readonly Objects.Mesh EllipsoidMesh = Maths.GenerateIcoSphere(3);

    /// <param name="args">'a'-'f' for which demo to run</param>
    public static void Main(string[] args)
    {
        if (args.Length < 1)
        {
            BasicCollisions();
            Stairs();
            FirstPersonCollisions();

            return;
        }
        
        switch (args[0][0])
        {
            case <= 'c': BasicCollisions(); return;
            case 'd': Stairs(); return;
            case >= 'e': FirstPersonCollisions(); return;
        }
    }
    
    /// <summary>
    /// Demos 5.a to 5.c
    /// Highlight Triangles Near Player with Threshold
    /// Change Colour of Ellipsoid as it Passes Through Triangle
    /// Change Velocity of Ellipsoid After Collision
    /// </summary>
    public static void BasicCollisions()
    {
        uiSettings.Title = "Physics - Demos 5.a to 5.c";
        
        using var game = new BasicCollisionDemo();
        game.InitWindow(gameSettings, uiSettings)
            .CursorState = CursorState.Normal;
        game.Run();
    }
    
    /// <summary>
    /// Demo 5.d
    /// Climbing Stair Geometry
    /// </summary>
    public static void Stairs()
    {
        uiSettings.Title = "Physics - Demo 5.d";
        
        using var game = new StairsDemo();
        game.InitWindow(gameSettings, uiSettings)
            .CursorState = CursorState.Normal;
        game.Run();
    }
    
    /// <summary>
    /// Demos 5.e to 5.f
    /// Jumping When Grounded
    /// Hitting Head on Ceiling to Stop Vertical Velocity
    /// </summary>
    public static void FirstPersonCollisions()
    {
        uiSettings.Title = "Physics - Demos 5.e to 5.f";
        
        using var game = new FirstPersonCollisionDemo();
        game.InitWindow(gameSettings, uiSettings)
            .CursorState = CursorState.Normal;
        game.Run();
    }
    
}