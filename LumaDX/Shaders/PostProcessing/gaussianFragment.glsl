#version 330 core

uniform sampler2D texture0;
in vec2 texCoords;

uniform int blurDirection; // 0 for horizontal, 1 for vertical

uniform float weight[5] = float[] (0.227027, 0.1945946, 0.1216216, 0.054054, 0.016216);

out vec4 brightColour;

void main()
{
    
    vec2 tex_offset = 1.0 / textureSize(texture0, 0); // gets size of single texel
    vec3 colour = texture(texture0, texCoords).rgb * weight[0]; // current fragment's contribution
    
    if(blurDirection==0) // horizontal
    {
        for(int i = 1; i < 5; ++i)
        {
            colour += texture(texture0, texCoords + vec2(tex_offset.x * i, 0.0)).rgb * weight[i];
            colour += texture(texture0, texCoords - vec2(tex_offset.x * i, 0.0)).rgb * weight[i];
        }
    }
    else // vertical
    {
        for(int i = 1; i < 5; ++i)
        {
            colour += texture(texture0, texCoords + vec2(0.0, tex_offset.y * i)).rgb * weight[i];
            colour += texture(texture0, texCoords - vec2(0.0, tex_offset.y * i)).rgb * weight[i];
        }
    }
    
    brightColour = vec4(colour,1.0);

}