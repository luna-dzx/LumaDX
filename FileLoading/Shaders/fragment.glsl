#version luma-dx

uniform sampler2D dingus;

in vec3 fragPos;
in vec3 normal;
in vec3 colour;

void main()
{

    if (dot(normalize(normal), vec3(0.0,0.0,-1.0)) > 0.0) discard;

    lx_FragColour = vec4(colour, 1.0);
}