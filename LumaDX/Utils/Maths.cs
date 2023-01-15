using System;
using OpenTK.Mathematics;

namespace LumaDX;

public class Maths
{
    /// <summary>
    /// Creates a combined matrix of translation, rotation and scale transformation matrices
    /// </summary>
    /// <param name="translate">the position of the transform</param>
    /// <param name="rotate">the rotation of the transform (in radians)</param>
    /// <param name="scale">the scale of the transform</param>
    /// <returns></returns>
    public static Matrix4 CreateTransformation(Vector3 translate, Vector3 rotate, Vector3 scale)
    {
        Vector3 sin = new Vector3(MathF.Sin(rotate.X), MathF.Sin(rotate.Y), MathF.Sin(rotate.Z));
        Vector3 cos = new Vector3(MathF.Cos(rotate.X), MathF.Cos(rotate.Y), MathF.Cos(rotate.Z));

        Matrix4 result;
        
        result.Row0.X = scale.X * cos.Y * cos.Z;
        result.Row0.Y = scale.X * cos.Y * sin.Z;
        result.Row0.Z = scale.X * -sin.Y;
        result.Row0.W = 0;
        result.Row1.X = scale.Y * (sin.X * sin.Y * cos.Z + cos.X * -sin.Z);
        result.Row1.Y = scale.Y * (sin.X * sin.Y * sin.Z + cos.X * cos.Z);
        result.Row1.Z = scale.Y * sin.X * cos.Y;
        result.Row1.W = 0;
        result.Row2.X = scale.Z * (cos.X * sin.Y * cos.Z + -sin.X * -sin.Z);
        result.Row2.Y = scale.Z * (cos.X * sin.Y * sin.Z + -sin.X * cos.Z);
        result.Row2.Z = scale.Z * cos.X * cos.Y;
        result.Row2.W = 0;
        result.Row3.X = translate.X;
        result.Row3.Y = translate.Y;
        result.Row3.Z = translate.Z;
        result.Row3.W = 1;

        return result;
    }

    public static Matrix4 TranslateMatrix(float x, float y, float z)
    {
        Matrix4 result = Matrix4.Identity;
        result.Row3 = new Vector4(x,y,z, 1f);
        return result;
    }


    public static (Vector3, Vector3) CalculateTangents((Vector3,Vector3,Vector3) positions, (Vector2,Vector2,Vector2) texCoords)
    {
        var (pos1, pos2, pos3) = positions;
        var (uv1, uv2, uv3) = texCoords;
        
        var edge1 = pos2 - pos1;
        var edge2 = pos3 - pos1;
        
        var deltaUv1 = uv2 - uv1;
        var deltaUv2 = uv3 - uv1;

        float f = 1f / (deltaUv1.X * deltaUv2.Y - deltaUv2.X * deltaUv1.Y);

        Vector3 tangent, biTangent;
        
        tangent.X = f * (deltaUv2.Y * edge1.X - deltaUv1.Y * edge2.X);
        tangent.Y = f * (deltaUv2.Y * edge1.Y - deltaUv1.Y * edge2.Y);
        tangent.Z = f * (deltaUv2.Y * edge1.Z - deltaUv1.Y * edge2.Z);

        biTangent.X = f * (-deltaUv2.X * edge1.X + deltaUv1.X * edge2.X);
        biTangent.Y = f * (-deltaUv2.X * edge1.Y + deltaUv1.X * edge2.Y);
        biTangent.Z = f * (-deltaUv2.X * edge1.Z + deltaUv1.X * edge2.Z);

        return (tangent, biTangent);
    }


    public static float Lerp(float a, float b, float f) => a + f * (b - a);

}