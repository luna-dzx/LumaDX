using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using StbImageSharp;

namespace LumaDX;

/// <summary>
/// Struct which stores data about a loaded image file
/// </summary>
public struct ImageData
{
    public string FileName;
    public Vector2i Size;
    public PixelFormat Format;
    public PixelInternalFormat InternalFormat;
    public byte[] PixelData;

    public ImageData(string fileName, Vector2i size, PixelFormat format, PixelInternalFormat internalFormat, byte[] data)
    {
        FileName = fileName;
        Size = size;
        Format = format;
        InternalFormat = internalFormat;
        PixelData = data;
    }

    /// <summary>
    /// Output info as table
    /// </summary>
    public override string ToString()
    {
        string output = "";

        int bytesPerPixel = 1;
        if (Format is PixelFormat.Bgr or PixelFormat.Rgb or PixelFormat.BgrInteger or PixelFormat.RgbInteger) bytesPerPixel = 3;
        if (Format is PixelFormat.Bgra or PixelFormat.Rgba or PixelFormat.BgraInteger or PixelFormat.RgbaInteger) bytesPerPixel = 4;
        
        output += "File Name       | " + FileName + "\n";
        output += "Resolution      | " + Size + "\n";
        output += "Bytes per Pixel | " + bytesPerPixel + "\n";
        output += "Pixel Format    | " + Format + "\n";
        output += "Num. Pixels     | " + PixelData.Length/bytesPerPixel + "\n";
        
        return output;
    }
}

// TODO: inheritance?

/// <summary>
/// Class for handling standard OpenGL texture objects
/// </summary>
public class Texture : IDisposable
{
    private int handle;
    private int unit;
    private readonly TextureTarget target;
    
    public Vector2i Size = Vector2i.Zero;
    
    /// <summary>
    /// Setup an OpenGL texture for manual texture handling
    /// </summary>
    /// <param name="textureUnit">the GPU texture unit to pass this texture to</param>
    /// <param name="textureTarget">the type of texture to store</param>
    public Texture(int textureUnit, TextureTarget textureTarget = TextureTarget.Texture2D)
    {
        handle = GL.GenTexture();
        unit = textureUnit;
        target = textureTarget;
    }

    /// <summary>
    /// Load a texture directly from a local file
    /// </summary>
    /// /// <param name="path">path to the image file</param>
    /// <param name="textureUnit">the GPU texture unit to pass this texture to</param>
    /// <param name="textureTarget">the type of texture to store</param>
    public Texture(string path, int textureUnit, TextureTarget textureTarget = TextureTarget.Texture2D, bool flipOnLoad = true) : this(textureUnit,textureTarget)
    {
        LoadFile(path, textureTarget, flipOnLoad);
    }

    /// <summary>
    /// Load an image file and store its data onto this texture on the GPU
    /// </summary>
    public Texture LoadFile(string path, TextureTarget textureTarget, bool flipOnLoad = true, bool generateMipMap = true)
    {
        var image = LoadImageData(path, flipOnLoad);
        
        this.Use();
        GL.TexImage2D(textureTarget,0,image.InternalFormat,image.Size.X,image.Size.Y,0,image.Format,PixelType.UnsignedByte,image.PixelData);
        if (generateMipMap) GL.GenerateMipmap((GenerateMipmapTarget)target);
        
        Size = image.Size;
        return this;
    }

    /// <summary>
    /// Efficiently load data from an image file (bitmaps are faster to load with a separate library than most images)
    /// </summary>
    public static ImageData LoadImageData(string path, bool flipOnLoad)
    {
        if (path.Substring(path.Length - 3) == "bmp") // significantly faster loads
        {
            var image = BmpSharp.BitmapFileHelper.ReadFileAsBitmap(path,flipOnLoad);
            return new ImageData(new FileInfo(path).Name, new Vector2i(image.Width, image.Height), PixelFormat.Bgr, PixelInternalFormat.Rgb, image.PixelData);
        }
        else
        {
            StbImage.stbi_set_flip_vertically_on_load((flipOnLoad)?1:0);
            using var stream = File.OpenRead(path);
            var image = ImageResult.FromStream(stream,ColorComponents.RedGreenBlueAlpha);
            return new ImageData(new FileInfo(path).Name, new Vector2i(image.Width, image.Height), PixelFormat.Rgba, PixelInternalFormat.Rgba, image.Data);
        }
    }

    // public so this can be overwritten
    public static Dictionary<string, TextureTarget> CubeMapTextureNames = GenerateCubeMapTextureNames();
    
