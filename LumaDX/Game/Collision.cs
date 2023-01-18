using OpenTK.Mathematics;

namespace LumaDX;

public class Collision
{
    public Vector3 eRadius;

    public Vector3 position;
    public int collisionRecursionDepth;

    private bool checkGrounded;
    private bool grounded;

    public Maths.Triangle[] world;
    public List<Maths.Triangle> toCheck;

    private Vector3 newGrav;

    
    private void GetClose(Vector3 pos, Vector3 vel)
    {
        toCheck = new List<Maths.Triangle>();
        foreach (var triangle in world)
        {
            Vector3 p0vec = triangle.Point0 - triangle.Center;
            Vector3 p1vec = triangle.Point1 - triangle.Center;
            Vector3 p2vec = triangle.Point2 - triangle.Center;
            
            // maximum radius (squared) from the centre of the triangle to an outer point
            float maxRadiusSquared = MathF.Max(Vector3.Dot(p0vec,p0vec), MathF.Max(Vector3.Dot(p1vec,p1vec), Vector3.Dot(p2vec,p2vec)));

            if (Vector3.Dot(pos - triangle.Center, pos - triangle.Center) - MathF.Max(Vector3.Dot(vel, vel), 1f) * 500f > MathF.Max(maxRadiusSquared, 1f)) continue;

            toCheck.Add(triangle);
        }
    }
    
    
    
    public (bool,Vector3) CollideAndSlide(Vector3 vel, Vector3 gravity)
    {

        checkGrounded = false;
        
        float velocityLenSquared = Vector3.Dot(vel, vel);
        float gravityLenSquared = Vector3.Dot(gravity, gravity);

        
        /*
        // not sure if this bit is necessary
        if (velocityLenSquared < Maths.VeryCloseDistance * Maths.VeryCloseDistance && velocityLenSquared > 0)
        {
            vel = vel.Normalized() * Maths.VeryCloseDistance;
            velocityLenSquared = Maths.VeryCloseDistance * Maths.VeryCloseDistance;
        }
        
        if (gravityLenSquared < Maths.VeryCloseDistance * Maths.VeryCloseDistance && gravityLenSquared > 0)
        {
            gravity = gravity.Normalized() * Maths.VeryCloseDistance;
            gravityLenSquared = Maths.VeryCloseDistance * Maths.VeryCloseDistance;
        }
        //
        */
        

        Vector3 eSpacePosition = position / eRadius;
        Vector3 eSpaceVelocity = vel / eRadius;

        Vector3 finalPosition = eSpacePosition;
        
        if (velocityLenSquared != 0.0f && !float.IsNaN(velocityLenSquared))
        {
            collisionRecursionDepth = 0;

            GetClose(finalPosition, eSpaceVelocity);
            finalPosition = CollideWithWorld(eSpacePosition,eSpaceVelocity);
            
        }

        grounded = true;
        if (!float.IsNaN(gravityLenSquared)) // we may still want this step if gravity == 0.0 for the grounded check (walking off a platform)
        {
            eSpaceVelocity = gravity / eRadius;
            newGrav = eSpaceVelocity;
            float len = newGrav.Length;
            if (len > 0f) GravityDirection = newGrav.Normalized();
            
            collisionRecursionDepth = 0;

            checkGrounded = true;
            grounded = false;
   
            GetClose(finalPosition, eSpaceVelocity);
            finalPosition = CollideWithWorld(finalPosition, eSpaceVelocity);
        }
        

        finalPosition *= eRadius;

        position = finalPosition;

        return (grounded,newGrav*eRadius);
    }

    
    private Vector3 velocity;
    private Vector3 normalizedVelocity;
    private Vector3 basePoint;
    private Vector3 intersectionPoint;
    private float nearestDistance;
    private bool foundCollision;

    public Vector3 CollideWithWorld(Vector3 pos, Vector3 vel)
    {
        if (collisionRecursionDepth > 5) return pos;

        velocity = vel;
        normalizedVelocity = vel;
        normalizedVelocity.Normalize();
        basePoint = pos;
        foundCollision = false;
        
        CheckCollision();

        if (!foundCollision) return pos + vel;
        
        
        // collision

        Vector3 destinationPoint = pos + vel;
        Vector3 newBasePoint = pos;
        
        // prevent player from getting too close to the object
        if (nearestDistance <= Maths.VeryCloseDistance)
        {
            Vector3 v = vel.Normalized();
            float newLength = (nearestDistance - Maths.VeryCloseDistance);
            newBasePoint = basePoint + v*newLength;

            // move sliding plane back against the player
            intersectionPoint -= Maths.VeryCloseDistance * v;
        }

        Vector3 slidePlaneOrigin = intersectionPoint;
        Vector3 slidePlaneNormal = newBasePoint - intersectionPoint;
        slidePlaneNormal.Normalize();
        Maths.Plane slidingPlane = new Maths.Plane(slidePlaneNormal, Vector3.Dot(slidePlaneNormal, slidePlaneOrigin));

        Vector3 newDestinationPoint = destinationPoint - slidingPlane.SignedDistance(destinationPoint) * slidePlaneNormal;
        Vector3 newVelocityVector = newDestinationPoint - intersectionPoint;

        float newVelLenSquared = Vector3.Dot(newVelocityVector, newVelocityVector);
        if (newVelLenSquared < Maths.VeryCloseDistance*Maths.VeryCloseDistance) return newBasePoint;
        //if (newVelLenSquared > 1f) newVelocityVector /= MathF.Sqrt(newVelLenSquared);

        collisionRecursionDepth++;
        checkGrounded = false;
        return CollideWithWorld(newBasePoint,newVelocityVector);
    }

