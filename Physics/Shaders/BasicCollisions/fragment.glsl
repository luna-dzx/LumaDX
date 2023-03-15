#version luma-dx

uniform sampler2D dingus;
uniform vec3 colour;

in vec2 texCoords;

[dingus]
void main()
{
    lx_FragColour = texture(dingus,texCoords);
}

[sphere]
void main()
{
    lx_FragColour = lx_Colour(colour);
}