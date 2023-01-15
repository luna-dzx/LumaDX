#version 330 core

uniform mat4 proj;

layout(location = 0) in vec2 in_position;
layout(location = 1) in vec2 in_texCoord;
layout(location = 2) in vec4 in_colour;

out vec4 colour;
out vec2 texCoord;

void main()
{
    gl_Position = proj * vec4(in_position, 0, 1);
    colour = in_colour;
    texCoord = in_texCoord;
}