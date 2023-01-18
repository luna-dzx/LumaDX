using OpenTK.Mathematics;

namespace LumaDX;

public class Collision
{
    public Vector3 eRadius;

    public Vector3 position;

    private bool checkGrounded;
    private bool grounded;

    public Maths.Triangle[] world;
    public List<Maths.Triangle> toCheck;

    private Vector3 newGrav;

    /// <summary>
    /// Get a list of all the triangles which are close enough to the player (reject every triangle that we definitely aren't colliding with).
    /// This also accounts for the player's velocity, as if we are moving really fast, there's a lot more triangles we are potentially colliding with
    /// </summary>
    /// <param name="pos">The player's current position</param>
    /// <param name="vel">The player's current velocity</param>
    private List<Maths.Triangle> GetCloseTriangles(Vector3 pos, Vector3 vel)
    {
        // usually when calculating distances, you would use a Sqrt() operation
        // this is inefficient, so instead we do comparisons with all the distances squared (ignoring the square root step)
        
        List<Maths.Triangle> closeTriangles = new List<Maths.Triangle>();
        foreach (var triangle in world)
        {
            // here we find an approximation of the distance from the player to this triangle
            // we do this by working out the maximum possible distance from the triangle ignoring its rotation
            // essentially we make a sphere which contains all 3 points of the triangle, and then work out the distance from the player to the sphere
            
            float distToTriangle = (pos - triangle.Center).LengthSquared; // the distance (squared) between the player and the triangle
            float speed = vel.LengthSquared; // the player's current velocity (squared), which increases the range for triangles to check
            float sphereRadius; // the radius (squared) of the sphere containing the triangle's points
            
            
            #region Find Sphere
            
            // find distances (squared) from the centre of the triangle to the 3 points
            float p0dist = (triangle.Point0 - triangle.Center).LengthSquared;
            float p1dist = (triangle.Point1 - triangle.Center).LengthSquared;
            float p2dist = (triangle.Point2 - triangle.Center).LengthSquared;
            
            // maximum radius (squared) from the centre of the triangle to a vertex of the triangle
            sphereRadius = Maths.Max(p0dist, p1dist, p2dist);

            #endregion
            

            #region Set Minimums
            
            // if the radius we found, or the speed is too small, it will give us inaccurate results
            // hence, we set a minimum value for the size of the sphere which contains our triangle
            
            if (sphereRadius < Constants.SmallestDistanceApproximation)
                sphereRadius = Constants.SmallestDistanceApproximation;

            if (speed < Constants.SmallestDistanceApproximation)
                speed = Constants.SmallestDistanceApproximation;
            
            #endregion
            
            // if we couldn't possibly be colliding with this, ignore it
            if (distToTriangle > sphereRadius + speed) continue;
            
            // otherwise add it to the list of possible triangles
            closeTriangles.Add(triangle);
        }

        return closeTriangles;
    }
    
    
    
    public (bool,Vector3) CollideAndSlide(Vector3 vel, Vector3 gravity)
    {

        checkGrounded = false;
        
        float velocityLenSquared = Vector3.Dot(vel, vel);
        float gravityLenSquared = Vector3.Dot(gravity, gravity);


        Vector3 eSpacePosition = position / eRadius;
        Vector3 eSpaceVelocity = vel / eRadius;

        Vector3 finalPosition = eSpacePosition;
        
        // if we aren't moving, don't check horizontal collisions
        if (velocityLenSquared != 0.0f && !float.IsNaN(velocityLenSquared))
        {

            toCheck = GetCloseTriangles(finalPosition, eSpaceVelocity);
            finalPosition = CollideWithWorld(eSpacePosition,eSpaceVelocity );
            
        }

        grounded = true;
        if (!float.IsNaN(gravityLenSquared)) // we may still want this step if gravity == 0.0 for the grounded check (walking off a platform)
        {
            eSpaceVelocity = gravity / eRadius;
            newGrav = eSpaceVelocity;
            float len = newGrav.Length;
            if (len > 0f) GravityDirection = newGrav.Normalized();

            checkGrounded = true;
            grounded = false;
   
            toCheck = GetCloseTriangles(finalPosition, eSpaceVelocity);
            finalPosition = CollideWithWorld(finalPosition, eSpaceVelocity);
        }
        

        finalPosition *= eRadius;

        position = finalPosition;

        return (grounded,newGrav*eRadius);
    }
    

    public Vector3 CollideWithWorld(Vector3 pos, Vector3 vel, int recursionDepth = 0)
    {
        if (recursionDepth > 5) return pos;

        var (point,distance) = CheckCollision(pos, vel);
        if (distance >= float.PositiveInfinity) return pos + vel; // if we didn't find any collisions, you are free to move

        // collision

        Vector3 destinationPoint = pos + vel;
        Vector3 newBasePoint = pos;
        
        // prevent player from getting too close to the object
        if (distance <= Constants.CollisionAccuracy)
        {
            Vector3 v = vel.Normalized();
            float newLength = (distance - Constants.CollisionAccuracy);
            newBasePoint = pos + v*newLength;

            // move sliding plane back against the player
            point -= Constants.CollisionAccuracy * v;
        }

        Vector3 slidePlaneOrigin = point;
        Vector3 slidePlaneNormal = newBasePoint - point;
        slidePlaneNormal.Normalize();
        Maths.Plane slidingPlane = new Maths.Plane(slidePlaneNormal, Vector3.Dot(slidePlaneNormal, slidePlaneOrigin));

        Vector3 newDestinationPoint = destinationPoint - slidingPlane.SignedDistance(destinationPoint) * slidePlaneNormal;
        Vector3 newVelocity = newDestinationPoint - point;

        float newVelLenSquared = newVelocity.LengthSquared;
        if (newVelLenSquared < Constants.CollisionAccuracy*Constants.CollisionAccuracy) return newBasePoint;
        //if (newVelLenSquared > 1f) newVelocity /= MathF.Sqrt(newVelLenSquared);

        checkGrounded = false;
        return CollideWithWorld(newBasePoint,newVelocity, recursionDepth+1);
    }

