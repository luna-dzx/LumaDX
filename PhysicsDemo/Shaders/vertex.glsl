#version luma-dx
layout (location = 0) in vec3 aPos;
layout (location = 3) in mat4 instanceMatrix;
layout (location = 7) in vec3 aColour;

out vec3 vertexColour;
uniform mat4 worldTransform;

[cube]
void main()
{
    vertexColour = aColour;
    gl_Position = lx_Transform * worldTransform * instanceMatrix * vec4(aPos , 1.0);
}

[sphere]
void main()
{
    gl_Position = lx_Transform * vec4(aPos , 1.0);
}