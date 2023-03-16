// --------------- Stairs/vertex.glsl --------------- //

#version luma-dx
layout (location = 0) in vec3 inPosition;
layout (location = 3) in mat4 instanceMatrix;
layout (location = 7) in vec3 inColour;

out vec3 vertexColour;
uniform mat4 worldTransform;

[cube]
void main()
{
    vertexColour = inColour;
    gl_Position = lx_Transform * worldTransform * instanceMatrix * vec4(inPosition , 1.0);
}

[sphere]
void main()
{
    gl_Position = lx_Transform * vec4(inPosition , 1.0);
}