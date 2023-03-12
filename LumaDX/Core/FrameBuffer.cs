using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace LumaDX;

public class FrameBuffer : IDisposable
{
    private int handle = -1;
    
    // optional extras for simplification
    protected bool usingPreset = false;
    public int NumColourAttachments;
    protected TextureBuffer[] colourAttachments;
    protected TextureBuffer depthStencilAttachment;
    protected RenderBuffer depthStencilRenderBuffer;
    protected bool usingRenderBuffer = true;
    public Vector2i Size;


    public FrameBuffer()
    {
        handle = GL.GenFramebuffer();
    }

    /// <summary>
    /// Create a common FBO with n textures bound to colour attachment 0,1,2... and a render buffer bound to the depth/stencil buffer
    /// </summary>
    /// <param name="pixelFormat"></param>
    /// <param name="size"></param>
    public FrameBuffer(Vector2i size, TextureTarget target = TextureTarget.Texture2D, PixelFormat pixelFormat= PixelFormat.Rgb,
        PixelInternalFormat internalFormat = PixelInternalFormat.Rgba8, int numSamples = 4, int numColourAttachments = 1, bool readableDepth = false) : this()
    {
        usingPreset = true;

        Size = size;

        NumColourAttachments = numColourAttachments;
        colourAttachments = new TextureBuffer[NumColourAttachments];
        for (int i = 0; i < NumColourAttachments; i++)
            colourAttachments[i] = new TextureBuffer(internalFormat, pixelFormat, (size.X,size.Y), target,samples:numSamples);

        if (37120 <= (int)target && (int)target <= 37123) // MSAA
        {
            for (int i = 0; i < NumColourAttachments; i++) 
                AttachTexture(colourAttachments[i], FramebufferAttachment.ColorAttachment0 + i);

            depthStencilAttachment = new TextureBuffer(PixelInternalFormat.Depth24Stencil8, PixelFormat.DepthStencil,
                (size.X, size.Y), target, samples: numSamples);
            
            AttachTexture(depthStencilAttachment, FramebufferAttachment.DepthStencilAttachment);

            usingRenderBuffer = false;
        }
        else // NO MSAA
        {
            for (int i = 0; i < NumColourAttachments; i++)
            {
                colourAttachments[i].Wrapping(TextureWrapMode.ClampToEdge);
                AttachTexture(colourAttachments[i], FramebufferAttachment.ColorAttachment0 + i);
            }
            
            if (readableDepth)
            {
                usingRenderBuffer = false;
                depthStencilAttachment = new TextureBuffer(PixelInternalFormat.Depth24Stencil8,
                    PixelFormat.DepthStencil, size, target,
                    PixelType.UnsignedInt248);
                
                AttachTexture(depthStencilAttachment, FramebufferAttachment.DepthStencilAttachment);
                
            }
            else
            {
                depthStencilRenderBuffer = new RenderBuffer(RenderbufferStorage.Depth24Stencil8, size);
                AttachRenderBuffer(depthStencilRenderBuffer, FramebufferAttachment.DepthStencilAttachment);
            }
        }
        


        CheckCompletion();

        ReadMode();

    }
    
    /// <summary>
    /// Special Case of Loading an Image Directly to a FrameBuffer
    /// </summary>
    public FrameBuffer(string fileName, bool flipOnLoad = true) : this()
    {
        var image = Texture.LoadImageData(fileName, flipOnLoad);
        Size = image.Size;
        
        usingPreset = true;

        NumColourAttachments = 1;
        colourAttachments = new TextureBuffer[NumColourAttachments];
        colourAttachments[0] = new TextureBuffer(PixelInternalFormat.Rgb8, PixelFormat.Rgb, Size);
        colourAttachments[0].Wrapping(TextureWrapMode.ClampToEdge);
        GL.TexImage2D(TextureTarget.Texture2D,0,PixelInternalFormat.Rgb,Size.X,Size.Y,0,PixelFormat.Bgr,PixelType.UnsignedByte,image.PixelData);
        
        AttachTexture(colourAttachments[0], FramebufferAttachment.ColorAttachment0);

        depthStencilRenderBuffer = new RenderBuffer(RenderbufferStorage.Depth24Stencil8, Size);
        AttachRenderBuffer(depthStencilRenderBuffer, FramebufferAttachment.DepthStencilAttachment);

        CheckCompletion();

        ReadMode();

    }
    


