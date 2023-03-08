#version luma-dx
layout (location = 0) in vec3 aPos;
layout (location = 1) in vec2 aTexCoords;
layout (location = 2) in vec3 aNormal;
layout (location = 3) in vec3 aTangent;

out VS_OUT {
    vec3 fragPos;
    vec3 normal;
    vec2 texCoords;
    vec3 TBNfragPos;
    vec3 TBNlightPos;
    vec3 TBNcameraPos;
    vec3 tangent;
    vec3 textureDir;
} vs_out;

uniform lx_Light light;
uniform vec3 cameraPos;


[scene]
void main()
{
    mat3 TBN = lx_TBN(lx_Model,aTangent,aNormal);

    vs_out.normal = lx_NormalFix(lx_Model,aNormal);
    vs_out.texCoords = aTexCoords;
    vs_out.fragPos = (lx_Model*vec4(aPos,1.0)).xyz;
    
    vs_out.TBNfragPos = TBN * vs_out.fragPos;
    vs_out.TBNcameraPos = TBN * cameraPos;
    vs_out.TBNlightPos = TBN * light.position;
    vs_out.tangent = TBN * vec3(1,0,0);

    gl_Position = lx_Transform * vec4(aPos, 1.0);
}


[skyBox]
void main()
{
    vs_out.textureDir = aPos;
    mat4 view = mat4(mat3(lx_View));
    gl_Position = (lx_Proj * view *vec4(aPos, 1.0)).xyww; // this sets z to w/w = 1.0 for maximum depth
}