using OpenTK.Graphics.OpenGL4;
using TextureWrapMode = OpenTK.Graphics.OpenGL4.TextureWrapMode;

namespace LumaDX;

public class TexUtils
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