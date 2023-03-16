// -------------------- fragmentCubeMap.glsl -------------------- //

#version 330 core

in vec3 cubePos;

uniform vec3 lightPos;
uniform float farPlane;

void main()
{
    gl_FragDepth = length(cubePos - lightPos) / farPlane;
}