    public FrameBuffer(Vector2i size, PixelInternalFormat[]? internalFormats = null,
        TextureTarget target = TextureTarget.Texture2D, PixelFormat pixelFormat = PixelFormat.Rgb, int numSamples = 4, bool readableDepth = false) : this()
    {
        internalFormats ??= new [] { PixelInternalFormat.Rgb8 };
        int numColourAttachments = internalFormats.Length;
        
        usingPreset = true;

        Size = size;

        NumColourAttachments = numColourAttachments;
        colourAttachments = new TextureBuffer[NumColourAttachments];
        for (int i = 0; i < NumColourAttachments; i++)
            colourAttachments[i] = new TextureBuffer(internalFormats[i], pixelFormat, (size.X,size.Y), target,samples:numSamples);

        if (37120 <= (int)target && (int)target <= 37123) // MSAA
        {
            for (int i = 0; i < NumColourAttachments; i++) 
                AttachTexture(colourAttachments[i], FramebufferAttachment.ColorAttachment0 + i);

            depthStencilAttachment = new TextureBuffer(PixelInternalFormat.Depth24Stencil8, PixelFormat.DepthStencil,
                (size.X, size.Y), target, samples: numSamples);
            
            AttachTexture(depthStencilAttachment, FramebufferAttachment.DepthStencilAttachment);

            usingRenderBuffer = false;
        }
        else // NO MSAA
        {
            for (int i = 0; i < NumColourAttachments; i++)
            {
                colourAttachments[i].Wrapping(TextureWrapMode.ClampToEdge);
                AttachTexture(colourAttachments[i], FramebufferAttachment.ColorAttachment0 + i);
            }

            if (readableDepth)
            {
                usingRenderBuffer = false;
                depthStencilAttachment = new TextureBuffer(PixelInternalFormat.Depth24Stencil8,
                    PixelFormat.DepthStencil, size, target,
                    PixelType.UnsignedInt248);
                
                AttachTexture(depthStencilAttachment, FramebufferAttachment.DepthStencilAttachment);
                
            }
            else
            {
                depthStencilRenderBuffer = new RenderBuffer(RenderbufferStorage.Depth24Stencil8, size);
                AttachRenderBuffer(depthStencilRenderBuffer, FramebufferAttachment.DepthStencilAttachment);
            }
            
        }
        


        CheckCompletion();

        ReadMode();
    }


    public FrameBuffer UseTexture(int num = -1)
    {
        if (usingPreset)
        {

            if (num == -1)
            {
                for (int i = 0; i < NumColourAttachments; i++)
                {
                    GL.ActiveTexture(TextureUnit.Texture0+i);
                    colourAttachments[i].Use();
                }
            }
            else
            {
                colourAttachments[num].Use();
            }

                
            return this;
        }

        throw new Exception("Texture isn't handled by this FrameBuffer");
    }

    public FrameBuffer UniformTexture(int shader, string name, int unit)
    {
        GL.UseProgram(shader);
        UseTexture();
        GL.Uniform1(GL.GetUniformLocation(shader,name),unit);
        return this;
    }
    
    // TEMPORARY THING, REMOVE THIS PLS
    public FrameBuffer UniformTexture(string name, ShaderProgram shaderProgram, int textureUnit = 0)
    {
        shaderProgram.Use();
        GL.ActiveTexture(TextureUnit.Texture0 + textureUnit);
        GL.BindTexture(TextureTarget.Texture2D,colourAttachments[0].Handle);
        shaderProgram.Uniform1(name, textureUnit);
        return this;
    }
    
    
    public FrameBuffer UniformTextures(int shader, string[] names, int offset=0)
    {
        GL.UseProgram(shader);
        UseTexture();
        for (int i = 0; i < names.Length; i++) { GL.Uniform1(GL.GetUniformLocation(shader,names[i]),offset+i); }
        return this;
    }

    
    

