// -------------------- fragment.glsl -------------------- //

// this is a standard shader needed to render ImGui to my OpenGL context

#version 330 core

uniform sampler2D fontTex;

in vec4 colour;
in vec2 texCoords;

out vec4 fragColour;

void main()
{
    vec4 fontSample = texture(fontTex, texCoords);
    fragColour = colour * fontSample;
}