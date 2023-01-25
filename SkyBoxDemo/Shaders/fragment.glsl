#version luma-dx

uniform samplerCube skyBox;

in vec3 fragPos;
in vec3 normal;
in vec3 textureDir;

uniform vec3 cameraPos;
uniform float refractionRatio;

[scene]
void main()
{
    vec3 viewDir = normalize(fragPos - cameraPos);
    vec3 refraction = refract(viewDir,normalize(normal), refractionRatio);
    lx_FragColour = texture(skyBox,refraction);
}

[skyBox]
void main()
{
    lx_FragColour = texture(skyBox,textureDir);
}