// --------------- PrimitivesRender/vertex.glsl --------------- //

#version luma-dx
layout (location = 0) in vec3 inPosition;

out vec3 fragPos;

void main()
{
    fragPos = inPosition;
    gl_Position = lx_Transform * vec4(inPosition , 1.0);
}