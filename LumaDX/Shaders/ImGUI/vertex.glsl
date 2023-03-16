// -------------------- vertex.glsl -------------------- //

// this is a standard shader needed to render ImGui to my OpenGL context

#version 330 core

uniform vec2 screenSize;

layout(location = 0) in vec2 inPosition;
layout(location = 1) in vec2 inTexCoords;
layout(location = 2) in vec4 inColour;

out vec4 colour;
out vec2 texCoords;

void main()
{
    colour = inColour;
    texCoord = inTexCoords;
    vec2 screenPos = (inPosition / screenSize) * 2.0 - 1.0;
    gl_Position = vec4(screenPos * vec2(1.0,-1.0), 0, 1);
}