#version luma-dx

in vec2 texCoords;
in vec3 fragPos;
in vec3 normal;
in vec4 fragPosLightSpace;

uniform lx_Light light;
uniform lx_Material material;
uniform sampler2D depthMap;
uniform vec3 cameraPos;

uniform sampler2D sceneSample;

[scene]
void main()
{
    float shadow = lx_ShadowCalculation(depthMap,fragPosLightSpace,1.0,4,20.0);
    lx_FragColour = lx_Colour(lx_Phong(normal, fragPos, cameraPos, texCoords, material, light, shadow));
}

[portal]
void main()
{
    vec3 col = texture(sceneSample, gl_FragCoord.xy / vec2(1600.0,900.0)).xyz;
    if (lx_IsGammaCorrectionEnabled)
    {
        col = lx_GammaCorrect(col,2.2);
    }
    lx_FragColour = lx_Colour(col);
}

[point]
void main()
{
    lx_FragColour = vec4(1.0,0.0,0.0,1.0);
}