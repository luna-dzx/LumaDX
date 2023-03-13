#version luma-dx

uniform sampler2D samplePosition;
uniform sampler2D sampleNormal;
uniform sampler2D noiseTex;

uniform mat4 proj;
uniform vec3 samples[64];

uniform vec2 noiseScale;

in vec2 texCoords;

void main()
{
    vec3 fragPos   = texture(samplePosition, texCoords).xyz;
    vec3 normal    = normalize(texture(sampleNormal, texCoords).xyz);
    vec3 randomVec = texture(noiseTex, texCoords*noiseScale).xyz;
    
    vec3 tangent   = normalize(randomVec - normal * dot(randomVec, normal));
    vec3 bitangent = cross(normal, tangent);
    mat3 TBN       = mat3(tangent, bitangent, normal);
    
    float kernelSize = 64.0;
    float radius = 0.5;
    float bias = 0.025;
    
    float occlusion = 0.0;
    for(int i = 0; i < kernelSize; ++i)
    {
        // get sample position
        vec3 samplePos = TBN * samples[i]; // from tangent to view-space
        samplePos = fragPos + samplePos * radius; 
        
        vec4 offset = vec4(samplePos, 1.0);
        offset      = proj * offset;    // from view to clip-space
        offset.xyz /= offset.w;               // perspective divide
        offset.xyz  = offset.xyz * 0.5 + 0.5; // transform to range 0.0 - 1.0  
        
        float sampleDepth = texture(samplePosition, offset.xy).z;
        float rangeCheck = smoothstep(0.0,1.0, radius / abs(fragPos.z - sampleDepth));
        occlusion       += (sampleDepth >= samplePos.z +bias ? 1.0 : 0.0)*rangeCheck;    
    } 
    
    lx_FragColour = vec4(1.0 - (occlusion / kernelSize));
}