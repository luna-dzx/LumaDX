#version luma-dx

uniform lx_Material material;
uniform lx_Light light;
uniform sampler2D normalMap;
uniform sampler2D displaceMap;

in VS_OUT {
    vec3 fragPos;
    vec3 TBNnormal;
    vec2 texCoords;
    vec3 TBNfragPos;
    vec3 TBNlightPos;
    vec3 tangent;
} fs_in;

uniform float heightScale;
uniform int doNormalMapping;

[mapping]
void main()
{
    vec2 texCoords = lx_ParallaxMapping(displaceMap, fs_in.texCoords,  normalize(- fs_in.TBNfragPos), heightScale, 8.0, 32.0);
    if(texCoords.x > 1.0 || texCoords.y > 1.0 || texCoords.x < 0.0 || texCoords.y < 0.0) discard;

    vec3 normal;
    if (doNormalMapping == 1) normal = lx_NormalMap(normalMap,texCoords);
    else normal = fs_in.TBNnormal;
    vec3 phong = lx_Phong(normal, fs_in.TBNfragPos, vec3(0.0), texCoords, texCoords, material, lx_MoveLight(light,fs_in.TBNlightPos), 1.0);
    lx_FragColour = lx_Colour(phong);
}

uniform int visualise;

[2d]
void main()
{
    if (visualise == 0)
    {
        lx_FragColour = lx_Colour(lx_GammaCorrect(texture(material.baseTex,fs_in.texCoords).rgb,2.2));
    }
    if (visualise == 1)
    {
        lx_FragColour = lx_Colour(lx_GammaCorrect(texture(material.specTex,fs_in.texCoords).rgb,2.2));
    }
    if (visualise == 2)
    {
        lx_FragColour = lx_Colour(lx_GammaCorrect(texture(normalMap,fs_in.texCoords).rgb,2.2));
    }
    if (visualise == 3)
    {
        lx_FragColour = lx_Colour(lx_GammaCorrect(texture(displaceMap,fs_in.texCoords).rgb,2.2));
    }
}

uniform vec2 mousePos;
uniform vec2 textureSize;
uniform vec2 screenSize;

[frameBuffer]
void main()
{
    if (length(gl_FragCoord.xy/textureSize - mousePos) > 0.1) discard;
    lx_FragColour = vec4(1.0,0.0,0.0,1.0);
    
    //lx_FragColour = vec4(gl_FragCoord.xy/textureSize,0.5,1.0);
    //lx_FragColour = lx_Colour(lx_GammaCorrect(texture(normalMap,fs_in.texCoords).rgb,2.2));
}