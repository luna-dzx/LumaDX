// -------------------- vertex.glsl -------------------- //

#version luma-dx

layout (location = 0) in vec4 vertex; // (position), (texCoords)

out vec2 texCoords;

uniform vec2 screenSize;

void main()
{
    gl_Position = vec4(2.0 * (vertex.xy / screenSize) - vec2(1.0), 0.0, 1.0);
    texCoords = vertex.zw;
}  