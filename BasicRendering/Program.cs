using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace BasicRendering;

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


    /// <param name="args">'a'-'g' for which demo to run</param>
    public static void Main(string[] args)
    {
        if (args.Length < 1)
        {
            ProjectedTriangle();
            PrimitivesRender();
            FullModelRender();

            return;
        }
        
        switch (args[0][0])
        {
            case 'a': ProjectedTriangle(); return;
            case 'b': PrimitivesRender(); return;
            case 'c': PrimitivesRender(); return;
            default: FullModelRender(); return;
        }
    }

    
    /// <summary>
    /// Demo 2.a
    /// Render Single Triangle Projected into 3D
    /// </summary>
    public static void ProjectedTriangle()
    {
        uiSettings.Title = "Basic Rendering - Demo 2.a";
        
        using var game = new ProjectedTriangleDemo();
        game.InitWindow(gameSettings, uiSettings);
        game.Run();
    }
    
    
    /// <summary>
    /// Demos 2.b to 2.c
    /// Render Cube and Quad, Coloured, in 3D
    /// Move Camera Around Scene
    /// </summary>
    public static void PrimitivesRender()
    {
        uiSettings.Title = "Basic Rendering - Demos 2.b to 2.c";
        
        using var game = new PrimitivesRenderDemo();
        game.InitWindow(gameSettings, uiSettings);
        game.Run();
    }


    /// <summary>
    /// Demos 2.d to 2.g
    /// Render Lines of Each triangle of Loaded 3D Model
    /// Colour 3D Model with Outlined Triangles
    /// Colour 3D Model based on Normal Data
    /// Texturing 3D Model using Texture Coordinates
    /// </summary>
    public static void FullModelRender()
    {
        uiSettings.Title = "Basic Rendering - Demos 2.d to 2.f";
        
        using var game = new FullModelRenderDemo();
        game.InitWindow(gameSettings, uiSettings);
        game.Run();
    }
    
}