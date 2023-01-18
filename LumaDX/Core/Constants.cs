namespace LumaDX;

public static class Constants
{
    public const string LibraryShaderPath = "Shaders/LumaDX/";

    public const int ImGuiVertSize = 20;
    
    public const float CollisionAccuracy = 0.001f; // base case for recursive collisions, closest to check
    public const float SmallestDistanceApproximation = 1f; // smallest distance squared to check when rejecting triangles for collisions
}