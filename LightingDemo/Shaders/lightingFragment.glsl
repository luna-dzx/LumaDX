#version luma-dx

uniform sampler2D gPosition;
uniform sampler2D gNormal;
uniform sampler2D gTexCoords;
uniform sampler2D ssaoTex;

uniform lx_Material material;
uniform lx_Light light;

uniform mat4 view;

in vec2 texCoords;

uniform int ambientOcclusion;

void main()
{
    float ssao;
    if (ambientOcclusion > 0){
    
        ssao = texture(ssaoTex, texCoords).r;
    } else
    {
        ssao = 1.0;
    } 
    vec2 localTexCoords    = texture(gTexCoords, texCoords).xy;
    
    if (localTexCoords.x >= 0)
    {
        vec3 normal    = texture(gNormal, texCoords).rgb;
        vec3 fragPos    = texture(gPosition, texCoords).rgb;
        
        vec3 viewSpaceLightPos = (view * vec4(light.position,1.0)).xyz;
        lx_FragColour = lx_Colour(lx_Phong(normal, fragPos, vec3(0.0), localTexCoords, localTexCoords, material, lx_MoveLight(light, viewSpaceLightPos), ssao));
    }
    else
    {
        lx_FragColour = lx_Colour(ssao);
    }
}