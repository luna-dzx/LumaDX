#version luma-dx
layout (location = 0) in vec3 aPos;
layout (location = 1) in vec2 aTexCoords;
layout (location = 2) in vec3 aNormal;

out VS_OUT {
    vec3 fragPos;
    vec3 normal;
    vec2 texCoords;
    vec4 fragPosLightSpace;
} vs_out;

uniform mat4 lightSpaceMatrix;
uniform int visualiseDepthMap;

void main()
{
    vs_out.normal = lx_NormalFix(lx_Model,aNormal);
    vs_out.texCoords = aTexCoords;
    vs_out.fragPos = (lx_Model*vec4(aPos,1.0)).xyz;
    
    vs_out.fragPosLightSpace = lightSpaceMatrix * vec4(vs_out.fragPos,1.0);
    
    if (visualiseDepthMap == 0)
    {
        gl_Position = lx_Transform*vec4(aPos, 1.0);
    }
    else
    {
        gl_Position = lightSpaceMatrix*lx_Model*vec4(aPos, 1.0);
    }
    
}