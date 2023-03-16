using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace LumaDX;

/// <summary>
/// A handler for infinitely many consecutive Post Processing effects using 2 FBOs (one which the user reads from and one which the user writes to) which work in tandem
/// </summary>
public class PostProcessing : IDisposable
{
    /// <summary>
    /// Draw a single triangle with no data (data to be constructed on the gpu to make the triangle cover the entire screen for whole screen pixel effects)
    /// </summary>
    public static void Draw() => GL.DrawArrays(PrimitiveType.Triangles, 0, 3);

    /// <summary>
    /// Render this texture to another FBO or the screen (e.g. after an effect has been rendered)
    /// </summary>
    public void DrawFbo() { ReadTexture(); Draw(); }

    [Flags]
    public enum PostProcessShader
    {
        GaussianBlur = 1,
        GreyScale = 2,
        NightVision = 4,
        MatrixText = 8
    }

    public Dictionary<PostProcessShader,ShaderProgram> shaderPrograms;

    public ShaderProgram BlitShader;

    public FrameBuffer WriteFbo;
    public FrameBuffer ReadFbo;

    /// <summary>
    /// Dynamically create a shader based on the number of colour attachments used in the post processor
    /// </summary>
    private string GenerateBlitShader(int numAttachments)
    {
        string output =
            "#version 330 core\n";

        for (int i = 0; i < numAttachments; i++)
        {
            output += "layout (location = "+i+") out vec4 fragColour"+i+";\n";
        }
        
        for (int i = 0; i < numAttachments; i++)
        {
            output += "uniform sampler2D texture"+i+";\n";
        }

        output +=
            "in vec2 texCoords;\n" +

            "void main()\n" +
            "{\n";
        
        for (int i = 0; i < numAttachments; i++)
        {
            output += "fragColour"+i+" = texture(texture"+i+",texCoords);\n";
        }
        
        output += "}\n";
        
        Console.WriteLine(output);

        return output;
    }

    /// <summary>
    /// For the Matrix text effect, each character has a duration of frames before it is randomized, which is stored here
    /// </summary>
    struct MatrixCharacter
    {
        public char Character;
        public int RandomizeSpeed;
    }

    // these numbers are fixed as the Matrix effect rendering is tuned for optimal performance - they shouldn't require adjusting
    private const int matrixColumns = 160;
    private const int matrixRows = 90;
    private const float matrixTextSize = 0.5f;

    private TextRenderer textRenderer;
    private MatrixCharacter[,] matrixTextArray;
    private int[] matrixColumnSpeed;
    
    private Random rand;

    private static readonly DrawBuffersEnum[] DefaultAttachments = {DrawBuffersEnum.ColorAttachment0};

    /// <summary>
    /// Initialize the Post Processor with the effects that are to be loaded, the size and format of its textures, and optionally a font file for rendering the Matrix effect
    /// </summary>
    public PostProcessing(PostProcessShader postProcessEffects, Vector2i frameBufferSize, PixelInternalFormat internalFormat = PixelInternalFormat.Rgba8, DrawBuffersEnum[]? colourAttachments = null, string fontFile = "")
    {
        colourAttachments ??= DefaultAttachments;

        if (fontFile != "")
        {
            // cache the rendered textures of the characters used in the matrix text effect
            textRenderer = new TextRenderer(20, frameBufferSize, fontFile, Enumerable.Range('゠', 'ヺ').Concat(new[] {(int)' '}));
            matrixTextArray = new MatrixCharacter[matrixColumns, matrixRows];
            matrixColumnSpeed = new int[matrixColumns];

            rand = new Random(1);
            
            // randomise data required by matrix effect
            for (int i = 0; i < matrixColumns; i++)
            {
                for (int j = 0; j < matrixRows; j++)
                {
                    MatrixCharacter mChar = new MatrixCharacter();
                    
                    if (rand.NextDouble() > 0.11) mChar.Character = (char)rand.Next('゠', 'ヺ');
                    else mChar.Character = ' ';
                    
                    mChar.RandomizeSpeed = rand.Next(3,10);

                    matrixTextArray[i, j] = mChar;
                }
                
                matrixColumnSpeed[i] = rand.Next(2,5);
            }
            
        }
        
        // create 2 FBOs for the post processing pipeline
        WriteFbo = new FrameBuffer(frameBufferSize,internalFormat: internalFormat,numColourAttachments:colourAttachments.Length);
        ReadFbo = new FrameBuffer(frameBufferSize,internalFormat: internalFormat,numColourAttachments:colourAttachments.Length);
        
        shaderPrograms = new Dictionary<PostProcessShader, ShaderProgram>();
        
        // generate a custom shader based on the number of colour attachments
        BlitShader = new ShaderProgram()
            .LoadPostProcessVertex()
            .LoadShaderText(GenerateBlitShader(colourAttachments.Length),ShaderType.FragmentShader)
            .Compile();

        // loop through post processing effects
        foreach (var postProcessShader in Enum.GetValues(typeof(PostProcessShader)).Cast<PostProcessShader>())
        {
            // if "postProcessEffects" has this effect
            if ((postProcessEffects & postProcessShader) != 0)
            {
                // add to dictionary of effects
                shaderPrograms[postProcessShader] = new ShaderProgram
                (
                    Constants.LibraryShaderPath + "PostProcessing/fx"+postProcessShader+".glsl"
                );
                
                WriteFbo.UniformTexture((int)shaderPrograms[postProcessShader], "texture0", 0);
            }
        }
        

        for (int i = 0; i < colourAttachments.Length; i++)
        {
            WriteFbo.UniformTexture((int)BlitShader, "texture"+i, i);
        }
        
        ReadFbo.SetDrawBuffers(colourAttachments);
        WriteFbo.SetDrawBuffers(colourAttachments);
        
        
        EndSceneRender();
    }
    
