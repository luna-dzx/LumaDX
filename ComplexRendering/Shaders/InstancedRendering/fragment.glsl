#version luma-dx

uniform samplerCube skyBox;
uniform sampler2D dingus;

in vec3 fragPos;
in vec3 normal;
in vec3 textureDir;
in vec2 texCoords;
flat in int instanceId;

uniform vec3 cameraPos;
uniform float refractionRatio;

uniform int renderRefraction;

[scene]
void main()
{
    if (instanceId % 2 == 0 && renderRefraction == 1)
    {
        vec3 viewDir = normalize(fragPos - cameraPos);
        vec3 refraction = refract(viewDir,normalize(normal), refractionRatio);
        lx_FragColour = texture(skyBox,refraction);
    }
    else
    {
        lx_FragColour = texture(dingus,texCoords);
    }
}

[skyBox]
void main()
{
    lx_FragColour = texture(skyBox,textureDir);
}