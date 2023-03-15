using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using TextureWrapMode = OpenTK.Graphics.OpenGL4.TextureWrapMode;

namespace LumaDX;

/// <summary>
/// Texture Utilities
/// </summary>
public static class TexUtils
{
    /// <summary>
    /// Generate an SSAO noise texture 
    /// </summary>
    /// <returns>The texture's OpenGL binding</returns>
    public static int GenSsaoNoiseTex(int width)
    {
        float[] pixels = RandUtils.SsaoNoise(width);
        
        int noiseTexture = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D,noiseTexture);
        GL.TexImage2D(TextureTarget.Texture2D,0,PixelInternalFormat.Rgba16f,width,width,0,PixelFormat.Rgb,PixelType.Float,pixels);
        GL.TexParameter(TextureTarget.Texture2D,TextureParameterName.TextureMinFilter,(int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D,TextureParameterName.TextureMagFilter,(int)TextureMagFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D,TextureParameterName.TextureWrapS,(int)TextureWrapMode.Repeat);
        GL.TexParameter(TextureTarget.Texture2D,TextureParameterName.TextureWrapT,(int)TextureWrapMode.Repeat);

        return noiseTexture;
    }
}

/// <summary>
/// Colour Utilities
/// </summary>
public static class ColourUtils
{
    /// <summary>
    /// Vector3 shorthand for converting colours from HSV to RGB
    /// </summary>
    public static Vector3 HsvToRgb(Vector3 hsv)
    {
        Vector4 colour = new Vector4(hsv.X, hsv.Y, hsv.Z, 1f);
        var a = Color4.FromHsv(colour);
        return new Vector3(a.R, a.G, a.B);
    }
}