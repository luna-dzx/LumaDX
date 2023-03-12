using LumaDX;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace FileLoading;

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
        switch (args[0])
        {
            case "a": ModelToConsole(); return;
            case "b": FlattenedRender(); return;
            case "c": TextureToConsole(); return;
            case "d": RenderTexture(); return;
            case "e": SkyBoxToConsole(); return;
            case "f": SkyBoxFlatRender(); return;
            case "g": SkyBoxFullRender(); return;
        }
    }
    
    
    /// <summary>
    /// Demo 1.a.
    /// Demonstrate 3D Model Loading in the Console
    /// </summary>
    public static void ModelToConsole()
    {
        FileManager fm = new FileManager(AssetLocation + "dingus-the-cat/source/dingus.fbx");
        var modelInfo = fm.GetInfo();
        
        Console.WriteLine($"File Name: {modelInfo.FileName}\n{modelInfo}");


    }
    
    /// <summary>
    /// Demo 1.b.
    /// Render 2D Flattened Version of 3D Model
    /// </summary>
    public static void FlattenedRender()
    {
        uiSettings.Title = "File Loading - Demo 1.b.";
        
        using var game = new FlattenedRender();
        game.InitWindow(gameSettings, uiSettings);
        game.Run();
    }
    
    /// <summary>
    /// Demo 1.c.
    /// Load a Texture, Output Info to Console
    /// </summary>
    public static void TextureToConsole()
    {
        
    }
    
    /// <summary>
    /// Demo 1.d.
    /// Render 2D Texture onto the Screen
    /// </summary>
    public static void RenderTexture()
    {
        
    }
    
    /// <summary>
    /// Demo 1.e.
    /// Load a SkyBox, Output Info to Console
    /// </summary>
    public static void SkyBoxToConsole()
    {
        
    }
    
    /// <summary>
    /// Demo 1.f.
    /// Render SkyBox as 6 Individual Textures
    /// </summary>
    public static void SkyBoxFlatRender()
    {
        
    }
    
    /// <summary>
    /// Demo 1.g.
    /// Render SkyBox with Simplified 3D (just directions)
    /// </summary>
    public static void SkyBoxFullRender()
    {
        
    }
    
    
}