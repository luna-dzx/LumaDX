#version luma-dx

uniform vec3 colour;

void main()
{
    lx_FragColour = vec4(colour,1.0);
}