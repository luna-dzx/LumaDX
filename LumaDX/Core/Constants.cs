namespace LumaDX;

public static class Constants
{
    public const string LibraryShaderPath = "Shaders/LumaDX/";

    public const int ImGuiVertSize = 20;
    
    public const float CollisionAccuracy = 0.001f; // base case for recursive collisions, closest to check (lower for more accuracy)
    public const float SmallestDistanceApproximation = 2f; // smallest distance squared to check when rejecting triangles for collisions (raise for more accuracy)
    public const float GroundedDist = 1.5f; // the furthest distance from the centre of the unit sphere to a triangle where it can be considered grounded
}