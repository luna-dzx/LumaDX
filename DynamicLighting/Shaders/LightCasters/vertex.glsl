// --------------- LightCasters/vertex.glsl --------------- //

#version luma-dx
layout (location = 0) in vec3 inPosition;
layout (location = 2) in vec3 inNormal;

out vec3 fragPos;
out vec3 normal;

void main()
{
    fragPos = (lx_Model * vec4(inPosition, 1.0)).xyz;
    normal = lx_NormalFix(lx_Model,inNormal);
    gl_Position = lx_Transform * vec4(inPosition , 1.0);
}