using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace LumaDX;

public class Portal : IDisposable
{
    // these things stay the same across all portals
    public static Vector3 Size = new Vector3(0.4f,0.6f,0.4f);
    
    // Position of this portal
    private Vector3 _position, _rotation;
    public Model Rectangle;

    public Maths.Plane ClippingPlane;

    public Vector3 Position
    {
        get => _position;
        set
        {
            _position = value;
            UpdateTransformation();
        }
    }
    
    public Vector3 Rotation
    {
        get => _rotation;
        set
        {
            _rotation = value;
            UpdateTransformation();
        }
    }
    
    public Matrix4 Transformation;

    // FrameBuffer and ViewMatrix for sampling destination portal which is at DestinationPos
    public FrameBuffer FrameBuffer;
    public Matrix4 ViewMatrix;
    public Vector3 DestinationPos;

    private readonly Vector3 up = Vector3.UnitY;

    private Maths.Triangle _triangle0;
    private Maths.Triangle _triangle1;
    
    
    public Portal(Vector2i resolution, Vector3 position, Vector3 rotation)
    {
        Position = position;
        Rotation = rotation;
        FrameBuffer = new FrameBuffer(resolution, TextureTarget.Texture2D);
        Rectangle = new(PresetMesh.Square);
        
        UpdateTransformation();
    }

    public void UpdateTransformation()
    {
        Transformation = Maths.CreateTransformation(Position, Rotation, Size);
        
        Transformation.Transpose();
        
        var p0 = Transformation * new Vector4(PresetMesh.Square.Vertices[0], PresetMesh.Square.Vertices[1], PresetMesh.Square.Vertices[2],1f);
        var p1 = Transformation * new Vector4(PresetMesh.Square.Vertices[3], PresetMesh.Square.Vertices[4], PresetMesh.Square.Vertices[5],1f);
        var p2 = Transformation * new Vector4(PresetMesh.Square.Vertices[6], PresetMesh.Square.Vertices[7], PresetMesh.Square.Vertices[8],1f);
        _triangle0 = new Maths.Triangle(p0.Xyz,p1.Xyz,p2.Xyz,Vector3.One);
        ClippingPlane = _triangle0.Plane;
        
        p0 = Transformation * new Vector4(PresetMesh.Square.Vertices[9], PresetMesh.Square.Vertices[10], PresetMesh.Square.Vertices[11],1f);
        p1 = Transformation * new Vector4(PresetMesh.Square.Vertices[12], PresetMesh.Square.Vertices[13], PresetMesh.Square.Vertices[14],1f);
        p2 = Transformation * new Vector4(PresetMesh.Square.Vertices[15], PresetMesh.Square.Vertices[16], PresetMesh.Square.Vertices[17],1f);
        _triangle1 = new Maths.Triangle(p0.Xyz,p1.Xyz,p2.Xyz,Vector3.One);
        
        Transformation.Transpose();
    }

    public Vector3 RelativeCameraPos = Vector3.Zero;

    public void Update(Vector3 cameraPos, Vector3 cameraDirection, Portal destination)
    {
        DestinationPos = destination.Position;

        // multiply by inverse of this transform, then by other portal's transform
        Vector3 primPos = (Transformation.Inverted() * new Vector4(cameraPos - Position,1f)).Xyz;
        var relativeVec = (destination.Transformation * new Vector4(primPos,1f)).Xyz;
        
        Vector3 primLook = (Transformation.Inverted() * new Vector4(cameraDirection,1f)).Xyz;
        var relativeLook = (destination.Transformation * new Vector4(primLook,1f)).Xyz;

        RelativeCameraPos = destination.Position + relativeVec;

        ViewMatrix = Matrix4.LookAt(RelativeCameraPos, RelativeCameraPos + relativeLook, up);
    }
    
    
  

    public bool Teleport(Portal destination, FirstPersonPlayer player, out Vector3 position)
    {
        position = Vector3.Zero;

        // if we are on the same side of the portal before and after moving
        if (!((Vector3.Dot(Position - player.Position, ClippingPlane.Normal) < 0) ^ 
            (Vector3.Dot(Position - (player.LastPosition), ClippingPlane.Normal) < 0)
            ))
        {
            // then we can't have teleported
            return false;
        }

        var pos = player.Position;

        float vn = Vector3.Dot(player.Position - player.LastPosition, ClippingPlane.Normal);
        if (vn == 0f) return false;
        float t = (Vector3.Dot(Position, ClippingPlane.Normal) - Vector3.Dot(pos, ClippingPlane.Normal)) / vn;
        Vector3 pointPos = pos + t * (player.Position - player.LastPosition);

        if ((Maths.CheckPointInTriangle(_triangle0,pointPos) || Maths.CheckPointInTriangle(_triangle1,pointPos)))
        {
            Vector3 primPos = (Transformation.Inverted() * new Vector4(player.Position - Position,1f)).Xyz;
            var relativeVec = (destination.Transformation * new Vector4(primPos,1f)).Xyz;
            position = destination.Position + relativeVec;
            return true;
        }

        return false;

    }
    

    public void StartSample() => FrameBuffer.WriteMode();
    public void EndSample() => FrameBuffer.ReadMode();
    
    
    
    public void Draw(ShaderProgram shader)
    {
        Rectangle.SetMatrix(shader,Transformation);
        Rectangle.Draw(shader);
    }

    public void Dispose()
    {
        Rectangle.Dispose();
        FrameBuffer.Dispose();
    }
}