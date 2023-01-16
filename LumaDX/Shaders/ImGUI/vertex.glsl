#version 330 core

uniform vec2 screenSize;

layout(location = 0) in vec2 in_position;
layout(location = 1) in vec2 in_texCoord;
layout(location = 2) in vec4 in_colour;

out vec4 colour;
out vec2 texCoord;

void main()
{
    vec2 glPos = ((in_position / screenSize) * 2.0 - 1.0) * vec2(1.0,-1.0);
    gl_Position = vec4(glPos, 0, 1);
    colour = in_colour;
    texCoord = in_texCoord;
}