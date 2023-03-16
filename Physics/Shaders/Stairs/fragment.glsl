// --------------- Stairs/fragment.glsl --------------- //

#version luma-dx

in vec3 vertexColour;
uniform vec3 colour;

[cube]
void main()
{
    lx_FragColour = lx_Colour(vertexColour);
}

[sphere]
void main()
{
    lx_FragColour = lx_Colour(colour);
}