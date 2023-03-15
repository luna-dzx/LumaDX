#version luma-dx
layout (location = 0) in vec3 inPosition;
layout (location = 1) in vec2 inTexCoords;
layout (location = 2) in vec3 inNormal;

out vec2 texCoords;
out vec3 fragPos;
out vec3 normal;
out vec4 fragPosLightSpace;
out vec3 textureDir;

uniform mat4 lightSpaceMatrix;

[scene]
void main()
{
    texCoords = inTexCoords;
    
    normal = lx_NormalFix(lx_Model,inNormal);
    fragPos = (lx_Model*vec4(inPosition,1.0)).xyz;
    
    fragPosLightSpace = lightSpaceMatrix * vec4(fragPos,1.0);
    
    gl_Position = lx_Transform*vec4(inPosition, 1.0);
}

[skyBox]
void main()
{
    textureDir = inPosition;
    mat4 view = mat4(mat3(lx_View));
    gl_Position = (lx_Proj * view *vec4(inPosition, 1.0)).xyww; // this sets z to w/w = 1.0 for maximum depth
}