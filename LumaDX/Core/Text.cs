using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using SharpFont;

namespace LumaDX;

public class TextRenderer
{
    private VertexArray vao;
    private Dictionary<char, GlChar> GlChars;
    public uint FontSize = 48;
    
    ShaderProgram textShader;

    public TextRenderer(uint fontSize, Vector2i screenSize)
    {
        FontSize = fontSize;
        GlChars = new Dictionary<char, GlChar>();

        textShader = new ShaderProgram(
            Constants.LibraryShaderPath+"Text/vertex.glsl",
            Constants.LibraryShaderPath+"Text/fragment.glsl",
            true).Use();
        
        textShader.Uniform2("screenSize", new Vector2(screenSize.X,screenSize.Y));
        
        
        Library ft = new Library();
        Face face = new Face(ft, "Assets/fonts/IBMPlexSans-Regular.ttf", 0);
        face.SetPixelSizes(0,FontSize); // set height to FontSize and allow the width to automatically calculate
        
        // disable 4-byte alignment (allow data to be stored in blocks of 1 byte instead of 4 (single byte pixels))
        GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1);

        Texture texture;
        // this loop covers all standard characters apart from control characters, new line, etc.
        for (char c = ' '; c < '~'; c++)
        {
            face.LoadChar(c, LoadFlags.Render, LoadTarget.Normal);
            
            texture = new Texture(0)
                // load texture into 8bit red channel for greyscale text (can be coloured later but is stored this way)
                .LoadPtr(face.Glyph.Bitmap.Buffer, face.Glyph.Bitmap.Width, face.Glyph.Bitmap.Rows,
                    PixelInternalFormat.R8, PixelFormat.Red)
                .Wrapping(TextureWrapMode.ClampToEdge)
                .Filter(Texture.TextureFilter.Linear);

            GlChar glChar = new GlChar(
                texture.GetHandle(),
                new Vector2i(face.Glyph.Bitmap.Width,face.Glyph.Bitmap.Rows),
                new Vector2i(face.Glyph.BitmapLeft,face.Glyph.BitmapTop),
                face.Glyph.Advance.X.Value
            );
            
            GlChars.Add(c,glChar);
        }
        
        ft.Dispose();
        
        // reset to default
        GL.PixelStore(PixelStoreParameter.UnpackAlignment, 4);
        
        
        vao = new VertexArray(BufferUsageHint.DynamicDraw);
        vao.EmptyBuffer(sizeof(float) * 24, BufferTarget.ArrayBuffer);
        vao.SetupBuffer(0,typeof(float),4,4);
    }
    
    struct GlChar
    {
        public int TextureId;
        public Vector2i Size;
        public Vector2i Bearing;   // offset from main line to character left and top
        public int Advance;       // spacing to next Glyph

        public GlChar(int textureId, Vector2i size, Vector2i bearing, int advance)
        {
            TextureId = textureId;
            Size = size;
            Bearing = bearing;
            Advance = advance;
        }
    }

    public void Draw(string text, float x, float y, float scale, Vector3 colour, bool centered = true)
    {
        textShader.Use();
        textShader.Uniform3("textColour", colour);
        GL.ActiveTexture(TextureUnit.Texture0);
        vao.Use();

        if (centered) x -= text.Sum(t => (GlChars[t].Advance >> 6) * scale) / 2f;

        foreach (char c in text)
        {
            GlChar glChar = GlChars[c];

            float xpos = x + glChar.Bearing.X * scale; // current x pos + distance to left side of character
            float ypos = y - (glChar.Size.Y - glChar.Bearing.Y) * scale; // bearing is from the line to the top, so Size.Y - Bearing.Y is from the bottom to the line

            float w = glChar.Size.X * scale;
            float h = glChar.Size.Y * scale;
            
            
            // construct a rectangle around the character
            
            float[] vertices =
            {
                // triangle 1
                xpos,     ypos + h,   0.0f, 0.0f,   // top left
                xpos,     ypos,       0.0f, 1.0f,   // bottom left
                xpos + w, ypos,       1.0f, 1.0f,   // bottom right
                
                /*
                | \
                |   \
                |     \
                + _ _ _ */


                // triangle 2
                xpos,     ypos + h,   0.0f, 0.0f,   // top left
                xpos + w, ypos,       1.0f, 1.0f,   // bottom right
                xpos + w, ypos + h,   1.0f, 0.0f    // top right
                
                /* _ _ _ +
                  \      |
                    \    |
                      \  |
                        */
            };
            
            GL.BindTexture(TextureTarget.Texture2D, glChar.TextureId);
            GL.BufferSubData(BufferTarget.ArrayBuffer,IntPtr.Zero,24 * sizeof(float),vertices);
            GL.DrawArrays(PrimitiveType.Triangles,0,6);

            x += (glChar.Advance >> 6) * scale;

        }
        
        GL.BindVertexArray(0);
        GL.BindTexture(TextureTarget.Texture2D,0);
    }

    public void UpdateScreenSize(Vector2i screenSize)
    {
        if (textShader.Compiled) textShader.Uniform2("screenSize", new Vector2(screenSize.X,screenSize.Y));
    }

    public void Delete()
    {
        vao.Delete();
        foreach (var glChar in GlChars.Values) { GL.DeleteTexture(glChar.TextureId); }
        textShader.Delete();
    }
}