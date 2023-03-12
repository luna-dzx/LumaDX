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
            ModelToConsole();
            FlattenedRender();
            TextureToConsole();
            RenderTexture();
            SkyBoxToConsole();
            SkyBoxFlatRender();
            SkyBoxFullRender();

            return;
        }
        
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
    /// Output Information from Loaded 3D Model
    /// </summary>
    public static void ModelToConsole()
    {
        FileManager fm = new FileManager(AssetLocation + "dingus-the-cat/source/dingus.fbx");
        Console.WriteLine(fm.GetInfo()); // formatted into table in ModelInfo struct
    }
    
    /// <summary>
    /// Demo 1.b.
    /// Render 2D Flattened Version of 3D Model
    /// </summary>
    public static void FlattenedRender()
    {
        uiSettings.Title = "File Loading - Demo 1.b.";
        uiSettings.Size = new Vector2i(1600, 900);
        
        using var game = new FlattenedRenderDemo();
        game.InitWindow(gameSettings, uiSettings);
        game.Run();
    }
    
    /// <summary>
    /// Demo 1.c.
    /// Load a Texture, Output Info to Console
    /// </summary>
    public static void TextureToConsole()
    {
        // formatted in ImageData struct
        Console.WriteLine(Texture.LoadImageData(AssetLocation + "dingus-the-cat/textures/dingus_nowhiskers.jpg", true));
    }
    
    /// <summary>
    /// Demo 1.d.
    /// Render 2D Texture onto the Screen
    /// </summary>
    public static void RenderTexture()
    {
        uiSettings.Title = "File Loading - Demo 1.d.";
        uiSettings.Size = new Vector2i(900,900);
        
        using var game = new RenderTextureDemo();
        game.InitWindow(gameSettings, uiSettings);
        game.Run();
    }
    
    /// <summary>
    /// Demo 1.e.
    /// Load a SkyBox, Output Info to Console
    /// </summary>
    public static void SkyBoxToConsole()
    {
        string path = AssetLocation + "skybox/";
        string fileExtension = ".jpg";

        foreach (var (side,target) in Texture.CubeMapTextureNames)
        {
            Console.WriteLine(target+ "\n" + Texture.LoadImageData(path + side + fileExtension, false)+"\n");
        }
    }


    /// <summary>
    /// Demo 1.f.
    /// Render SkyBox as 6 Individual Textures
    /// </summary>
    public static void SkyBoxFlatRender()
    {
        uiSettings.Title = "File Loading - Demo 1.f.";
        uiSettings.Size = new Vector2i(1200,900);

        using var game = new SkyBoxFlatRenderDemo();
        game.InitWindow(gameSettings, uiSettings);
        game.Run();
    }
    
    /// <summary>
    /// Demo 1.g.
    /// Render SkyBox with Simplified 3D (just directions)
    /// </summary>
    public static void SkyBoxFullRender()
    {
        uiSettings.Title = "File Loading - Demo 1.g.";
        uiSettings.Size = new Vector2i(1200,720);

        using var game = new SkyBoxFullRenderDemo();
        game.InitWindow(gameSettings, uiSettings);
        game.Run();
    }
    
    
}