    /// <summary>
    /// Does nothing if complete, throws error if incomplete
    /// </summary>
    /// <exception cref="Exception"></exception>
    public void CheckCompletion()
    {
        var status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
        if (status != FramebufferErrorCode.FramebufferComplete)
        {
            throw new Exception("Incomplete Fbo -> " + status);
        }
    }


    public FrameBuffer AttachTexture(TextureTarget textureTarget, int textureHandle, FramebufferAttachment attachment, FramebufferTarget target = FramebufferTarget.Framebuffer, int mipmap = 0)
    {
        WriteMode();
        GL.FramebufferTexture2D(target,attachment,textureTarget,textureHandle,mipmap);
        ReadMode();
        return this;
    }

    public FrameBuffer AttachTexture(TextureBuffer textureBuffer, FramebufferAttachment attachment,
        FramebufferTarget target = FramebufferTarget.Framebuffer, int mipmap = 0)
    {
        return AttachTexture(textureBuffer.Target, textureBuffer.Handle,attachment,target,mipmap);
    }

    public FrameBuffer AttachRenderBuffer(RenderbufferTarget renderBufferTarget, int renderBufferHandle, FramebufferAttachment attachment, FramebufferTarget target = FramebufferTarget.Framebuffer)
    {
        WriteMode();
        GL.FramebufferRenderbuffer(target,attachment,renderBufferTarget,renderBufferHandle);
        ReadMode();
        return this;
    }

    public FrameBuffer AttachRenderBuffer(RenderBuffer renderBuffer,
        FramebufferAttachment attachment, FramebufferTarget target = FramebufferTarget.Framebuffer)
    {
        return AttachRenderBuffer(renderBuffer.Target, renderBuffer.Handle, attachment, target);
    }

    public FrameBuffer WriteMode()
    {
        GL.BindFramebuffer(FramebufferTarget.Framebuffer,handle);
        return this;
    }
    
    public FrameBuffer ReadMode()
    {
        GL.BindFramebuffer(FramebufferTarget.Framebuffer,0);
        return this;
    }


    public FrameBuffer SetDrawBuffers(DrawBuffersEnum[] colourAttachments)
    {
        this.WriteMode();
        GL.DrawBuffers(colourAttachments.Length,colourAttachments);
        return this;
    }
    

    public void Dispose()
    {
        GL.BindFramebuffer(FramebufferTarget.Framebuffer,0);

        if (usingPreset)
        {
            for (int i = 0; i < NumColourAttachments; i++)
                GL.DeleteTexture(colourAttachments[i].Handle);
            if (usingRenderBuffer)
            {
                GL.DeleteRenderbuffer(depthStencilRenderBuffer.Handle);
            }
            else
            {
                GL.DeleteTexture(depthStencilAttachment.Handle);
            }
        }

        GL.DeleteFramebuffer(handle);
    }
    
    public static explicit operator int(FrameBuffer fbo) => fbo.handle;



    public FrameBuffer BlitDepth(Vector2i size, bool gammaCorrection = false)
    {
        if (gammaCorrection) GL.Disable(EnableCap.FramebufferSrgb);
        
        GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, handle);
        GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);
        GL.BlitFramebuffer(0,0,Size.X,Size.Y,0,0,size.X,size.Y,ClearBufferMask.DepthBufferBit,BlitFramebufferFilter.Nearest);
        
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        
        if (gammaCorrection) GL.Enable(EnableCap.FramebufferSrgb);
        
        
        
        return this;
    }
    
    public FrameBuffer BlitDepth(FrameBuffer frameBuffer, bool gammaCorrection = false)
    {
        if (gammaCorrection) GL.Disable(EnableCap.FramebufferSrgb);
        
        GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, handle);
        GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, frameBuffer.handle);
        GL.BlitFramebuffer(0,0,Size.X,Size.Y,0,0,frameBuffer.Size.X,frameBuffer.Size.Y,ClearBufferMask.DepthBufferBit,BlitFramebufferFilter.Nearest);
        
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        
        if (gammaCorrection) GL.Enable(EnableCap.FramebufferSrgb);
        
        
        
        return this;
    }
    
    

}


