// -------------------- fragment.glsl -------------------- //

#version luma-dx

uniform samplerCube skyBox;
uniform sampler2D dingus;

in vec3 fragPos;
in vec3 normal;
in vec3 textureDir;
in vec2 texCoords;

uniform vec3 cameraPos;

[scene]
void main()
{
    lx_FragColour = texture(dingus,texCoords);
}

[skyBox]
void main()
{
    lx_FragColour = texture(skyBox,textureDir);
}