    public Vector3 GravityDirection = -Vector3.UnitY;

    public void CheckCollision()
    {
        float velocitySquaredLength = Vector3.Dot(velocity,velocity);

        foreach (var triangle in toCheck)
        {

            if (checkGrounded && !grounded && Vector3.Dot(basePoint - triangle.Center, GravityDirection) < 0f) // only check triangles in the direction of gravity
            {
                // lambda for the intersection of the line (pos + lambda x direction) and the plane
                float lambda = (triangle.Plane.Value - Vector3.Dot(triangle.Plane.Normal, basePoint)) / Vector3.Dot(triangle.Plane.Normal, GravityDirection);
                Vector3 point = basePoint + lambda * GravityDirection;
                Vector3 pointToPos = basePoint - point;
                if (Vector3.Dot(pointToPos, pointToPos) < 1.5 && Maths.CheckPointInTriangle(triangle, point))
                {
                    if (GravityDirection.Y > 0)
                    {
                        newGrav = Vector3.Zero;
                    }
                    else
                    {
                        grounded = true;
                    }
                    
                }
            }
            
            
            float signedDistToTrianglePlane = triangle.Plane.SignedDistance(basePoint);
   

            float normalDotVelocity = Vector3.Dot(triangle.Plane.Normal, velocity);

            float t0 = 0;
            float t1 = 0;

            bool embeddedInPlane = false;

            if (MathF.Abs(normalDotVelocity) == 0.0f) // normal is perpendicular to velocity
            {
                if (MathF.Abs(signedDistToTrianglePlane) > 1f) continue; // impossible to collide

                // otherwise, this must be an embedded sphere, which can only
                // collide against a vertex or an edge, not the whole triangle
                t0 = 0f;
                t1 = 1f;
                embeddedInPlane = true;
            }
            else
            {
                t0 = (1f - signedDistToTrianglePlane) / normalDotVelocity;
                t1 = (-1f - signedDistToTrianglePlane) / normalDotVelocity;

                if (t0 > t1)
                {
                    (t0, t1) = (t1, t0);
                } // swap so t0 is smaller;

                if (t0 > 1f || t1 < 0f)
                {
                    // impossible for the sphere to collide with this triangle
                    continue;
                }

                t0 = Math.Clamp(t0, 0f, 1f);
                t1 = Math.Clamp(t1, 0f, 1f);

            }

                
            Vector3 collisionPoint = Vector3.Zero;
            bool collision = false;
            float t = 1f;

            if (!embeddedInPlane)
            {
                Vector3 planeIntersectionPoint = (basePoint - triangle.Plane.Normal) + t0 * velocity;

                if (Maths.CheckPointInTriangle(triangle, planeIntersectionPoint))
                {
                    collision = true;
                    t = t0;
                    collisionPoint = planeIntersectionPoint;
          
                }

            }



            if (!collision)
            {

                Vector3 vel = velocity;
                Vector3 pos = basePoint;

                float newT;

                float a, b, c;
                // Check against points:
                a = velocitySquaredLength;
                
                // P0
                b = 2f*(Vector3.Dot(vel,pos-triangle.Point0));
                c = (triangle.Point0-pos).LengthSquared - 1f;
                if (Maths.GetLowestRoot(a,b,c, t, out newT) && newT < t) {
                    t = newT;
                    collision = true;
                    collisionPoint = triangle.Point0;
                }

                // P1
                b = 2f*(Vector3.Dot(vel,pos-triangle.Point1));
                c = (triangle.Point1-pos).LengthSquared - 1f;
                if (Maths.GetLowestRoot(a,b,c, t, out newT) && newT < t) {
                    t = newT;
                    collision = true;
                    collisionPoint = triangle.Point1;
                }
                
                // P2
                b = 2f*(Vector3.Dot(vel,pos-triangle.Point2));
                c = (triangle.Point2-pos).LengthSquared - 1f;
                if (Maths.GetLowestRoot(a,b,c, t, out newT) && newT < t) {
                    t = newT;
                    collision = true;
                    collisionPoint = triangle.Point2;
                }
               
                    
                    
                    
                    
                // Check against edges:
                    
                // p0 -> p1:
                Vector3 edge = triangle.Point1-triangle.Point0;
                Vector3 baseToVertex = triangle.Point0 - pos;
                float baseToVertexSquaredLength = Vector3.Dot(baseToVertex,baseToVertex);
                float edgeSquaredLength = Vector3.Dot(edge,edge);

                float edgeDotVelocity = Vector3.Dot(edge,vel);
                float edgeDotBaseToVertex = Vector3.Dot(edge,baseToVertex);
                // Calculate parameters for equation
                a = edgeSquaredLength*-velocitySquaredLength +
                          edgeDotVelocity*edgeDotVelocity;
                b = edgeSquaredLength*(2f*Vector3.Dot(vel,baseToVertex))-
                          2f*edgeDotVelocity*edgeDotBaseToVertex;
                c = edgeSquaredLength*(1f-baseToVertexSquaredLength)+
                          edgeDotBaseToVertex*edgeDotBaseToVertex;
                // Does the swept sphere collide against infinite edge?

                if (Maths.GetLowestRoot(a,b,c, t, out newT) && newT < t) {
                    // Check if intersection is within line segment:
                    float f=(edgeDotVelocity*newT-edgeDotBaseToVertex)/
                            edgeSquaredLength;
                    if (f >= 0.0 && f <= 1.0) {
                        // intersection took place within segment.
                        t = newT;
                        collision = true;
                        collisionPoint = triangle.Point0 + f*edge;
                    }
                }     
                    

                // p1 -> p2:
                edge = triangle.Point2-triangle.Point1;
                baseToVertex = triangle.Point1 - pos;
                baseToVertexSquaredLength = Vector3.Dot(baseToVertex,baseToVertex);
                edgeSquaredLength = Vector3.Dot(edge,edge);

                edgeDotVelocity = Vector3.Dot(edge,vel);
                edgeDotBaseToVertex = Vector3.Dot(edge,baseToVertex);
                // Calculate parameters for equation
                a = edgeSquaredLength*-velocitySquaredLength +
                    edgeDotVelocity*edgeDotVelocity;
                b = edgeSquaredLength*(2f*Vector3.Dot(vel,baseToVertex))-
                    2f*edgeDotVelocity*edgeDotBaseToVertex;
                c = edgeSquaredLength*(1f-baseToVertexSquaredLength)+
                    edgeDotBaseToVertex*edgeDotBaseToVertex;
                // Does the swept sphere collide against infinite edge?

                if (Maths.GetLowestRoot(a,b,c, t, out newT) && newT < t) {
                    // Check if intersection is within line segment:
                    float f=(edgeDotVelocity*newT-edgeDotBaseToVertex)/
                            edgeSquaredLength;
                    if (f >= 0.0 && f <= 1.0) {
                        // intersection took place within segment.
                        t = newT;
                        collision = true;
                        collisionPoint = triangle.Point1 + f*edge;
                    }
                }

                    
                // p2 -> p0:
                edge = triangle.Point0-triangle.Point2;
                baseToVertex = triangle.Point2 - pos;
                baseToVertexSquaredLength = Vector3.Dot(baseToVertex,baseToVertex);
                edgeSquaredLength = Vector3.Dot(edge,edge);

                edgeDotVelocity = Vector3.Dot(edge,vel);
                edgeDotBaseToVertex = Vector3.Dot(edge,baseToVertex);
                // Calculate parameters for equation
                a = edgeSquaredLength*-velocitySquaredLength +
                    edgeDotVelocity*edgeDotVelocity;
                b = edgeSquaredLength*(2f*Vector3.Dot(vel,baseToVertex))-
                    2f*edgeDotVelocity*edgeDotBaseToVertex;
                c = edgeSquaredLength*(1f-baseToVertexSquaredLength)+
                    edgeDotBaseToVertex*edgeDotBaseToVertex;
                // Does the swept sphere collide against infinite edge?

                if (Maths.GetLowestRoot(a,b,c, t, out newT) && newT < t) {
                    // Check if intersection is within line segment:
                    float f=(edgeDotVelocity*newT-edgeDotBaseToVertex)/
                            edgeSquaredLength;
                    if (f >= 0.0 && f <= 1.0) {
                        // intersection took place within segment.
                        t = newT;
                        collision = true;
                        collisionPoint = triangle.Point2 + f*edge;
                    }
                }
                    



            }

            if (!collision) continue;
                
            float distToCollision = t * MathF.Sqrt(velocitySquaredLength);

            if (!foundCollision || distToCollision < nearestDistance)
            {
                nearestDistance = distToCollision;
                intersectionPoint = collisionPoint;
                foundCollision = true;
            }
        }
    }
}