/// <summary>
/// different way of constructing FBOs for complex combinations of 2D textures and one depth/stencil attachment
/// </summary>
public class GeometryBuffer : FrameBuffer
{
    private List<TextureBuffer> colourAttachmentsList;
    private DrawBuffersEnum[] drawBuffers;

    public GeometryBuffer(Vector2i size, bool readableDepth = false) : base()
    {
        Size = size;
        colourAttachmentsList = new List<TextureBuffer>();

        usingPreset = true;

        if (readableDepth)
        {
            usingRenderBuffer = false;
            depthStencilAttachment = new TextureBuffer(PixelInternalFormat.Depth24Stencil8,
                PixelFormat.DepthStencil, size, TextureTarget.Texture2D,
                PixelType.UnsignedInt248);
            
            AttachTexture(depthStencilAttachment, FramebufferAttachment.DepthStencilAttachment);
        }
        else
        {
            depthStencilRenderBuffer = new RenderBuffer(RenderbufferStorage.Depth24Stencil8, size);
            AttachRenderBuffer(depthStencilRenderBuffer, FramebufferAttachment.DepthStencilAttachment);
        }


    }

    public FrameBuffer SetDrawBuffers()
    {
        this.WriteMode();
        GL.DrawBuffers(drawBuffers.Length,drawBuffers);
        return this;
    }


    public GeometryBuffer AddTexture(
        PixelInternalFormat internalFormat = PixelInternalFormat.Rgb8,
        PixelFormat pixelFormat = PixelFormat.Rgb,
        int numSamples = 4
    )
    {
        TextureBuffer textureBuffer = new TextureBuffer(internalFormat, pixelFormat, Size, TextureTarget.Texture2D, samples:numSamples);
        textureBuffer.Wrapping(TextureWrapMode.ClampToEdge);
        AttachTexture(textureBuffer, FramebufferAttachment.ColorAttachment0 + colourAttachmentsList.Count);
        
        colourAttachmentsList.Add(textureBuffer);

        return this;
    }

    public GeometryBuffer Construct()
    {
        NumColourAttachments = colourAttachmentsList.Count;
        colourAttachments = colourAttachmentsList.ToArray();
        drawBuffers = OpenGL.GetDrawBuffers(NumColourAttachments);
        
        CheckCompletion();
        ReadMode();

        return this;
    }

}



public class DepthMap : IDisposable
{

    public readonly ShaderProgram Shader;
    
    public readonly int Handle;
    public readonly int TextureHandle;

    public readonly Vector2i Size;
    
    public Matrix4 ViewSpaceMatrix;
    
    public Vector3 Position;
    public Vector3 Direction;


    public DepthMap(Vector2i size, Vector3 position, Vector3 direction) : this(Constants.LibraryShaderPath + "DepthMap/",size,position,direction) { }

    public DepthMap(string shaderPath, Vector2i size, Vector3 position, Vector3 direction)
    {
        Handle = GL.GenFramebuffer();
        TextureHandle = GL.GenTexture();
        Size = size;
        Position = position;
        Direction = direction;
        
        GL.BindTexture(TextureTarget.Texture2D,TextureHandle);
        GL.TexImage2D(TextureTarget.Texture2D,0,PixelInternalFormat.DepthComponent,Size.X,Size.Y,0,PixelFormat.DepthComponent,PixelType.Float,IntPtr.Zero);
        GL.TexParameter(TextureTarget.Texture2D,TextureParameterName.TextureMinFilter,(int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D,TextureParameterName.TextureMagFilter,(int)TextureMagFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D,TextureParameterName.TextureWrapS,(int)TextureWrapMode.Repeat);
        GL.TexParameter(TextureTarget.Texture2D,TextureParameterName.TextureWrapT,(int)TextureWrapMode.Repeat);
        
        GL.BindFramebuffer(FramebufferTarget.Framebuffer,Handle);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer,FramebufferAttachment.DepthAttachment,TextureTarget.Texture2D,TextureHandle,0);
        GL.DrawBuffer(DrawBufferMode.None);
        GL.ReadBuffer(ReadBufferMode.None);

        GL.BindFramebuffer(FramebufferTarget.Framebuffer,0);

        Shader = new ShaderProgram(
            shaderPath + "vertex.glsl",
            shaderPath + "fragment.glsl"
        ).SetModelLocation("model");
    }

