#version luma-dx

in vec2 texCoords;
in vec3 fragPos;
in vec3 normal;
in vec4 fragPosLightSpace;

uniform lx_Light light;
uniform lx_Material material;
uniform sampler2D depthMap;
uniform vec3 cameraPos;

void main()
{
    float shadow = lx_ShadowCalculation(depthMap,fragPosLightSpace,1.0,4,20.0);
    lx_FragColour = lx_Colour(lx_Phong(normal, fragPos, cameraPos, texCoords, material, light, shadow));
}