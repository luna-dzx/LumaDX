#version 330 core

uniform sampler2D fontTex;

in vec4 colour;
in vec2 texCoord;

out vec4 fragColour;

void main()
{
    fragColour = colour * texture(fontTex, texCoord);
}