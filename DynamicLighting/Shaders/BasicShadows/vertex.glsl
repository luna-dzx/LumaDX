#version luma-dx
layout (location = 0) in vec3 inPosition;
layout (location = 1) in vec2 inTexCoords;
layout (location = 2) in vec3 inNormal;

out VS_OUT {
    vec3 fragPos;
    vec3 normal;
    vec2 texCoords;
    vec4 fragPosLightSpace;
} vs_out;

uniform mat4 lightSpaceMatrix;
uniform int visualiseDepthMap;
uniform float texCoordsMult;

void main()
{
    vs_out.normal = lx_NormalFix(lx_Model,inNormal);
    vs_out.texCoords = texCoordsMult*inTexCoords;
    vs_out.fragPos = (lx_Model*vec4(inPosition,1.0)).xyz;
    
    vs_out.fragPosLightSpace = lightSpaceMatrix * vec4(vs_out.fragPos,1.0);
    
    if (visualiseDepthMap == 0)
    {
        gl_Position = lx_Transform*vec4(inPosition, 1.0);
    }
    else
    {
        gl_Position = lightSpaceMatrix*lx_Model*vec4(inPosition, 1.0);
    }
    
}