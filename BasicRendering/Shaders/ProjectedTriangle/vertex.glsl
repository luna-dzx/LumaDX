// --------------- ProjectedTriangle/vertex.glsl --------------- //

#version luma-dx
layout (location = 0) in vec3 inPosition;

void main()
{
    gl_Position = lx_Transform * vec4(inPosition , 1.0);
}