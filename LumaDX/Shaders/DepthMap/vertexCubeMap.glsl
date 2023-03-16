// -------------------- vertexCubeMap.glsl -------------------- //

#version 330 core
layout (location = 0) in vec3 inPosition;

uniform mat4 model;
uniform vec3 offsetPos;

void main()
{
    vec4 pos = model * vec4(inPosition, 1.0);
    gl_Position = vec4(pos.xyz - offsetPos, pos.w);
}