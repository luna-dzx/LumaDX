using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace LumaDX;

/// <summary>
/// Off screen textures which can be rendered to
/// </summary>
public class FrameBuffer : IDisposable
{
    private int handle = -1;
    protected bool usingPreset = false;
    public int NumColourAttachments;
    protected TextureBuffer[] colourAttachments;
    protected TextureBuffer depthStencilAttachment;
    protected RenderBuffer depthStencilRenderBuffer;
    protected bool usingRenderBuffer = true;
    public Vector2i Size;

    /// <summary>
    /// Initialize OpenGL object
    /// </summary>
    public FrameBuffer()
    {
        handle = GL.GenFramebuffer();
    }

    /// <summary>
    /// Most general case of creating a standard FBO with the option of multiple colour attachments
    /// </summary>
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
    /// Special case of loading an image directly to an FBO
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
    
    /// <summary>
    /// Create a common FBO with n textures and a render buffer bound to the depth/stencil buffer
    /// </summary>
    /// <param name="size"></param>
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


    /// <summary>
    /// Bind the FBOs texture(/s) based on colour attachment index
    /// </summary>
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

    /// <summary>
    /// Link the FBOs texture to a shader given the shader's OpenGL ID
    /// </summary>
    public FrameBuffer UniformTexture(int shader, string name, int unit)
    {
        GL.UseProgram(shader);
        UseTexture();
        GL.Uniform1(GL.GetUniformLocation(shader,name),unit);
        return this;
    }
    
    /// <summary>
    /// Link the FBOs texture to a shader given a ShaderProgram object
    /// </summary>
    public FrameBuffer UniformTexture(ShaderProgram shaderProgram, string name, int textureUnit = 0)
    {
        shaderProgram.Use();
        GL.ActiveTexture(TextureUnit.Texture0 + textureUnit);
        GL.BindTexture(TextureTarget.Texture2D,colourAttachments[0].Handle);
        shaderProgram.Uniform1(name, textureUnit);
        return this;
    }


    /// <summary>
    /// Does nothing if complete, throws error if incomplete (incomplete FBOs cause hard to debug errors later on)
    /// </summary>
    public void CheckCompletion()
    {
        var status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
        if (status != FramebufferErrorCode.FramebufferComplete)
        {
            throw new Exception("Incomplete Fbo -> " + status);
        }
    }

    /// <summary>
    /// Attach an existing OpenGL texture to this FBO before it is finalized
    /// </summary>
    public FrameBuffer AttachTexture(TextureTarget textureTarget, int textureHandle, FramebufferAttachment attachment,
        FramebufferTarget target = FramebufferTarget.Framebuffer, int mipmap = 0)
    {
        WriteMode();
        GL.FramebufferTexture2D(target,attachment,textureTarget,textureHandle,mipmap);
        ReadMode();
        return this;
    }

    /// <summary>
    /// Attach an existing texture buffer object to this FBO before it is finalized
    /// </summary>
    public FrameBuffer AttachTexture(TextureBuffer textureBuffer, FramebufferAttachment attachment,
        FramebufferTarget target = FramebufferTarget.Framebuffer, int mipmap = 0)
    {
        return AttachTexture(textureBuffer.Target, textureBuffer.Handle,attachment,target,mipmap);
    }

    /// <summary>
    /// Attach an existing OpenGL render buffer to this FBO before it is finalized
    /// </summary>
    public FrameBuffer AttachRenderBuffer(RenderbufferTarget renderBufferTarget, int renderBufferHandle, FramebufferAttachment attachment, FramebufferTarget target = FramebufferTarget.Framebuffer)
    {
        WriteMode();
        GL.FramebufferRenderbuffer(target,attachment,renderBufferTarget,renderBufferHandle);
        ReadMode();
        return this;
    }

    /// <summary>
    /// Attach an existing render buffer object to this FBO before it is finalized
    /// </summary>
    public FrameBuffer AttachRenderBuffer(RenderBuffer renderBuffer,
        FramebufferAttachment attachment, FramebufferTarget target = FramebufferTarget.Framebuffer)
    {
        return AttachRenderBuffer(renderBuffer.Target, renderBuffer.Handle, attachment, target);
    }

