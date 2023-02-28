#version luma-dx

uniform sampler2D depthMap;
in vec2 texCoords;

void main()
{
    lx_FragColour = vec4(texture(depthMap,texCoords).r);
}