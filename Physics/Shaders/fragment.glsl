#version luma-dx

in vec3 vertexColour;
uniform vec3 colour;

[cube]
void main()
{
    lx_FragColour = vec4(vertexColour,1.0);
}

[sphere]
void main()
{
    lx_FragColour = vec4(colour,1.0);
}