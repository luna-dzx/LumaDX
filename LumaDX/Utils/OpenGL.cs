using System;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using Half = OpenTK.Mathematics.Half;

namespace LumaDX;

public class OpenGL
{
    /// <summary>
    /// Finds the equivalent OpenGL type of an object
    /// </summary>
    /// <param name="t">the variable type to check</param>
    /// <returns>OpenGL vertex attrib pointer type for loading</returns>
    /// <exception cref="Exception">entered type not accounted for</exception>
    public static VertexAttribPointerType GetAttribPointerType(Type t)
    {
        if (t == typeof(byte)) return VertexAttribPointerType.Byte;
        if (t == typeof(double)) return VertexAttribPointerType.Double;
        if (t == typeof(float)) return VertexAttribPointerType.Float;
        if (t == typeof(int)) return VertexAttribPointerType.Int;
        if (t == typeof(short)) return VertexAttribPointerType.Short;
        if (t == typeof(Half)) return VertexAttribPointerType.HalfFloat;
        if (t == typeof(uint)) return VertexAttribPointerType.UnsignedInt;
        if (t == typeof(ushort)) return VertexAttribPointerType.UnsignedShort;

        throw new Exception("Invalid Type");
    }

    /// <summary>
    /// Calculate the size of a variable
    /// </summary>
    /// <param name="t">the variable type to check</param>
    /// <returns>the number of bytes this object takes up as an integer</returns>
    public static int GetSizeInBytes(Type t)
    {
        if (t == typeof(byte)) return sizeof(byte);
        if (t == typeof(double)) return sizeof(double);
        if (t == typeof(float)) return sizeof(float);
        if (t == typeof(int)) return sizeof(int);
        if (t == typeof(short)) return sizeof(short);
        if (t == typeof(Half)) return sizeof(float)/2;
        if (t == typeof(uint)) return sizeof(uint);
        if (t == typeof(ushort)) return sizeof(ushort);
        if (t == typeof(Matrix4)) return sizeof(float) * 16;

        return 4;
    }

    public static DrawBuffersEnum[] GetDrawBuffers(int numBuffers, int offset=0)
    {
        DrawBuffersEnum[] buffers = new DrawBuffersEnum[numBuffers];
        for (int i = 0; i < numBuffers; i++)
        {
            buffers[i] = DrawBuffersEnum.ColorAttachment0 + i + offset;
        }

        return buffers;
    }


    public static void BindTexture(int unit, TextureTarget target, int texture)
    {
        GL.ActiveTexture(TextureUnit.Texture0 + unit);
        GL.BindTexture(target,texture);
    }
    
    
}