    /// <summary>
    /// After calling this function, subsequent renders will render to this FBOs texture(/s)
    /// </summary>
    public FrameBuffer WriteMode()
    {
        GL.BindFramebuffer(FramebufferTarget.Framebuffer,handle);
        return this;
    }
    
    /// <summary>
    /// After calling this function, subsequent renders will render to the actual display
    /// </summary>
    public FrameBuffer ReadMode()
    {
        GL.BindFramebuffer(FramebufferTarget.Framebuffer,0);
        return this;
    }

    /// <summary>
    /// Define which draw buffers this FBO is to use in a render (for multiple textures, which textures to render to)
    /// </summary>
    public FrameBuffer SetDrawBuffers(DrawBuffersEnum[] colourAttachments)
    {
        this.WriteMode();
        GL.DrawBuffers(colourAttachments.Length,colourAttachments);
        return this;
    }
    
    /// <summary>
    /// Clear the resources used by this FBO from the GPU
    /// </summary>
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
    
    /// <summary>
    /// Return the OpenGL handle upon casting to an integer
    /// </summary>
    public static explicit operator int(FrameBuffer fbo) => fbo.handle;


    /// <summary>
    /// Transfer the FBOs depth to the screen's depth
    /// </summary>
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
    
    /// <summary>
    /// Transfer the FBOs depth to another FBO
    /// </summary>
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
/// Different way of constructing FBOs for complex combinations of 2D textures and one depth/stencil attachment
/// </summary>
public class GeometryBuffer : FrameBuffer
{
    private List<TextureBuffer> colourAttachmentsList;
    private DrawBuffersEnum[] drawBuffers;

    /// <summary>
    /// Initialize a new Geometry Buffer
    /// </summary>
    /// <remarks>All FBOs must have a depth attachment, which in this case isn't usually very useful</remarks>
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

    /// <summary>
    /// Set the current outputs to this FBOs draw buffers
    /// </summary>
    public FrameBuffer SetDrawBuffers()
    {
        this.WriteMode();
        GL.DrawBuffers(drawBuffers.Length,drawBuffers);
        return this;
    }
    
    /// <summary>
    /// Add texture to FBO outputs (e.g. position data)
    /// </summary>
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

    /// <summary>
    /// Actually create the FBO after initial settings
    /// </summary>
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


/// <summary>
/// FBO for sampling only the depth of the scene
/// </summary>
public class DepthMap : IDisposable
{

    public readonly ShaderProgram Shader;
    
    public readonly int Handle;
    public readonly int TextureHandle;

    public readonly Vector2i Size;
    
    public Matrix4 ViewSpaceMatrix;
    
    public Vector3 Position;
    public Vector3 Direction;

    /// <summary>
    /// Standard initialization given the texture size and camera position/direction
    /// </summary>
    public DepthMap(Vector2i size, Vector3 position, Vector3 direction) : this(Constants.LibraryShaderPath + "DepthMap/",size,position,direction) { }

    /// <summary>
    /// Custom shader program initialization
    /// </summary>
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

    /// <summary>
    /// After calling this function, subsequent renders will render to this FBOs texture(/s)
    /// </summary>
    public DepthMap WriteMode(int x = 0, int y = 0, int width = 0, int height = 0, CullFaceMode cullFaceMode = CullFaceMode.Front)
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

    /// <summary>
    /// After calling this function, subsequent renders will render to the actual display
    /// </summary>
    public DepthMap ReadMode()
    {
        GL.BindFramebuffer(FramebufferTarget.Framebuffer,0);
        return this;
    }

    /// <summary>
    /// Set the orthographic projection matrix in the depth map's shader
    /// </summary>
    public DepthMap ProjectOrthographic(float orthoWidth = 24f, float orthoHeight = 24f, float clipNear = 0.05f, float clipFar = 50f, Vector3 up = default)
    {
        if (up == default) up = Vector3.UnitY;
        Matrix4 viewMatrix = Matrix4.LookAt(Position, Position + Direction, up);
        Matrix4 projMatrix = Matrix4.CreateOrthographic(orthoWidth,orthoHeight, clipNear, clipFar);
        ViewSpaceMatrix = viewMatrix * projMatrix;

        UniformMatrix(Shader, "lightSpaceMatrix");
        
        return this;
    }

