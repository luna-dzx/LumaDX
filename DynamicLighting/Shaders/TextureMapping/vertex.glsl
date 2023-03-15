#version luma-dx
layout (location = 0) in vec3 inPosition;
layout (location = 1) in vec2 inTexCoords;
layout (location = 2) in vec3 inNormal;
layout (location = 3) in vec3 inTangent;

out VS_OUT {
    vec3 fragPos;
    vec3 TBNnormal;
    vec2 texCoords;
    vec3 TBNfragPos;
    vec3 TBNlightPos;
    vec3 tangent;
} vs_out;

uniform lx_Light light;

[mapping]
void main()
{
    mat3 TBN = lx_TBN(lx_Model,inTangent,inNormal);

    vs_out.TBNnormal = TBN * lx_NormalFix(lx_Model,inNormal);
    vs_out.texCoords = inTexCoords;
    vs_out.fragPos = (lx_Model*vec4(inPosition,1.0)).xyz;
    
    vs_out.TBNfragPos = TBN * vs_out.fragPos;
    vs_out.TBNlightPos = TBN * light.position;
    vs_out.tangent = TBN * vec3(1,0,0);

    gl_Position = lx_Transform * vec4(inPosition, 1.0);

}

uniform vec2 squarePos;
uniform vec2 squareSize;
uniform vec2 screenSize;

[2d]
void main()
{
    vs_out.texCoords = inTexCoords;
    gl_Position = vec4((inPosition.xy*squareSize + squarePos)/screenSize, 0.0, 1.0);
}

[frameBuffer]
void main()
{
    gl_Position = vec4(inPosition.xy, 0.0, 1.0);
}