    public Vector3 GravityDirection = -Vector3.UnitY;

    public (Vector3,float) CheckCollision(Vector3 positionE, Vector3 velocity)
    {
        // defining functions here so we don't have to pass in position and velocity
        
        bool CheckEdge(Vector3 p0, Vector3 p1, ref float t, ref Vector3 collisionPoint)
        {
            Vector3 edge = p1 - p0;
            Vector3 posToVertex = p0 - positionE;
        
            float edgeSquaredLength = Vector3.Dot(edge,edge);

            float edgeDotVelocity = Vector3.Dot(edge,velocity);
            float edgeDotBaseToVertex = Vector3.Dot(edge,posToVertex);

            float a, b, c;
            // Calculate parameters for equation
            a = edgeSquaredLength*-velocity.LengthSquared +
                edgeDotVelocity*edgeDotVelocity;
            b = edgeSquaredLength*(2f*Vector3.Dot(velocity,posToVertex))-
                2f*edgeDotVelocity*edgeDotBaseToVertex;
            c = edgeSquaredLength*(1f-posToVertex.LengthSquared)+
                edgeDotBaseToVertex*edgeDotBaseToVertex;
            // Does the swept sphere collide against infinite edge?
        
            if (Maths.GetLowestRoot(a,b,c, t, out float newT) && newT < t) {
                // Check if intersection is within line segment:
                float f=(edgeDotVelocity*newT-edgeDotBaseToVertex)/
                        edgeSquaredLength;
                if (f >= 0.0 && f <= 1.0) {
                    // intersection took place within segment.
                    t = newT;
                    collisionPoint = p0 + f*edge;
                    return true;
                }
            }

            return false;
        }
        
        
        
        
        
        
        
        
        
        
        float nearestDistance = float.PositiveInfinity;
        Vector3 intersectionPoint = Vector3.Zero;


        float velocitySquaredLength = velocity.LengthSquared;

        foreach (var triangle in toCheck)
        {

            if (checkGrounded && !grounded && Vector3.Dot(positionE - triangle.Center, GravityDirection) < 0f) // only check triangles in the direction of gravity
            {
                // lambda for the intersection of the line (pos + lambda x direction) and the plane
                float lambda = (triangle.Plane.Value - Vector3.Dot(triangle.Plane.Normal, positionE)) / Vector3.Dot(triangle.Plane.Normal, GravityDirection);
                Vector3 point = positionE + lambda * GravityDirection;
                Vector3 pointToPos = positionE - point;
                if (pointToPos.LengthSquared < 1.5 && Maths.CheckPointInTriangle(triangle, point))
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
            
            
            float signedDistToTrianglePlane = triangle.Plane.SignedDistance(positionE);
   

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
            bool colliding = false;
            float t = 1f;

            if (!embeddedInPlane)
            {
                Vector3 planeIntersectionPoint = (positionE - triangle.Plane.Normal) + t0 * velocity;

                if (Maths.CheckPointInTriangle(triangle, planeIntersectionPoint))
                {
                    colliding = true;
                    t = t0;
                    collisionPoint = planeIntersectionPoint;
          
                }

            }



            if (!colliding)
            {

                float newT;

                float a, b, c;
                // Check against points:
                a = velocitySquaredLength;
                
                #region Point 0
                
                // P0
                b = 2f*(Vector3.Dot(velocity,positionE-triangle.Point0));
                c = (triangle.Point0-positionE).LengthSquared - 1f;
                if (Maths.GetLowestRoot(a,b,c, t, out newT) && newT < t) {
                    t = newT;
                    colliding = true;
                    collisionPoint = triangle.Point0;
                }
                
                #endregion
                
                #region Point 1

                // P1
                b = 2f*(Vector3.Dot(velocity,positionE-triangle.Point1));
                c = (triangle.Point1-positionE).LengthSquared - 1f;
                if (Maths.GetLowestRoot(a,b,c, t, out newT) && newT < t) {
                    t = newT;
                    colliding = true;
                    collisionPoint = triangle.Point1;
                }
                
                #endregion
                
                #region Point 2
                
                // P2
                b = 2f*(Vector3.Dot(velocity,positionE-triangle.Point2));
                c = (triangle.Point2-positionE).LengthSquared - 1f;
                if (Maths.GetLowestRoot(a,b,c, t, out newT) && newT < t) {
                    t = newT;
                    colliding = true;
                    collisionPoint = triangle.Point2;
                }
                
                #endregion
                
                    
                // Check against edges:
                
                if (CheckEdge(triangle.Point0,triangle.Point1, ref t, ref collisionPoint)) colliding = true;
                if (CheckEdge(triangle.Point1,triangle.Point2, ref t, ref collisionPoint)) colliding = true;
                if (CheckEdge(triangle.Point2,triangle.Point0, ref t, ref collisionPoint)) colliding = true;
                



            }

            if (!colliding) continue;
                
            float distToCollision = t * MathF.Sqrt(velocitySquaredLength);
            if (!(distToCollision < nearestDistance)) continue; // if we already found a closer collision, ignore this
            
            
            nearestDistance = distToCollision;
            intersectionPoint = collisionPoint;
        }

        return (intersectionPoint, nearestDistance);
    }
}