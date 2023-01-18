using Assimp;
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

    
    

    public struct Triangle
    {
        public Vector3 Point0;
        public Vector3 Point1;
        public Vector3 Point2;
        
        public Vector3 Center;
        public Plane Plane;

        public Triangle(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 eRadius)
        {
            if (eRadius == default) eRadius = Vector3.One;
            
            Point0 = p0/eRadius;
            Point1 = p1/eRadius;
            Point2 = p2/eRadius;

            Center = (Point0 + Point1 + Point2) / 3f;

            Plane = Plane.FromTriangle(Point0, Point1, Point2);
        }
    }

    public class Plane
    {
        // dot(normal,point) = value, where point is a point on the plane
        public Vector3 Normal;
        public float Value;

        public Plane(Vector4 equation)
        {
            Normal = equation.Xyz.Normalized();
            Value = equation.W;
        }
        
        public Plane(Vector3 normal, float value)
        {
            Normal = normal.Normalized();
            Value = value;
        }
        

        public static Plane FromNormalAndPoint(Vector3 normal, Vector3 point)
        {
            Vector3 norm = normal.Normalized();
            return new Plane(norm,Vector3.Dot(norm, point));
        }

        public static Plane FromTriangle(Triangle triangle)
        {
            Vector3 v0 = triangle.Point1 - triangle.Point0;
            Vector3 v1 = triangle.Point2 - triangle.Point0;

            Vector3 normal = Vector3.Cross(v0, v1).Normalized();
            float value = Vector3.Dot(normal, triangle.Point0);
            
            return new Plane(normal, value);
        }
        
        public static Plane FromTriangle(Vector3 p0,Vector3 p1,Vector3 p2)
        {
            Vector3 v0 = p1 - p0;
            Vector3 v1 = p2 - p0;

            Vector3 normal = Vector3.Cross(v0, v1).Normalized();
            float value = Vector3.Dot(normal, p0);
            
            return new Plane(normal, value);
        }

        public float SignedDistance(Vector3 position)
        {
            return (Vector3.Dot(Normal, position) - Value);
        }


    }
    
    public static Triangle[] GetTriangles(Scene model, Vector3 position, Vector3 rotation, Vector3 eRadius)
    {
        Matrix4 transform = CreateTransformation(position, rotation, Vector3.One);
        transform.Transpose();
        
        List<int> indexList = new List<int>();
        List<float> vertexList = new List<float>();

        int offset = 0;
        foreach (var mesh in model.Meshes)
        {
                
            foreach (var vert in mesh.Vertices)
            {
                    
                vertexList.Add(vert.X/100f);
                vertexList.Add(vert.Y/100f);
                vertexList.Add(vert.Z/100f);
            }

            foreach (var index in mesh.GetIndices())
            {
                indexList.Add(index + offset);
            }

            offset += mesh.VertexCount;
        }

        List<Triangle> triangles = new List<Triangle>();
        for (int i = 0; i < indexList.Count; i += 3)
        {

            int v0 = (indexList[i]) * 3;
            int v1 = (indexList[i + 1]) * 3;
            int v2 = (indexList[i + 2]) * 3;


            var triangle = new Triangle
            (
                (transform * new Vector4(vertexList[v0], vertexList[v0 + 1], vertexList[v0 + 2], 1f)).Xyz,
                (transform * new Vector4(vertexList[v1], vertexList[v1 + 1], vertexList[v1 + 2], 1f)).Xyz,
                (transform * new Vector4(vertexList[v2], vertexList[v2 + 1], vertexList[v2 + 2], 1f)).Xyz,
                eRadius
            );
            if (triangle.Plane.Normal == Vector3.Zero) continue;

            triangles.Add(triangle);
        }

        return triangles.ToArray();

    }

    // https://blackpawn.com/texts/pointinpoly/
    public static bool SameSide(Vector3 p1, Vector3 p2, Vector3 a, Vector3 b)
    {
        Vector3 cp1 = Vector3.Cross(b - a, p1 - a);
        Vector3 cp2 = Vector3.Cross(b - a, p2 - a);
        return Vector3.Dot(cp1, cp2) >= 0;
    }

    public static bool CheckPointInTriangle(Triangle triangle, Vector3 point)
    {
        return (SameSide(point, triangle.Point0, triangle.Point1, triangle.Point2)&&
                SameSide(point, triangle.Point1, triangle.Point0, triangle.Point2)&&
                SameSide(point, triangle.Point2, triangle.Point0, triangle.Point1)
            );
    }
    

    public static bool GetLowestRoot(float a, float b, float c, float maxR,
        out float root)
    {
        root = 0f;
        
        // Check if a solution exists
        float determinant = b*b - 4.0f*a*c;
        // If determinant is negative it means no solutions.
        if (determinant < 0f) return false;
        
        // calculate the two roots: (if determinant == 0 then
        // x1==x2 but let’s disregard that slight optimization)
        float sqrtD = MathF.Sqrt(determinant);
        float r1 = (-b - sqrtD) / (2*a);
        float r2 = (-b + sqrtD) / (2*a);
        
        // Sort so x1 <= x2
        if (r1 > r2) { (r2, r1) = (r1, r2); }
        
        // Get lowest root:
        if (r1 > 0 && r1 < maxR) {
            root = r1;
            return true;
        }
        // It is possible that we want x2 - this can happen
        // if x1 < 0
        if (r2 > 0 && r2 < maxR) {
            root = r2;
            return true;
        }
        
        // No (valid) solutions
        return false;
        
    }
    
    public const float VeryCloseDistance = 0.001f;
    
    
}