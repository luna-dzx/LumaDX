#version luma-dx

uniform sampler2D texture0;
in vec2 texCoords;

void main()
{
    lx_FragColour = lx_Colour(lx_ApplyHDR(texture(texture0,texCoords).rgb,1.0,2.2));
}