    // generator for above property
    private static Dictionary<string, TextureTarget> GenerateCubeMapTextureNames()
    {
        var dict = new Dictionary<string, TextureTarget>();
        dict.Add("right",TextureTarget.TextureCubeMapPositiveX);
        dict.Add("left",TextureTarget.TextureCubeMapNegativeX);
        dict.Add("top",TextureTarget.TextureCubeMapPositiveY);
        dict.Add("bottom",TextureTarget.TextureCubeMapNegativeY);
        dict.Add("back",TextureTarget.TextureCubeMapNegativeZ);
        dict.Add("front",TextureTarget.TextureCubeMapPositiveZ);
        return dict;
    }



    /// <summary>
    /// Given a folder and an image file extension, load all 6 textures of a cubemap onto this OpenGL texture
    /// </summary>
    public static Texture LoadCubeMap(string filePath, string fileExtension, int textureUnit, bool flipOnLoad = false)
    {
        var texture = new Texture(textureUnit, TextureTarget.TextureCubeMap);

        foreach (var (side,texTarget) in CubeMapTextureNames) texture.LoadFile(filePath + side + fileExtension, texTarget, flipOnLoad, false);
        
        texture
            .MagFilter(TextureMagFilter.Linear)
            .MinFilter(TextureMinFilter.Linear)
            .Wrapping(TextureParameterName.TextureWrapS,TextureWrapMode.ClampToEdge)
            .Wrapping(TextureParameterName.TextureWrapT,TextureWrapMode.ClampToEdge)
            .Wrapping(TextureParameterName.TextureWrapR,TextureWrapMode.ClampToEdge);

        return texture;
    }

    /// <summary>
    /// Load image data from a pointer in memory to the start of the data
    /// </summary>
    public Texture LoadPtr(IntPtr pointer, int width, int height,
        PixelInternalFormat internalFormat = PixelInternalFormat.Rgba,
        PixelFormat pixelFormat = PixelFormat.Rgba,
        PixelType pixelType = PixelType.UnsignedByte)
    {
        this.Use();
        
        GL.TexImage2D
        (
            target,
            0,
            internalFormat,
            width,
            height,
            0,
            pixelFormat,
            pixelType,
            pointer
        );

        return this;
    }

    /// <summary>
    /// Load Image data from an IntPtr, and also set-up mip-mapping
    /// </summary>
    public Texture LoadPtrMipMapped(IntPtr pointer, int width, int height, int levels,
        SizedInternalFormat internalFormat = SizedInternalFormat.Rgba8,
        PixelFormat pixelFormat = PixelFormat.Rgba, PixelType pixelType = PixelType.UnsignedByte)
    {
        this.Use();
        
        GL.TexStorage2D((TextureTarget2d)target, levels, internalFormat, width, height);
        GL.TexSubImage2D(target, 0, 0, 0, width, height, pixelFormat, pixelType, pointer);

        GL.GenerateMipmap((GenerateMipmapTarget)target);

        return this;
    }


    /// <summary>
    /// Activate the correct texture unit and set up the OpenGL texture for editing properties
    /// </summary>
    public Texture Use()
    {
        GL.ActiveTexture((TextureUnit) (unit + (int)TextureUnit.Texture0));
        GL.BindTexture(target,handle);
        return this;
    }

    /// <summary>
    /// Set parameters for how textures handle texture coordinates when they lie outside of the range of 0->1
    /// </summary>
    /// <param name="paramName">set wrapping for x,y,z components of the texture separately</param>
    /// <param name="wrapMode">how to wrap the texture when texture coordinates lie outside of the range of 0->1</param>
    public Texture Wrapping(TextureParameterName paramName, TextureWrapMode wrapMode) { this.Use(); GL.TexParameter(target, paramName, (int)wrapMode); return this; }

    /// <summary>
    /// Set parameters for how textures handle texture coordinates when they lie outside of the range of 0->1 (for specifically x and y wrapping)
    /// </summary>
    /// <param name="wrapMode">how to wrap the texture when texture coordinates lie outside of the range of 0->1</param>
    public Texture Wrapping(TextureWrapMode wrapMode)
    {
        Wrapping(TextureParameterName.TextureWrapS,wrapMode);
        Wrapping(TextureParameterName.TextureWrapT,wrapMode);
        return this;
    }