    public DepthMap DrawMode(int x = 0, int y = 0, int width = 0, int height = 0, CullFaceMode cullFaceMode = CullFaceMode.Front)
    {
        if (width == 0) width = Size.X;
        if (height == 0) height = Size.Y;
        
        GL.CullFace(cullFaceMode);
        GL.Viewport(x,y,width,height);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer,Handle);
        GL.Clear(ClearBufferMask.DepthBufferBit);
        
        Shader.Use();

        return this;
    }

    public DepthMap ReadMode()
    {
        GL.BindFramebuffer(FramebufferTarget.Framebuffer,0);
        return this;
    }

    public DepthMap ProjectOrthographic(float orthoWidth = 24f, float orthoHeight = 24f, float clipNear = 0.05f, float clipFar = 50f, Vector3 up = default)
    {
        if (up == default) up = Vector3.UnitY;
        Matrix4 viewMatrix = Matrix4.LookAt(Position, Position + Direction, up);
        Matrix4 projMatrix = Matrix4.CreateOrthographic(orthoWidth,orthoHeight, clipNear, clipFar);
        ViewSpaceMatrix = viewMatrix * projMatrix;

        UniformMatrix(Shader, "lightSpaceMatrix");
        
        return this;
    }

    public DepthMap ProjectPerspective(float fieldOfView = MathHelper.PiOver3, float clipNear = 0.1f, float clipFar = 100f, Vector3 up = default)
    {
        if (up == default) up = Vector3.UnitY;
        Matrix4 viewMatrix = Matrix4.LookAt(Position, Position + Direction, up);
        Matrix4 projMatrix = Matrix4.CreatePerspectiveFieldOfView(fieldOfView, (float) Size.X / Size.Y, clipNear, clipFar);
        ViewSpaceMatrix = viewMatrix * projMatrix;
        
        return this;
    }

    public DepthMap UniformMatrix(ShaderProgram shaderProgram, string name)
    {
        shaderProgram.Use();
        GL.UniformMatrix4(GL.GetUniformLocation((int)shaderProgram,name),false, ref ViewSpaceMatrix);
        return this;
    }


    public DepthMap UniformTexture(string name, ShaderProgram shaderProgram, int textureUnit = 0)
    {
        shaderProgram.Use();
        GL.ActiveTexture(TextureUnit.Texture0 + textureUnit);
        GL.BindTexture(TextureTarget.Texture2D,TextureHandle);
        shaderProgram.Uniform1(name, textureUnit);
        return this;
    }

    public void Dispose()
    {
        GL.DeleteTexture(TextureHandle);
        GL.DeleteFramebuffer(Handle);
        Shader.Dispose();
    }

}


public class CubeDepthMap : IDisposable
{
    public readonly ShaderProgram Shader;
    
    public readonly int Handle;
    public readonly int TextureHandle;
    private int textureUnit;

    public readonly Vector2i Size;
    
    public Matrix4[] ViewSpaceMatrices;
    public Vector3 Position;

    public float ClipNear;
    public float ClipFar;