    /// <summary>
    /// Blit the data from the write FBO to the read FBO
    /// </summary>
    public PostProcessing BlitFbo()
    {
        BlitShader.Use();
        ReadFbo.WriteMode();
        
        GL.Clear(ClearBufferMask.ColorBufferBit);
        
        WriteFbo.UseTexture();
        Draw();
        
        ReadFbo.ReadMode();

        return this;
    }
    
    /// <summary>
    /// Enum for readability of gaussian blur calls (gaussian blur takes 2 stages of blurring, horizontal then vertical)
    /// </summary>
    private enum BlurDirection
    {
        Horizontal,
        Vertical
    }
    
    /// <summary>
    /// Most effects follow the same render format given a certain shader, this is just to prevent repeating code
    /// </summary>
    private void BasicEffect(DrawBuffersEnum[] colourAttachments)
    {
        ReadFbo.ReadMode();
        
        WriteFbo.WriteMode();
        WriteFbo.SetDrawBuffers(colourAttachments);

        GL.Clear(ClearBufferMask.ColorBufferBit);

        ReadFbo.UseTexture();
        Draw();
        
        WriteFbo.ReadMode();


        BlitFbo();
    }
    
    /// <summary>
    /// One blur stage of the 2-part gaussian blur effect
    /// </summary>
    private void GaussianBlurStep(DrawBuffersEnum[] colourAttachments, BlurDirection direction)
    {
        shaderPrograms[PostProcessShader.GaussianBlur].Uniform1("blurDirection", (int)direction);
        WriteFbo.WriteMode();
        WriteFbo.SetDrawBuffers(colourAttachments);
        GL.Clear(ClearBufferMask.ColorBufferBit);
        
        ReadFbo.UseTexture();
        Draw();
        
        WriteFbo.ReadMode();

        BlitFbo();
    }
    
    /// <summary>
    /// Convert screen colours to randomized characters which seem to be raining down
    /// </summary>
    private void MatrixTextEffect(DrawBuffersEnum[] colourAttachments)
    {
        ReadFbo.WriteMode();
        
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        ReadFbo.ReadMode();
        
        WriteFbo.WriteMode();
        WriteFbo.SetDrawBuffers(colourAttachments);
        
        GL.Clear(ClearBufferMask.ColorBufferBit);

        string thing = "";
        for (int j = 0; j < matrixRows; j++)
        {
            for (int i = 0; i < matrixColumns; i++)
            {
                char c = matrixTextArray[i, j].Character;
                if (c != ' ') thing += c;
                else thing += "  "; // 1 space is half a character, so replace ' ' with "  "
            }

            thing += "\n";
        }

        textRenderer.Draw(thing, 0,0,matrixTextSize, Vector3.UnitY, false);
        
        
        GL.BlendFunc(BlendingFactor.DstColor, BlendingFactor.SrcAlpha);
        shaderPrograms[PostProcessShader.MatrixText].Use();

        ReadFbo.UseTexture();
        Draw();



        WriteFbo.ReadMode();
        
        GL.Disable(EnableCap.Blend);


        BlitFbo();
    }


    int frameCount = 0;
    public int BlurTexture = 0;