    /// <summary>
    /// Set the max mip-mapping level
    /// </summary>
    /// <param name="level">Maximum mip-mapping level</param>
    public Texture MaxLevel(int level) { this.Use(); GL.TexParameter(target, TextureParameterName.TextureMaxLevel, level); return this; }
    
    
    /// <summary>
    /// Set parameters for how textures are displayed which take up less pixels on screen than the number of pixels in the texture image
    /// </summary>
    /// <param name="filter">the filter for how surrounding pixels should be sampled (if at all)</param>
    public Texture MinFilter(TextureMinFilter filter) { this.Use(); GL.TexParameter(target,TextureParameterName.TextureMinFilter,(int)filter); return this; }

    /// <summary>
    /// Set parameters for how textures are displayed which take up more pixels on screen than the number of pixels in the texture image
    /// </summary>
    /// <param name="filter">the filter for how surrounding pixels should be sampled (if at all)</param>
    public Texture MagFilter(TextureMagFilter filter) { this.Use(); GL.TexParameter(target,TextureParameterName.TextureMagFilter,(int)filter); return this; }

    /// <summary>
    /// simplify the crossover of MagFilter and MinFilter
    /// </summary>
    public enum TextureFilter
    {
        /// <summary>[requires: v1.0] Original was GL_NEAREST = 0x2600</summary>
        Nearest = 9728, // 0x00002600
        /// <summary>[requires: v1.0] Original was GL_LINEAR = 0x2601</summary>
        Linear = 9729, // 0x00002601
    }

    /// <summary>
    /// Set parameters for how textures are displayed which take up less/more pixels on screen than the number of pixels in the texture image
    /// </summary>
    /// <param name="filter">the filter for how surrounding pixels should be sampled (if at all)</param>
    public Texture Filter(TextureFilter filter)
    {
        MinFilter((TextureMinFilter)filter);
        MagFilter((TextureMagFilter)filter);
        return this;
    }
    
    
    /// <summary>
    /// Set parameters for how mipmapping should be handled (lower res textures further away)
    /// </summary>
    /// <param name="mipmapFilter">the texture filter for blending between mipmaps and the current texture's sample</param>
    public Texture Mipmapping(TextureMinFilter mipmapFilter) { this.Use(); GL.TexParameter(target,TextureParameterName.TextureMinFilter,(int)mipmapFilter); return this; }

    /// <summary>
    /// Delete the OpenGL texture object
    /// </summary>
    public void Dispose() => GL.DeleteTexture(handle);

    /// <summary>
    /// Get the OpenGL handle of the texture for manipulation beyond this class
    /// </summary>
    /// <returns>OpenGL texture handle</returns>
    public int GetHandle() => handle;
    
    /// <summary>
    /// Get the OpenGL texture unit of the texture
    /// </summary>
    /// <returns>OpenGL texture unit</returns>
    public int GetUnit() => unit;
    
    /// <summary>
    /// Override (int) cast to return texture handle
    /// </summary>
    /// <param name="texture">the texture who's handle you are getting from the cast</param>
    /// <returns>texture handle</returns>
    public static explicit operator int(Texture texture) => texture.GetHandle();

}

/// <summary>
/// Textures which the CPU can read from, but doesn't write to
/// </summary>
public class TextureBuffer
{
    public readonly int Handle;
    public readonly int NumSamples;
    public readonly PixelFormat Format;
    public readonly PixelInternalFormat InternalFormat;
    public readonly SizedInternalFormat InternalFormatSized;
    public readonly TextureTarget Target;
    public readonly PixelType PixelType;
    public readonly Vector2i Size;

    /// <summary>
    /// Standard initialization with texture formatting and size
    /// </summary>
    public TextureBuffer(PixelInternalFormat internalFormat, PixelFormat format, (int,int) size,
        TextureTarget target = TextureTarget.Texture2D, PixelType pixelType = PixelType.UnsignedByte, int samples = 4, int mipmap = 0, int border = 0)
    {
        (Size.X, Size.Y) = size;
        
        Handle = GL.GenTexture();
        Format = format;
        Target = target;
        PixelType = pixelType;
        InternalFormat = internalFormat;
        Format = format;
        NumSamples = samples;

        this.Use(); 
        
        if (37120 <= (int)target && (int)target <= 37123) // MSAA
        {
            if (internalFormat == PixelInternalFormat.Depth24Stencil8)
            {
                GL.TexImage2DMultisample((TextureTargetMultisample)target,samples,
                    PixelInternalFormat.Depth24Stencil8,Size.X,Size.Y,true);
            }
            else
            {
                GL.TextureStorage2DMultisample(Handle,NumSamples,(SizedInternalFormat)internalFormat,Size.X,Size.Y,true);
            }
            
        }
        else // NO MSAA
        {
            GL.TexImage2D(Target, mipmap, InternalFormat, Size.X, Size.Y, border, Format, PixelType, IntPtr.Zero);
            MinFilter(TextureMinFilter.Linear);
        }
        
        
        GL.BindTexture(Target,0);
    }
    
