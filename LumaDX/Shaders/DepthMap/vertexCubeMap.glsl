#version 330 core
layout (location = 0) in vec3 inPosition;

uniform mat4 model;
uniform vec3 offsetPos;

void main() { gl_Position = model*vec4(inPosition, 1.0) - vec4(offsetPos,0.0); }