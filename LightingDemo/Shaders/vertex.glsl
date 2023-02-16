#version luma-dx
layout (location = 0) in vec3 aPos;

void main()
{
    gl_Position = lx_Transform * vec4(aPos, 1.0);
}