    /// <summary>
    /// Set the perspective projection matrix in the depth map's shader
    /// </summary>
    public DepthMap ProjectPerspective(float fieldOfView = MathHelper.PiOver3, float clipNear = 0.1f, float clipFar = 100f, Vector3 up = default)
    {
        if (up == default) up = Vector3.UnitY;
        Matrix4 viewMatrix = Matrix4.LookAt(Position, Position + Direction, up);
        Matrix4 projMatrix = Matrix4.CreatePerspectiveFieldOfView(fieldOfView, (float) Size.X / Size.Y, clipNear, clipFar);
        ViewSpaceMatrix = viewMatrix * projMatrix;
        
        return this;
    }

    /// <summary>
    /// Load the depth map's view space matrix to an external shader program
    /// </summary>
    public DepthMap UniformMatrix(ShaderProgram shaderProgram, string name)
    {
        shaderProgram.Use();
        GL.UniformMatrix4(GL.GetUniformLocation((int)shaderProgram,name),false, ref ViewSpaceMatrix);
        return this;
    }
    
    /// <summary>
    /// Link the FBOs texture to a shader given a ShaderProgram object
    /// </summary>
    public DepthMap UniformTexture(ShaderProgram shaderProgram, string name, int textureUnit = 0)
    {
        shaderProgram.Use();
        GL.ActiveTexture(TextureUnit.Texture0 + textureUnit);
        GL.BindTexture(TextureTarget.Texture2D,TextureHandle);
        shaderProgram.Uniform1(name, textureUnit);
        return this;
    }

    /// <summary>
    /// Clear the resources used by this FBO from the GPU
    /// </summary>
    public void Dispose()
    {
        GL.DeleteTexture(TextureHandle);
        GL.DeleteFramebuffer(Handle);
        Shader.Dispose();
    }

}

/// <summary>
/// FBO for sampling only the depth of the scene in all 6 directions (of a cube)
/// </summary>
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


    /// <summary>
    /// Standard initialization given the texture size of one surface of the cube map, and camera position/direction
    /// </summary>
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

    /// <summary>
    /// After calling this function, subsequent renders will render to this FBOs texture(/s)
    /// </summary>
    public CubeDepthMap WriteMode(int x = 0, int y = 0, int width = 0, int height = 0)
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

    /// <summary>
    /// After calling this function, subsequent renders will render to the actual display
    /// </summary>
    public CubeDepthMap ReadMode()
    {
        GL.BindFramebuffer(FramebufferTarget.Framebuffer,0);
        return this;
    }
    
    /// <summary>
    /// Update 6 matrices to the gpu for the 6 directions of a cube for sampling to the cube map
    /// </summary>
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

    /// <summary>
    /// Load the 6 matrices for the depth map 
    /// </summary>
    public CubeDepthMap UniformMatrices(ShaderProgram shaderProgram, string name)
    {
        shaderProgram.UniformMat4Array(name, ref ViewSpaceMatrices);
        return this;
    }
    
    /// <summary>
    /// Link the FBOs cube sampler to a shader given a ShaderProgram object
    /// </summary>
    public CubeDepthMap UniformTexture(ShaderProgram shaderProgram, string name, int unit = 0)
    {
        textureUnit = unit;
        shaderProgram.Use();
        GL.ActiveTexture(TextureUnit.Texture0 + textureUnit);
        GL.BindTexture(TextureTarget.TextureCubeMap,TextureHandle);
        shaderProgram.Uniform1(name, textureUnit);
        return this;
    }

    /// <summary>
    /// Link the FBOs clip distance to a shader given a ShaderProgram object
    /// </summary>
    public CubeDepthMap UniformClipFar(ShaderProgram shaderProgram, string name)
    {
        shaderProgram.Use();
        shaderProgram.Uniform1(name, ClipFar);
        return this;
    }

    /// <summary>
    /// Bind the FBOs texture(/s) based on colour attachment index
    /// </summary>
    public CubeDepthMap UseTexture()
    {
        GL.ActiveTexture(TextureUnit.Texture0 + textureUnit);
        GL.BindTexture(TextureTarget.TextureCubeMap,TextureHandle);
        return this;
    }

    /// <summary>
    /// Clear the resources used by this FBO from the GPU
    /// </summary>
    public void Dispose()
    {
        GL.DeleteTexture(TextureHandle);
        GL.DeleteFramebuffer(Handle);
        Shader.Dispose();
    }

}