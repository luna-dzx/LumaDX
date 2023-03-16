// --------------- FirstPersonCollisions/fragment.glsl --------------- //

#version luma-dx

in vec2 texCoords;
in vec3 fragPos;
in vec3 normal;
in vec4 fragPosLightSpace;
in vec3 textureDir;

uniform lx_Light light;
uniform lx_Material material;
uniform sampler2D depthMap;
uniform vec3 cameraPos;
uniform samplerCube skyBox;

[scene]
void main()
{
    float shadow = lx_ShadowCalculation(depthMap,fragPosLightSpace,1.0,4,20.0);
    lx_FragColour = lx_Colour(lx_Phong(normal, fragPos, cameraPos, texCoords, material, light, shadow));
}

[point]
void main()
{
    lx_FragColour = vec4(1.0,0.0,0.0,1.0);
}

[skyBox]
void main()
{
    lx_FragColour = texture(skyBox,textureDir * vec3(-1.0,1.0,-1.0));
}