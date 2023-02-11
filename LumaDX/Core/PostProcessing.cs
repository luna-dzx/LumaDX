using System.Drawing;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace LumaDX;

public class PostProcessing : IDisposable
{
    public static void Draw() => GL.DrawArrays(PrimitiveType.Triangles, 0, 3);

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

    struct MatrixCharacter
    {
        public char Character;
        public int RandomizeSpeed;
    }

    private const int matrixColumns = 160;
    private const int matrixRows = 90;
    private const float matrixTextSize = 0.5f;

    private TextRenderer textRenderer;
    private MatrixCharacter[,] matrixTextArray;
    private int[] matrixColumnSpeed;
    
    private Random rand;

    private static readonly DrawBuffersEnum[] DefaultAttachments = {DrawBuffersEnum.ColorAttachment0};

    public PostProcessing(PostProcessShader postProcessEffects, Vector2i frameBufferSize, PixelInternalFormat internalFormat = PixelInternalFormat.Rgba8, DrawBuffersEnum[]? colourAttachments = null, string fontFile = "")
    {
        colourAttachments ??= DefaultAttachments;

        if (fontFile != "")
        {
            textRenderer = new TextRenderer(20, frameBufferSize, fontFile, Enumerable.Range('゠', 'ヺ').Concat(new[] {(int)' '}));
            matrixTextArray = new MatrixCharacter[matrixColumns, matrixRows];
            matrixColumnSpeed = new int[matrixColumns];

            rand = new Random(1);
            
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
        
        WriteFbo = new FrameBuffer(frameBufferSize,internalFormat: internalFormat,numColourAttachments:colourAttachments.Length);
        ReadFbo = new FrameBuffer(frameBufferSize,internalFormat: internalFormat,numColourAttachments:colourAttachments.Length);
        
        shaderPrograms = new Dictionary<PostProcessShader, ShaderProgram>();
        
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
    }
    
    
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
    

    private enum BlurDirection
    {
        Horizontal,
        Vertical
    }

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

    public PostProcessing RenderEffect(PostProcessShader effect, DrawBuffersEnum[]? colourAttachments = null)
    {
        colourAttachments ??= DefaultAttachments; 
        
        if ((effect & PostProcessShader.GaussianBlur) != 0)
        {
            shaderPrograms[PostProcessShader.GaussianBlur].Use();

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


    public PostProcessing ReadTexture(int unit = -1)
    {
        ReadFbo.UseTexture(unit);
        return this;
    }


    public PostProcessing StartSceneRender(DrawBuffersEnum[]? colourAttachments = null)
    {
        if (colourAttachments != null) { WriteFbo.SetDrawBuffers(colourAttachments); }
        WriteFbo.WriteMode();
        return this;
    }
    public PostProcessing EndSceneRender()
    {
        WriteFbo.ReadMode();
        BlitFbo();
        return this;
    }
    

    public PostProcessing UniformTexture(ShaderProgram program, string name, int binding)
    {
        WriteFbo.UniformTexture((int)program, name, binding);
        return this;
    }
    
    public PostProcessing UniformTextures(ShaderProgram program, string[] names)
    {
        for (int i = 0; i < names.Length; i++)
        {
            WriteFbo.UniformTexture((int)program, names[i], i);
        }
        
        return this;
    }
    
    

    public void Dispose()
    {
        ReadFbo.Dispose();
        WriteFbo.Dispose();
        
        foreach (var program in shaderPrograms.Values)
        {
            program.Dispose();
        }
        
        textRenderer.Dispose();
        
        BlitShader.Dispose();
        
    }
}