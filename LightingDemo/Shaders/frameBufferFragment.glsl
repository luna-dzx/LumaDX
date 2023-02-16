#version 330 core

uniform sampler2D texture0;
in vec2 texCoords;

out vec4 fragColour;

void main()
{
    fragColour = texture(texture0,texCoords);
}