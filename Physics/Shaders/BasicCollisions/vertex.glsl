#version luma-dx
layout (location = 0) in vec3 aPos;
layout (location = 1) in vec2 inTexCoords;

out vec2 texCoords;

void main()
{
    texCoords = inTexCoords;
    gl_Position = lx_Transform * vec4(aPos , 1.0);
}