    public CubeDepthMap(Vector2i size, Vector3 position, float clipNear = 0.05f, float clipFar = 100f)
    {
        Handle = GL.GenFramebuffer();
        TextureHandle = GL.GenTexture();
        Size = size;
        Position = position;
        ViewSpaceMatrices = new Matrix4[6];
        ClipNear = clipNear;
        ClipFar = clipFar;
        
        GL.BindTexture(TextureTarget.TextureCubeMap,TextureHandle);
        for (int i = 0; i < 6; i++)
        {
            GL.TexImage2D(TextureTarget.TextureCubeMapPositiveX + i,
                0, PixelInternalFormat.DepthComponent,
                Size.X,Size.Y,0,
                PixelFormat.DepthComponent,PixelType.Float,IntPtr.Zero);
        }

        GL.TexParameter(TextureTarget.TextureCubeMap,TextureParameterName.TextureMagFilter,(int)TextureMagFilter.Nearest);
        GL.TexParameter(TextureTarget.TextureCubeMap,TextureParameterName.TextureMinFilter,(int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.TextureCubeMap,TextureParameterName.TextureWrapS,(int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.TextureCubeMap,TextureParameterName.TextureWrapT,(int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.TextureCubeMap,TextureParameterName.TextureWrapR,(int)TextureWrapMode.ClampToEdge);
        
        GL.BindFramebuffer(FramebufferTarget.Framebuffer,Handle);
        GL.FramebufferTexture(FramebufferTarget.Framebuffer,FramebufferAttachment.DepthAttachment,TextureHandle,0);
        GL.DrawBuffer(DrawBufferMode.None);
        GL.ReadBuffer(ReadBufferMode.None);

        GL.BindFramebuffer(FramebufferTarget.Framebuffer,0);

        Shader = new ShaderProgram()
            .LoadShader(Constants.LibraryShaderPath + "DepthMap/vertexCubeMap.glsl", ShaderType.VertexShader)
            .LoadShader(Constants.LibraryShaderPath + "DepthMap/geometryCubeMap.glsl", ShaderType.GeometryShader)
            .LoadShader(Constants.LibraryShaderPath + "DepthMap/fragmentCubeMap.glsl", ShaderType.FragmentShader)
            .Compile()
            .SetModelLocation("model");
    }

    public CubeDepthMap DrawMode(int x = 0, int y = 0, int width = 0, int height = 0)
    {
        if (width == 0) width = Size.X;
        if (height == 0) height = Size.Y;
        
        GL.Disable(EnableCap.CullFace);
        GL.Viewport(x,y,width,height);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer,Handle);
        GL.Clear(ClearBufferMask.DepthBufferBit);
        
        Shader.Use();
        Shader.Uniform3("offsetPos",Position);

        return this;
    }

    public CubeDepthMap ReadMode()
    {
        GL.BindFramebuffer(FramebufferTarget.Framebuffer,0);
        return this;
    }
    

    public CubeDepthMap UpdateMatrices(Vector3[] directions = default, Vector3[] upDirections = default)
    {
        if (directions == default)
        {
            directions = new[]
            {
                Vector3.UnitX,
                -Vector3.UnitX,
                Vector3.UnitY,
                -Vector3.UnitY,
                Vector3.UnitZ,
                -Vector3.UnitZ
            };
        }
        if (upDirections == default)
        {
            upDirections = new[]
            {
                -Vector3.UnitY,
                -Vector3.UnitY,
                Vector3.UnitZ,
                -Vector3.UnitZ,
                -Vector3.UnitY,
                -Vector3.UnitY
            };
        }
        
        var proj = Matrix4.CreatePerspectiveFieldOfView(MathHelper.PiOver2, (float)Size.X/Size.Y, ClipNear, ClipFar);
        for (int i = 0; i < 6; i++) ViewSpaceMatrices[i] = Matrix4.LookAt(Vector3.Zero, directions[i], upDirections[i]) * proj;

        UniformMatrices(Shader, "shadowMatrices");
        UniformClipFar(Shader, "farPlane");

        return this;
    }

    public CubeDepthMap UniformMatrices(ShaderProgram shaderProgram, string name)
    {
        shaderProgram.UniformMat4Array(name, ref ViewSpaceMatrices);
        return this;
    }


    public CubeDepthMap UniformTexture(ShaderProgram shaderProgram, string name, int unit = 0)
    {
        textureUnit = unit;
        shaderProgram.Use();
        GL.ActiveTexture(TextureUnit.Texture0 + textureUnit);
        GL.BindTexture(TextureTarget.TextureCubeMap,TextureHandle);
        shaderProgram.Uniform1(name, textureUnit);
        return this;
    }

    public CubeDepthMap UniformClipFar(ShaderProgram shaderProgram, string name)
    {
        shaderProgram.Use();
        shaderProgram.Uniform1(name, ClipFar);
        return this;
    }

    public CubeDepthMap UseTexture()
    {
        GL.ActiveTexture(TextureUnit.Texture0 + textureUnit);
        GL.BindTexture(TextureTarget.TextureCubeMap,TextureHandle);
        return this;
    }

    public void Dispose()
    {
        GL.DeleteTexture(TextureHandle);
        GL.DeleteFramebuffer(Handle);
        Shader.Dispose();
    }

}