using System;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Mathematics;

namespace LumaDX;

public class Input
{
    private static readonly float OneOverRoot2 = 1f/(float)Math.Sqrt(2);
        
    /// <summary>
    /// Gets the unit vector in x,z of the w,a,s,d inputs for player controllers
    /// </summary>
    /// <param name="keyboardState">the current state of the keyboard to check</param>
    /// <returns>a 3D vector representing the direction pressed</returns>
    public static Vector3 DirectionWASD(KeyboardState keyboardState)
    {
        int forwards = (keyboardState.IsKeyDown(Keys.W)) ? 1 : 0;
        int backwards = (keyboardState.IsKeyDown(Keys.S)) ? 1 : 0;
        int left = (keyboardState.IsKeyDown(Keys.A)) ? 1 : 0;
        int right = (keyboardState.IsKeyDown(Keys.D)) ? 1 : 0;

        float mult = 1f;
        // diagonal
        if (Math.Abs(right - left) + Math.Abs(forwards - backwards) > 1) mult = OneOverRoot2;
        return new Vector3(right-left,0,forwards-backwards) * mult;
    }
}