    /// <summary>
    /// Standard initialization with texture formatting and size as a vector
    /// </summary>
    public TextureBuffer(PixelInternalFormat internalFormat, PixelFormat format, Vector2i size,
        TextureTarget target = TextureTarget.Texture2D, PixelType pixelType = PixelType.UnsignedByte, int samples = 4, int mipmap = 0,
        int border = 0) :
        this(internalFormat, format, (size.X,size.Y),target,pixelType,samples,mipmap,border) { }

    /// <summary>
    /// Allow int cast to return the OpenGL handle
    /// </summary>
    public static explicit operator int(TextureBuffer textureBuffer) => textureBuffer.Handle;

    /// <summary>
    /// Bind for editing / rendering / etc.
    /// </summary>
    public TextureBuffer Use() { GL.BindTexture(Target,Handle); return this; }
    
    
    // have to repeat these instead of using inheritance to allow for interfacing
    
    /// <summary>
    /// Set parameters for how textures handle texture coordinates when they lie outside of the range of 0->1
    /// </summary>
    /// <param name="paramName">set wrapping for x,y,z components of the texture separately</param>
    /// <param name="wrapMode">how to wrap the texture when texture coordinates lie outside of the range of 0->1</param>
    public TextureBuffer Wrapping(TextureParameterName paramName, TextureWrapMode wrapMode) { this.Use(); GL.TexParameter(Target, paramName, (int)wrapMode); return this; }

    /// <summary>
    /// Set parameters for how textures handle texture coordinates when they lie outside of the range of 0->1 (for specifically x and y wrapping)
    /// </summary>
    /// <param name="wrapMode">how to wrap the texture when texture coordinates lie outside of the range of 0->1</param>
    public TextureBuffer Wrapping(TextureWrapMode wrapMode)
    {
        Wrapping(TextureParameterName.TextureWrapS,wrapMode);
        Wrapping(TextureParameterName.TextureWrapT,wrapMode);
        return this;
    }

    /// <summary>
    /// Set parameters for how textures are displayed which take up less pixels on screen than the number of pixels in the texture image
    /// </summary>
    /// <param name="filter">the filter for how surrounding pixels should be sampled (if at all)</param>
    public TextureBuffer MinFilter(TextureMinFilter filter) { this.Use(); GL.TexParameter(Target,TextureParameterName.TextureMinFilter,(int)filter); return this; }

    /// <summary>
    /// Set parameters for how textures are displayed which take up more pixels on screen than the number of pixels in the texture image
    /// </summary>
    /// <param name="filter">the filter for how surrounding pixels should be sampled (if at all)</param>
    public TextureBuffer MagFilter(TextureMagFilter filter) { this.Use(); GL.TexParameter(Target,TextureParameterName.TextureMagFilter,(int)filter); return this; }

}


/// <summary>
/// Textures which the CPU cannot read from
/// </summary>
public class RenderBuffer
{
    public readonly int Handle;
    public readonly RenderbufferStorage Format;
    public readonly RenderbufferTarget Target;
    public readonly Vector2i Size;

    /// <summary>
    /// Standard initialization with texture formatting and size
    /// </summary>
    public RenderBuffer(RenderbufferStorage format, (int,int) size, RenderbufferTarget target = RenderbufferTarget.Renderbuffer)
    {
        (Size.X, Size.Y) = size;
        
        Handle = GL.GenRenderbuffer();
        Format = format;
        Target = target;

        GL.BindRenderbuffer(Target,Handle);
        GL.RenderbufferStorage(Target,Format,Size.X,Size.Y);
        GL.BindRenderbuffer(Target,0);
    }

    /// <summary>
    /// Standard initialization with texture formatting and size as a vector
    /// </summary>
    public RenderBuffer(RenderbufferStorage internalformat, Vector2i resolution, RenderbufferTarget bufferTarget = RenderbufferTarget.Renderbuffer) : 
        this(internalformat, (resolution.X, resolution.Y), bufferTarget) { }

    /// <summary>
    /// Allow int cast to return the OpenGL handle
    /// </summary>
    public static explicit operator int(RenderBuffer renderBuffer) => renderBuffer.Handle;

    /// <summary>
    /// Bind for editing / rendering / etc.
    /// </summary>
    public RenderBuffer Use()
    {
        GL.BindRenderbuffer(Target,Handle);
        return this;
    }

}