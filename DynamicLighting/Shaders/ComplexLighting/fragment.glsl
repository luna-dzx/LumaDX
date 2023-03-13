#version luma-dx

uniform samplerCube cubeMap;

uniform lx_Material material;
uniform lx_Light light;

uniform sampler2D normalMap;

layout (location = 0) out vec4 fragColour;
layout (location = 1) out vec4 brightColour;
layout (location = 2) out vec4 outPosition;
layout (location = 3) out vec4 outNormal;

in VS_OUT {
    vec3 fragPos;
    vec3 normal;
    vec2 texCoords;
    vec3 TBNfragPos;
    vec3 TBNlightPos;
    vec3 TBNcameraPos;
    vec3 tangent;
    vec3 textureDir;
    vec3 viewPos;
    vec3 viewNormal;
} fs_in;

uniform vec3 cameraPos;

vec3 CalculateBrightColour(vec3 colour)
{
    float brightness = dot(colour, vec3(0.2126, 0.7152, 0.0722));
    if(brightness > 1.0) { return colour; }
    return vec3(0.0);
}

uniform int flipNormals = 0;
uniform float farPlane;

uniform float shadowThreshold;

uniform int renderShadows;

[scene]
void main()
{
    float shadow = 1.0;
    if (renderShadows == 1) shadow = lx_ShadowCalculation(cubeMap, fs_in.fragPos, light.position, cameraPos, farPlane);
    vec3 normal = lx_NormalMap(normalMap,fs_in.texCoords);
    vec3 viewNormal = fs_in.viewNormal;
    if (flipNormals == 1) normal *= -1.0;
    vec3 phong = lx_Phong(normal, fs_in.TBNfragPos, fs_in.TBNcameraPos, fs_in.texCoords, fs_in.texCoords, material, lx_MoveLight(light,fs_in.TBNlightPos),shadow * shadowThreshold + (1.0 - shadowThreshold));
    fragColour = lx_Colour(phong);
    brightColour = lx_Colour(CalculateBrightColour(fragColour.rgb));
    
    outPosition = lx_Colour(fs_in.viewPos);
    outNormal = lx_Colour(viewNormal);
}

[skyBox]
void main()
{
    fragColour = texture(cubeMap,fs_in.textureDir);
    brightColour = vec4(0.0);
}

[light]
void main()
{
    fragColour = vec4(12.0);
    brightColour = vec4(12.0);
    outPosition = lx_Colour(fs_in.viewPos);
    outNormal = lx_Colour(fs_in.viewNormal);
}