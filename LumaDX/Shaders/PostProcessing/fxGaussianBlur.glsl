// -------------------- fxGaussianBlur.glsl -------------------- //

#version 330 core

uniform sampler2D texture0;
in vec2 texCoords;

uniform int blurDirection;

uniform float weight[5] = float[] (0.227027, 0.1945946, 0.1216216, 0.054054, 0.016216);

out vec4 blurColour;

void main()
{
    // pixel size to blur by number of pixels
    vec2 tex_offset = 1.0 / textureSize(texture0, 0);
    vec4 colour = texture(texture0, texCoords) * weight[0];
    
    if(blurDirection==0) // horizontal
    {
        for(int i = 1; i < 5; ++i)
        {
            colour += texture(texture0, texCoords + vec2(tex_offset.x * i, 0.0)) * weight[i];
            colour += texture(texture0, texCoords - vec2(tex_offset.x * i, 0.0)) * weight[i];
        }
    }
    else // vertical
    {
        for(int i = 1; i < 5; ++i)
        {
            colour += texture(texture0, texCoords + vec2(0.0, tex_offset.y * i)) * weight[i];
            colour += texture(texture0, texCoords - vec2(0.0, tex_offset.y * i)) * weight[i];
        }
    }
    
    blurColour = colour;
}