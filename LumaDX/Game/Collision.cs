using OpenTK.Mathematics;

namespace LumaDX;

public static class Collision
{
    public static Maths.Triangle[] World = Array.Empty<Maths.Triangle>();

    /// <summary>
    /// Get a list of all the triangles which are close enough to the player (reject every triangle that we definitely aren't colliding with).
    /// This also accounts for the player's velocity, as if we are moving really fast, there's a lot more triangles we are potentially colliding with
    /// </summary>
    /// <param name="pos">The player's current position</param>
    /// <param name="vel">The player's current velocity</param>
    private static List<Maths.Triangle> GetCloseTriangles(Vector3 pos, Vector3 vel)
    {
        // usually when calculating distances, you would use a Sqrt() operation
        // this is inefficient, so instead we do comparisons with all the distances squared (ignoring the square root step)
        
        List<Maths.Triangle> closeTriangles = new List<Maths.Triangle>();
        foreach (var triangle in World)
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
    
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="vel"></param>
    /// <param name="gravity"></param>
    /// <param name="grounded"></param>
    /// <returns></returns>
    public static (Vector3,Vector3) CollideAndSlide(Vector3 position, Vector3 vel, Vector3 gravity, Vector3 eRadius, ref bool grounded)
    {
        // Adjust scene relative to ellipsoid scale in order to treat the ellipsoid as a sphere
        position /= eRadius;
        vel /= eRadius;

        List<Maths.Triangle> toCheck;
        
        #region Horizontal

        // if we aren't moving, don't check horizontal collisions
        if (vel.LengthSquared != 0.0f && !float.IsNaN(vel.LengthSquared))
        {

            toCheck = GetCloseTriangles(position, vel);
            position = CollideWithWorld(position,vel, ref toCheck);
            
        }
        
        #endregion

        #region Vertical
        
        grounded = true;
        Vector3 newGrav = gravity;
        if (!float.IsNaN(gravity.LengthSquared)) // we may still want this step if gravity == 0.0 for the grounded check (walking off a platform)
        {
            vel = gravity / eRadius;
            newGrav = vel;
            float len = newGrav.Length;
            if (len > 0f) GravityDirection = newGrav.Normalized();
            
            grounded = false;
   
            toCheck = GetCloseTriangles(position, vel);
            
            #region Grounded Check

            foreach (var triangle in toCheck)
            {
                if (grounded || !(Vector3.Dot(position - triangle.Center, GravityDirection) < 0f)) continue; // only check triangles in the direction of gravity
                
                // lambda for the intersection of the line (pos + lambda x direction) and the plane
                float lambda = (triangle.Plane.Value - Vector3.Dot(triangle.Plane.Normal, position)) / Vector3.Dot(triangle.Plane.Normal, GravityDirection);
                Vector3 point = position + lambda * GravityDirection;
                Vector3 pointToPos = position - point;
                if (!(pointToPos.LengthSquared < 1.5) || !Maths.CheckPointInTriangle(triangle, point)) continue;
                
                // hitting your head on a roof
                if (GravityDirection.Y > 0)
                {
                    newGrav = Vector3.Zero;
                    continue;
                }

                // hitting the ground
                grounded = true;
            }
            
            
            #endregion

            position = CollideWithWorld(position, vel, ref toCheck);
        }
        
        #endregion

        return (position * eRadius, newGrav * eRadius);
    }
    

    public static Vector3 CollideWithWorld(Vector3 pos, Vector3 vel, ref List<Maths.Triangle> toCheck, int recursionDepth = 0)
    {
        // base case of maximum accuracy (otherwise could go forever bouncing between 2 walls if we're stuck)
        if (recursionDepth > 5) return pos;

        
        // find the closest point of intersection with the scene
        var (intersection,distToIntersection) = SceneIntersection(pos, vel, ref toCheck);
        
        // if we never intersected with the scene, move
        if (distToIntersection >= float.PositiveInfinity) return pos + vel;
        
        
        #region Collision
        
        Vector3 destinationPoint = pos + vel;

        // If player extremely close to scene
        #region Move Player Out Of Scene

        if (distToIntersection <= Constants.CollisionAccuracy)
        {
            // redirect vector away from surface, and move the position here
            Vector3 v = vel.Normalized();
            float newLength = (distToIntersection - Constants.CollisionAccuracy);
            pos += v*newLength;

            // move sliding plane back against the player
            intersection -= Constants.CollisionAccuracy * v;
        }
        
        #endregion
        
        
        // by here, we have moved until we are pushed up against the triangle and we now want to slide against the triangle
        
        Maths.Plane slidingPlane = Maths.Plane.FromNormalAndPoint((pos - intersection), intersection);

        // from the intersection point to destination along the sliding plane
        vel = (destinationPoint - slidingPlane.SignedDistance(destinationPoint) * slidingPlane.Normal) - intersection;
        
        // if we're already close enough to our destination, return
        if (vel.LengthSquared < Constants.CollisionAccuracy*Constants.CollisionAccuracy) return pos;

        // recursively check for more collisions with the new position and velocity
        return CollideWithWorld(pos,vel, ref toCheck, recursionDepth+1);
        
        #endregion
    }

    public static Vector3 GravityDirection = -Vector3.UnitY;

    /// <summary>
    /// Check Intersections with the Entire Scene
    /// </summary>
    /// <param name="position">The Player's Position</param>
    /// <param name="velocity"><The Player's Velocity/param>
    /// /// <returns>IntersectionPoint (closest point of intersection with all of the triangles), Distance (to intersection point)</returns>
    public static (Vector3,float) SceneIntersection(Vector3 position, Vector3 velocity, ref List<Maths.Triangle> toCheck)
    {
        float velocitySquaredLength = velocity.LengthSquared;
        
        #region Local Functions
        
        // defining functions here so we don't have to pass in position and velocity, etc.

        bool CheckPoint(Vector3 point, ref float lowestCollisionTime, ref Vector3 collisionPoint)
        {
            float a, b, c;
            
            a = velocitySquaredLength;
            b = 2f*(Vector3.Dot(velocity,position-point));
            c = (point-position).LengthSquared - 1f;
            if (Maths.GetLowestRoot(a,b,c, lowestCollisionTime, out float collisionTime)) {
                lowestCollisionTime = collisionTime;
                collisionPoint = point;
                return true;
            }

            return false;
        }
        

        bool CheckEdge(Vector3 p0, Vector3 p1, ref float lowestCollisionTime, ref Vector3 collisionPoint)
        {
            // forming an infinite line which the 2 vertices lie on
            Vector3 lineDir = p1 - p0;
            Vector3 posToLine = p0 - position;
        
            // length squared of the edge between the 2 vertices
            float lineLength = Vector3.Dot(lineDir,lineDir);
            
            float dirDotVel = Vector3.Dot(lineDir,velocity);
            float lineDotPos = Vector3.Dot(lineDir,posToLine);

            float a, b, c;
            // Form quadratic for the 2 (or less) intersection points of the sphere and the line
            // (the line being that which passes through both vertices of the edge)
            a = lineLength*-velocity.LengthSquared + dirDotVel*dirDotVel;
            b = lineLength*(2f*Vector3.Dot(velocity,posToLine))- 2f*dirDotVel*lineDotPos;
            c = lineLength*(1f-posToLine.LengthSquared)+ lineDotPos*lineDotPos;

            // if there was an intersection between 0 < t < collisionTime (where t is how far we are travelling across the velocity vector)
            if (Maths.GetLowestRoot(a,b,c, lowestCollisionTime, out float collisionTime)) {

                // "intersection" is a value between 0 and 1 representing where along the line the intersection took place
                float intersection =(dirDotVel*collisionTime-lineDotPos)/ lineLength;

                // if this intersection was on the triangle's edge (and not on the rest of this line, which stretches out to infinity)
                if (intersection >= 0.0 && intersection <= 1.0) {
                    lowestCollisionTime = collisionTime; // set a new lowest collision time
                    collisionPoint = p0 + intersection*lineDir; // move along the line to the point of intersection
                    return true;
                }
            }
            
            // otherwise, discard this result
            return false;
        }

        #endregion

        float nearestDistance = float.PositiveInfinity;
        Vector3 intersectionPoint = Vector3.Zero;

        foreach (var triangle in toCheck)
        {
            float signedDistToTrianglePlane = triangle.Plane.SignedDistance(position);
            float normalDotVelocity = Vector3.Dot(triangle.Plane.Normal, velocity);

            // value between 0 and 1 representing how far along the velocity vector the collision happens
            float collisionTime = 1f;
            
            // bounds of collisionTime
            float timeMin = 0;
            float timeMax = 0;

            #region Collision Time Bounds
            
            bool embeddedInPlane = false;

            if (MathF.Abs(normalDotVelocity) == 0.0f) // normal is perpendicular to velocity
            {
                if (MathF.Abs(signedDistToTrianglePlane) > 1f) continue; // impossible to collide

                // otherwise, this must be an embedded sphere, which can only
                // collide against a vertex or an edge, not the whole triangle
                timeMin = 0f;
                timeMax = 1f;
                embeddedInPlane = true;
            }
            else
            {
                timeMin = (1f - signedDistToTrianglePlane) / normalDotVelocity;
                timeMax = (-1f - signedDistToTrianglePlane) / normalDotVelocity;

                if (timeMin > timeMax)
                {
                    (timeMin, timeMax) = (timeMax, timeMin);
                } // swap so t0 is smaller;

                if (timeMin > 1f || timeMax < 0f)
                {
                    // impossible for the sphere to collide with this triangle
                    continue; // continue to next triangle
                }

                // time represents distance across the velocity vector so impossible for this to be outside the range 0,1 if there was a collision
                // (only check for collisions which lie along the velocity vector)
                timeMin = Math.Clamp(timeMin, 0f, 1f);
            }
            
            #endregion
            
            Vector3 collisionPoint = Vector3.Zero;
            bool colliding = false;

            #region Touching Triangle
            
            if (!embeddedInPlane) // if there was a single intersection point, we can skip the heavier calculations
            {
                // find the single intersection point
                Vector3 planeIntersectionPoint = (position - triangle.Plane.Normal) + timeMin * velocity;

                // if this point actually lies within the triangle
                if (Maths.CheckPointInTriangle(triangle, planeIntersectionPoint))
                {
                    colliding = true;
                    collisionTime = timeMin;
                    collisionPoint = planeIntersectionPoint;
                }
            }
            
            #endregion

            #region Points and Edges
            if (!colliding)
            {
                // Check against points:
                if(CheckPoint(triangle.Point0,ref collisionTime, ref collisionPoint)) colliding = true;
                if(CheckPoint(triangle.Point1,ref collisionTime, ref collisionPoint)) colliding = true;
                if(CheckPoint(triangle.Point2,ref collisionTime, ref collisionPoint)) colliding = true;

                // Check against edges:
                if (CheckEdge(triangle.Point0,triangle.Point1, ref collisionTime, ref collisionPoint)) colliding = true;
                if (CheckEdge(triangle.Point1,triangle.Point2, ref collisionTime, ref collisionPoint)) colliding = true;
                if (CheckEdge(triangle.Point2,triangle.Point0, ref collisionTime, ref collisionPoint)) colliding = true;
                
                // After this, if there was a collision, collisionTime will be the minimum of all 3 points and all 3 edges,
                // with collisionPoint being the closest to the player's current position
            }
            #endregion

            #region Final Checks
            
            // we have checked all valid ways of colliding, so if this is still false we must not be colliding with this triangle
            if (!colliding) continue;
                
            // was this the closest collision?
            float distToCollision = collisionTime * MathF.Sqrt(velocitySquaredLength);
            if (!(distToCollision < nearestDistance)) continue; // if we already found a closer collision, ignore this
            
            // set this as the new closest collision and carry on
            nearestDistance = distToCollision;
            intersectionPoint = collisionPoint;
            
            #endregion
        }

        // nearestDistance -> the closest distance to all of the triangles we checked
        // intersectionPoint -> the point at which we are colliding with the closest of these triangles
        return (intersectionPoint, nearestDistance);
    }
}