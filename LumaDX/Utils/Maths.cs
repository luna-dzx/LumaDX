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
    public static Matrix4 CreateTransformation(Vector3 translate, Vector3 rotate, Vector3 scale, bool transpose = false)
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

        if (transpose) result.Transpose();
        return result;
    }
    
    /// <summary>
    /// Given a triangle as individual vertices and its relative texture coordinates, approximate the tangents and bi-tangents (should follow the direction of texture coordinates)
    /// </summary>
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


    /// <summary>
    /// Linear Interpolation
    /// </summary>
    public static float Lerp(float a, float b, float f) => a + f * (b - a);

    /// <summary>
    /// Recursive sphere mesh generation using triangle subdivisions
    /// </summary>
    public static Objects.Mesh GenerateIcoSphere(int iterations)
    {
        if (iterations <= 0) return PresetMesh.Icosahedron;
        Objects.Mesh mesh = GenerateIcoSphere(iterations - 1);
        
        List<float> vertices = new List<float>(mesh.Vertices);
        List<int> indices = new List<int>();

        // dictionary that contains the index of a midpoint given the indices the 2 point it is between in either order
        var midPoints = new Dictionary<(int, int), int>(new OrderlessIntPairs());

        // loop through triangles (index t)
        for (int t = 0; t < mesh.Indices.Length; t += 3)
        {
            
            for (int v = 0; v < 3; v++)
            {
                int index0 = mesh.Indices[t + v];
                int index1 = mesh.Indices[t + (v + 1) % 3];

                // ignore existing points
                if (midPoints.ContainsKey((index0, index1))) continue;

                // add new point
                midPoints.Add((index0, index1), vertices.Count / 3);

                //add midpoint vertex
                Vector3 point0 = vertices.GetVertex(index0);
                Vector3 point1 = vertices.GetVertex(index1);
                
                vertices.AddVec3((point0 + point1).Normalized());
            }


            // original triangle
            int a = mesh.Indices[t];
            int b = mesh.Indices[t + 1];
            int c = mesh.Indices[t + 2];
            
            // midpoints
            int ab = midPoints[(a,b)];
            int bc = midPoints[(b,c)];
            int ca = midPoints[(c,a)];
            
            /*
                            a
                            /\
                          /   \
                        /      \
                  ab  / _ _ _ _ \ ac
                    /\          /\ 
                  /   \       /   \
                /      \    /      \
            b / _ _ _ _ \ / _ _ _ _ \ c
                        bc
              
            */

            indices.AddItems(
            a, ab, ca,
                        b, bc, ab,
                        c, ca, bc,
                        ab, bc, ca
            );

        }
        
        mesh.Vertices = vertices.ToArray();
        mesh.Indices = indices.ToArray();

        return mesh;
    }

    /// <summary>
    /// Struct for caching important data about 3D triangles
    /// </summary>
    public struct Triangle
    {
        public Vector3 Point0;
        public Vector3 Point1;
        public Vector3 Point2;
        
        public Vector3 Center;
        public Plane Plane;
        
        /// <param name="eRadius">Ellipsoid radius of player in collisions</param>
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

    /// <summary>
    /// 3D infinite plane calculations in vector form (r*N = a*N)
    /// </summary>
    public class Plane
    {
        // dot(normal,point) = value, where point is a point on the plane
        public Vector3 Normal;
        public float Value;
        
        /// <param name="equation">x,y,z is normal, w is a*N where a is a point on the plane</param>
        public Plane(Vector4 equation)
        {
            Normal = equation.Xyz.Normalized();
            Value = equation.W;
        }

        /// <param name="value">a*N where a is a point on the plane</param>
        public Plane(Vector3 normal, float value)
        {
            Normal = normal.Normalized();
            Value = value;
        }
        

        /// <summary>
        /// Calculate plane values given the plane's normal and a point which lies on the plane
        /// </summary>
        public static Plane FromNormalAndPoint(Vector3 normal, Vector3 point)
        {
            Vector3 norm = normal.Normalized();
            return new Plane(norm,Vector3.Dot(norm, point));
        }

        /// <summary>
        /// Calculate the plane which a triangle lies on
        /// </summary>
        public static Plane FromTriangle(Triangle triangle)
        {
            Vector3 v0 = triangle.Point1 - triangle.Point0;
            Vector3 v1 = triangle.Point2 - triangle.Point0;

            Vector3 normal = Vector3.Cross(v0, v1).Normalized();
            float value = Vector3.Dot(normal, triangle.Point0);
            
            return new Plane(normal, value);
        }
        
        /// <summary>
        /// Calculate the plane which a triangle lies on given the 3 vertices of that triangle
        /// </summary>
        public static Plane FromTriangle(Vector3 p0,Vector3 p1,Vector3 p2)
        {
            Vector3 v0 = p1 - p0;
            Vector3 v1 = p2 - p0;

            Vector3 normal = Vector3.Cross(v0, v1).Normalized();
            float value = Vector3.Dot(normal, p0);
            
            return new Plane(normal, value);
        }

        /// <summary>
        /// Distance from plane to a position accounting for the side of the plane this is on (represented by the sign, +/-)
        /// </summary>
        public float SignedDistance(Vector3 position)
        {
            return Vector3.Dot(Normal, position) - Value;
        }

        /// <summary>
        /// Convert to a simplification of the plane equation for loading to the GPU.
        /// x,y,z is normal, w is a*N where a is a point on the plane
        /// </summary>
        public Vector4 AsVector() => new (Normal, Value);
    }
    
    
    /// <summary>
    /// Are first 2 points on the same side of the line through the last 2 points?
    /// </summary>
    public static bool SameSide(Vector3 p1, Vector3 p2, Vector3 a, Vector3 b)
    {
        Vector3 cp1 = Vector3.Cross(b - a, p1 - a);
        Vector3 cp2 = Vector3.Cross(b - a, p2 - a);
        return Vector3.Dot(cp1, cp2) >= 0;
    }
    
    /// <summary>
    /// Given the three vertices of a triangle and a point, return whether the point lies within the triangle
    /// </summary>
    public static bool CheckPointInTriangle(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 point)
    {
        return (SameSide(point, p0, p1, p2)&&
                SameSide(point, p1, p0, p2)&&
                SameSide(point, p2, p0, p1)
            );
    }

    /// <summary>
    /// Does the given point lie on the given triangle?
    /// </summary>
    public static bool CheckPointInTriangle(Triangle triangle, Vector3 point) => CheckPointInTriangle(triangle.Point0,triangle.Point1,triangle.Point2,point);


    /// <summary>
    /// Get the lowest root of a quadratic between 0 and maxR
    /// </summary>
    /// <param name="a">The x^2 coefficient of the quadratic</param>
    /// <param name="b">The x coefficient of the quadratic</param>
    /// <param name="c">The constant of the quadratic</param>
    /// <param name="maxR">The maximum value of these roots</param>
    /// <param name="root">The lowest root (or 0 if none are valid)</param>
    /// <returns>Whether or not there was a valid solution</returns>
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

    /// <summary>
    /// Given any number of values, return the maximum
    /// </summary>
    public static float Max(params float[] values)
    {
        float max = values[0];
        for (int i = 1; i < values.Length; i++)
        {
            max = MathF.Max(max, values[i]);
        }
        return max;
    }
    
    /// <summary>
    /// Given any number of vectors, return the longest/largest (the one with the highest length calculated by pythagorean theorem)
    /// </summary>
    public static Vector3 Largest(params Vector3[] values)
    {
        Vector3 vec = values[0];
        for (int i = 1; i < values.Length; i++)
        {
            vec = (vec.LengthSquared > values[i].LengthSquared) ? vec : values[i];
        }
        return vec;
    }
    

}