    /// <summary>
    /// After rendering the scene to the post processor's framebuffer, render the desired post processing effect here
    /// </summary>
    public PostProcessing RenderEffect(PostProcessShader effect, DrawBuffersEnum[]? colourAttachments = null)
    {
        colourAttachments ??= DefaultAttachments; 
        
        if ((effect & PostProcessShader.GaussianBlur) != 0)
        {
            shaderPrograms[PostProcessShader.GaussianBlur].Use();

            shaderPrograms[PostProcessShader.GaussianBlur].Uniform1("texture0", BlurTexture);

            shaderPrograms[PostProcessShader.GaussianBlur].Uniform1("blurDirection", (int)BlurDirection.Horizontal);
            BasicEffect(colourAttachments);
            shaderPrograms[PostProcessShader.GaussianBlur].Uniform1("blurDirection", (int)BlurDirection.Vertical);
            BasicEffect(colourAttachments);
        }
        if ((effect & PostProcessShader.GreyScale) != 0)
        {
            shaderPrograms[PostProcessShader.GreyScale].Use();
            BasicEffect(colourAttachments);
        }
        if ((effect & PostProcessShader.NightVision) != 0)
        {
            shaderPrograms[PostProcessShader.NightVision].Use();
            
            frameCount++;
            shaderPrograms[PostProcessShader.NightVision].Uniform1("time", (float)frameCount);
            BasicEffect(colourAttachments);
        }
        if ((effect & PostProcessShader.MatrixText) != 0)
        {
            for (int i = 0; i < matrixColumns; i++)
            {
                for (int j = 0; j < matrixRows; j++)
                {
                    if (matrixTextArray[i, j].RandomizeSpeed <= 0)
                    {
                        if (rand.NextDouble() > 0.11) matrixTextArray[i,j].Character = (char)rand.Next('゠', 'ヺ');
                        else matrixTextArray[i,j].Character = ' ';
                        
                        matrixTextArray[i, j].RandomizeSpeed = rand.Next(3,10);
                    }

                    matrixTextArray[i, j].RandomizeSpeed--;
                }
            }

            var matrixTextCopy = matrixTextArray.Clone() as MatrixCharacter[,];
            

            for (int i = 0; i < matrixColumns; i++)
            {
                if (matrixColumnSpeed[i] <= 0)
                {
                    for (int j = 0; j < matrixRows; j++)
                    {
                        int k = j + 1;
                        if (k >= matrixRows) k -= matrixRows;
                        matrixTextArray[i, j] = matrixTextCopy[i, k];
                    }
                    
                    matrixColumnSpeed[i] = rand.Next(2,5);
                }

                matrixColumnSpeed[i]--;
            }


            shaderPrograms[PostProcessShader.MatrixText].Use();
            shaderPrograms[PostProcessShader.MatrixText].Uniform2("pixelateResolution",new Vector2(matrixColumns,matrixRows));
            MatrixTextEffect(colourAttachments);
        }


        return this;
    }


    /// <summary>
    /// Bind the read FBOs texture (/ image data) to a specific OpenGL texture unit
    /// </summary>
    public PostProcessing ReadTexture(int unit = -1)
    {
        ReadFbo.UseTexture(unit);
        return this;
    }

    /// <summary>
    /// After calling this function, subsequent renders will render to this FBOs texture(/s)
    /// </summary>
    public PostProcessing StartSceneRender(DrawBuffersEnum[]? colourAttachments = null)
    {
        if (colourAttachments != null) { WriteFbo.SetDrawBuffers(colourAttachments); }
        WriteFbo.WriteMode();
        return this;
    }
    
    /// <summary>
    /// After calling this function, subsequent renders will render to the actual display,
    /// and the render of the scene is ready for post processing effects
    /// </summary>
    public PostProcessing EndSceneRender()
    {
        WriteFbo.ReadMode();
        BlitFbo();
        return this;
    }
    
    /// <summary>
    /// Link the FBOs texture to a shader given a ShaderProgram object
    /// </summary>
    public PostProcessing UniformTexture(ShaderProgram program, string name, int binding)
    {
        WriteFbo.UniformTexture((int)program, name, binding);
        return this;
    }
    
    /// <summary>
    /// Link the FBOs textures to a shader given a ShaderProgram object
    /// </summary>
    public PostProcessing UniformTextures(ShaderProgram program, string[] names)
    {
        for (int i = 0; i < names.Length; i++)
        {
            WriteFbo.UniformTexture((int)program, names[i], i);
        }
        
        return this;
    }
    
    
    /// <summary>
    /// Clear the resources used by the FBOs and any effects from the GPU
    /// </summary>
    public void Dispose()
    {
        ReadFbo.Dispose();
        WriteFbo.Dispose();
        
        foreach (var program in shaderPrograms.Values)
        {
            program.Dispose();
        }
        
        if (textRenderer!=null) textRenderer.Dispose();
        
        BlitShader.Dispose();
        
    }
}