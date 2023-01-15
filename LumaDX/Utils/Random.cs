using OpenTK.Mathematics;

namespace LumaDX;

public class RandUtils
{
    
    /// <summary>
    /// Pseudo-random float array representing a texture with 0 in the z component and no repeating values
    /// </summary>
    /// <param name="width">Texture Width</param>
    public static float[] SsaoNoise(int width)
    {
        Random rand = new Random();
        int arrLength = width * width * 3;
        return
            Enumerable.Range(0, arrLength+1) // integer values from 0 to n
            .Select(a => -1 + 2f* a / arrLength) // change to float values between -1 and 1
            .OrderBy(_ => rand.Next(arrLength)) // order by random numbers
            .Select((a,i) => (i%3 == 2)? 0:a) // set every z component to 0
            .ToArray(); // to float[]
    }


    /// <summary>
    /// Positive z hemisphere of sample positions
    /// </summary>
    /// <param name="count">num samples</param>
    public static Vector3[] SsaoKernel(int count)
    {
        Vector3[] ssaoKernel = new Vector3[count];
        Random rand = new Random();
        
        // random vectors in positive Z and any X,Y direction, of length 0.0 to 1.0
        // (random positions in hemisphere of positive Z)
        for (int i = 0; i < count; i++)
        {
            // scale for favouring points closer to the centre of the hemisphere
            float scale = (float)i/64f;
            // interpolate
            scale = Maths.Lerp(0.1f, 1.0f, scale * scale);

            ssaoKernel[i] = (
                new Vector3(
                    (float)rand.NextDouble() * 2f - 1f,
                    (float)rand.NextDouble() * 2f - 1f,
                    (float)rand.NextDouble()
                ).Normalized()
            ) * (float)rand.NextDouble() * scale;
        }
        
        return ssaoKernel;
    }
    
    
    
    
    
    
}